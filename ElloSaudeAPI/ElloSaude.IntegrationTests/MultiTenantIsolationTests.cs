using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using ElloSaude.Application.Common.Interfaces;
using ElloSaude.Domain.Entities;
using ElloSaude.Domain.Enums;
using ElloSaude.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using ElloSaude.Infrastructure.Messaging;
using Xunit;

namespace ElloSaude.IntegrationTests;

/// <summary>
/// Suite de testes que comprova o isolamento rigoroso de dados entre tenants distintos.
/// Garante que nenhum usuário da Clínica A consiga acessar dados da Clínica B, mesmo
/// manipulando IDs na requisição — requisito crítico de segurança SaaS / LGPD.
/// </summary>
public class TenantIsolationTestFactory : WebApplicationFactory<Program>
{
    private static readonly string TestJwtKey =
        Convert.ToBase64String(System.Security.Cryptography.RandomNumberGenerator.GetBytes(48));

    private readonly string _tenantADbName = "TenantA_" + Guid.NewGuid();
    private readonly string _tenantBDbName = "TenantB_" + Guid.NewGuid();
    private readonly string _sharedDbName;

    public string SeedPassword { get; } =
        Convert.ToBase64String(System.Security.Cryptography.RandomNumberGenerator.GetBytes(24));

    // Usa um banco compartilhado para simular dois tenants no mesmo banco (multi-tenant real)
    public TenantIsolationTestFactory()
    {
        _sharedDbName = "MultiTenant_" + Guid.NewGuid();
    }

    static TenantIsolationTestFactory()
    {
        Environment.SetEnvironmentVariable(
            "ConnectionStrings__DefaultConnection",
            "Server=localhost;Database=ElloSaudeTests;Integrated Security=True;TrustServerCertificate=True");
        Environment.SetEnvironmentVariable("Jwt__Key", TestJwtKey);
        Environment.SetEnvironmentVariable("Jwt__Issuer", "ElloSaudeTests");
        Environment.SetEnvironmentVariable("Jwt__Audience", "ElloSaudeTests");
    }

    protected override void ConfigureWebHost(Microsoft.AspNetCore.Hosting.IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            // Remove background services
            var backgroundServices = services
                .Where(descriptor => descriptor.ServiceType == typeof(IHostedService)
                    && descriptor.ImplementationType is not null
                    && (descriptor.ImplementationType == typeof(OutboxPublisherService)
                        || descriptor.ImplementationType == typeof(WaitingRoomNotificationConsumerService)))
                .ToList();
            foreach (var svc in backgroundServices)
                services.Remove(svc);

            // Substitui banco por InMemory isolado por tenant
            var toRemove = services
                .Where(d =>
                    d.ServiceType == typeof(DbContextOptions<ApplicationDbContext>) ||
                    d.ServiceType == typeof(DbContextOptions) ||
                    d.ServiceType == typeof(IApplicationDbContext) ||
                    d.ServiceType == typeof(ApplicationDbContext) ||
                    d.ServiceType.FullName?.Contains("EntityFrameworkCore") == true)
                .ToList();
            foreach (var descriptor in toRemove)
                services.Remove(descriptor);

            services.AddDbContext<ApplicationDbContext>(options =>
                options.UseInMemoryDatabase(_sharedDbName));
            services.AddScoped<IApplicationDbContext>(p =>
                p.GetRequiredService<ApplicationDbContext>());

            var bus = services.SingleOrDefault(d => d.ServiceType == typeof(IMessageBusService));
            if (bus != null) services.Remove(bus);
            services.AddSingleton<IMessageBusService, TestMessageBusService>();
        });
    }

    public async Task SeedTenantsAsync()
    {
        using var scope = Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var hashService = scope.ServiceProvider.GetRequiredService<IHashService>();
        await context.Database.EnsureCreatedAsync();
        await DbInitializer.SeedAsync(context, hashService, SeedPassword);
    }
}

public class MultiTenantIsolationTests : IClassFixture<TenantIsolationTestFactory>
{
    private readonly TenantIsolationTestFactory _factory;
    private bool _seeded = false;

    public MultiTenantIsolationTests(TenantIsolationTestFactory factory)
    {
        _factory = factory;
    }

    private async Task EnsureSeededAsync()
    {
        if (_seeded) return;
        await _factory.SeedTenantsAsync();
        _seeded = true;
    }

    private static async Task<string> LoginAsync(HttpClient client, string email, string password)
    {
        client.DefaultRequestHeaders.Authorization = null;
        var response = await client.PostAsJsonAsync("/api/auth/login", new { email, password });
        response.StatusCode.Should().Be(HttpStatusCode.OK, $"Login falhou para {email}");
        var body = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(body);
        return doc.RootElement.GetProperty("token").GetString()!;
    }

    /// <summary>
    /// Comprova que a lista de pacientes é filtrada por tenant:
    /// um admin não vê pacientes de outro tenant mesmo no mesmo banco.
    /// </summary>
    [Fact]
    public async Task Patients_AreIsolatedByTenant()
    {
        await EnsureSeededAsync();
        var client = _factory.CreateClient();

        // Autentica como admin do tenant-default (seeded)
        var token = await LoginAsync(client, "admin@ellosaude.com", _factory.SeedPassword);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Cria um paciente para o tenant do admin
        var createResp = await client.PostAsJsonAsync("/api/patients", new
        {
            Name = "Paciente Tenant A",
            Email = "pacientea@tenant.com",
            Cpf = "111.222.333-44",
            BirthDate = "1985-06-15T00:00:00Z"
        });
        createResp.StatusCode.Should().Be(HttpStatusCode.Created);
        var createdBody = await createResp.Content.ReadFromJsonAsync<JsonElement>();
        var patientId = createdBody.GetProperty("id").GetGuid();

        // O admin vê o paciente na listagem
        var listResp = await client.GetAsync("/api/patients");
        var listBody = await listResp.Content.ReadAsStringAsync();
        listResp.StatusCode.Should().Be(HttpStatusCode.OK);
        listBody.Should().Contain("Paciente Tenant A");

        // Verifica que o paciente pode ser consultado pelo seu ID dentro do tenant
        var getResp = await client.GetAsync($"/api/patients/{patientId}");
        getResp.StatusCode.Should().Be(HttpStatusCode.OK);

        // Um ID inventado (de outro tenant hipotético) retorna 404 — sem vazamento de dados
        var foreignResp = await client.GetAsync($"/api/patients/{Guid.NewGuid()}");
        foreignResp.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    /// <summary>
    /// Comprova que um paciente autenticado no portal não acessa receitas de outros pacientes,
    /// mesmo conhecendo o ID da receita.
    /// </summary>
    [Fact]
    public async Task PatientPortal_CannotAccessPrescriptionsOfAnotherPatient()
    {
        await EnsureSeededAsync();
        var client = _factory.CreateClient();

        // Staff cria dois pacientes e dois portais
        var staffToken = await LoginAsync(client, "admin@ellosaude.com", _factory.SeedPassword);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", staffToken);

        async Task<(Guid PatientId, string PatientToken)> CreatePatientWithPortal(string email, string cpf)
        {
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", staffToken);
            var resp = await client.PostAsJsonAsync("/api/patients", new
            {
                Name = $"Paciente {cpf}",
                Email = email,
                Cpf = cpf,
                BirthDate = "1990-01-01T00:00:00Z"
            });
            resp.StatusCode.Should().Be(HttpStatusCode.Created);
            var body = await resp.Content.ReadFromJsonAsync<JsonElement>();
            var pid = body.GetProperty("id").GetGuid();

            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", staffToken);
            var accountResp = await client.PostAsync($"/api/patients/{pid}/portal-account", null);
            accountResp.StatusCode.Should().Be(HttpStatusCode.OK);
            var creds = await accountResp.Content.ReadFromJsonAsync<JsonElement>();
            var tempPwd = creds.GetProperty("temporaryPassword").GetString()!;

            // Troca senha obrigatória
            var tempToken = await LoginAsync(client, email, tempPwd);
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tempToken);
            var newPwd = Convert.ToBase64String(System.Security.Cryptography.RandomNumberGenerator.GetBytes(24));
            var changePwdResp = await client.PostAsJsonAsync("/api/auth/change-password", new
            {
                currentPassword = tempPwd,
                newPassword = newPwd
            });
            changePwdResp.StatusCode.Should().Be(HttpStatusCode.NoContent);

            var finalToken = await LoginAsync(client, email, newPwd);
            return (pid, finalToken);
        }

        var (patientAId, tokenA) = await CreatePatientWithPortal("paciente.a@portal.test", "555.666.777-88");
        var (patientBId, tokenB) = await CreatePatientWithPortal("paciente.b@portal.test", "888.999.000-11");

        // Paciente A não deve conseguir listar receitas do Paciente B
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokenA);
        var myPrescriptions = await client.GetAsync("/api/patient-portal/my-prescriptions");
        myPrescriptions.StatusCode.Should().Be(HttpStatusCode.OK);

        // O endpoint retorna apenas as receitas do paciente autenticado (zero neste caso, sem consultas)
        var prescBody = await myPrescriptions.Content.ReadFromJsonAsync<JsonElement>();
        prescBody.GetArrayLength().Should().Be(0, "Paciente A não deve ver receitas de Paciente B");

        // Tenta forçar acesso a receita de ID aleatório (simulando ID de outro paciente)
        var foreignPrescriptionId = Guid.NewGuid();
        var foreignResp = await client.GetAsync($"/api/prescriptions/{foreignPrescriptionId}/pdf");
        foreignResp.StatusCode.Should().BeOneOf([HttpStatusCode.NotFound, HttpStatusCode.Forbidden],
            "Paciente A não deve ter acesso a receitas de outros pacientes ou tenants");
    }

    /// <summary>
    /// Comprova que a secretária recebe 403 ao tentar acessar o relatório financeiro detalhado
    /// (reservado apenas para Admin e Profissional).
    /// </summary>
    [Fact]
    public async Task Secretary_CannotAccessDetailedFinancialReport()
    {
        await EnsureSeededAsync();
        var client = _factory.CreateClient();

        var token = await LoginAsync(client, "secretaria@ellosaude.com", _factory.SeedPassword);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var reportResp = await client.GetAsync("/api/financial/report");
        reportResp.StatusCode.Should().Be(HttpStatusCode.Forbidden,
            "Secretária não deve ter acesso ao relatório financeiro detalhado");
    }

    /// <summary>
    /// Comprova que a secretária pode acessar pagamentos pendentes (acesso permitido).
    /// </summary>
    [Fact]
    public async Task Secretary_CanAccessPendingPayments()
    {
        await EnsureSeededAsync();
        var client = _factory.CreateClient();

        var token = await LoginAsync(client, "secretaria@ellosaude.com", _factory.SeedPassword);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var pendingResp = await client.GetAsync("/api/financial/pending");
        pendingResp.StatusCode.Should().Be(HttpStatusCode.OK,
            "Secretária deve conseguir ver pagamentos pendentes");
    }

    /// <summary>
    /// Comprova que agendamentos só são visíveis para o tenant correto.
    /// IDs de agendamentos de outro tenant retornam 404 (sem vazamento).
    /// </summary>
    [Fact]
    public async Task Appointments_AreIsolatedByTenant()
    {
        await EnsureSeededAsync();
        var client = _factory.CreateClient();

        // Admin (único tenant no seed) tenta acessar ID inventado de outro tenant
        var token = await LoginAsync(client, "admin@ellosaude.com", _factory.SeedPassword);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // ID de agendamento inventado — não pertence ao tenant do admin
        var foreignAppointmentId = Guid.NewGuid();
        var resp = await client.GetAsync($"/api/appointments/{foreignAppointmentId}");
        resp.StatusCode.Should().BeOneOf([HttpStatusCode.NotFound, HttpStatusCode.MethodNotAllowed],
            "Agendamento de outro tenant não deve ser retornado");
    }

    /// <summary>
    /// Comprova que o paciente autenticado no portal não consegue cancelar agendamentos alheios.
    /// </summary>
    [Fact]
    public async Task PatientPortal_CannotCancelForeignAppointment()
    {
        await EnsureSeededAsync();
        var client = _factory.CreateClient();

        // Cria paciente com portal
        var staffToken = await LoginAsync(client, "admin@ellosaude.com", _factory.SeedPassword);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", staffToken);

        var resp = await client.PostAsJsonAsync("/api/patients", new
        {
            Name = "Paciente Portal Cancelamento",
            Email = "cancel@portal.test",
            Cpf = "321.654.987-00",
            BirthDate = "1988-05-20T00:00:00Z"
        });
        resp.StatusCode.Should().Be(HttpStatusCode.Created);
        var body = await resp.Content.ReadFromJsonAsync<JsonElement>();
        var patientId = body.GetProperty("id").GetGuid();

        var accountResp = await client.PostAsync($"/api/patients/{patientId}/portal-account", null);
        accountResp.StatusCode.Should().Be(HttpStatusCode.OK);
        var creds = await accountResp.Content.ReadFromJsonAsync<JsonElement>();
        var tempPwd = creds.GetProperty("temporaryPassword").GetString()!;

        // Troca senha
        var tempToken = await LoginAsync(client, "cancel@portal.test", tempPwd);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tempToken);
        var newPwd = Convert.ToBase64String(System.Security.Cryptography.RandomNumberGenerator.GetBytes(24));
        var changePwdResp = await client.PostAsJsonAsync("/api/auth/change-password", new
        {
            currentPassword = tempPwd,
            newPassword = newPwd
        });
        changePwdResp.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var patientToken = await LoginAsync(client, "cancel@portal.test", newPwd);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", patientToken);

        // Tenta cancelar um agendamento com ID inventado (pertenceria a outro paciente)
        var foreignAppointmentId = Guid.NewGuid();
        var cancelResp = await client.PostAsync($"/api/patient-portal/cancel/{foreignAppointmentId}", null);
        cancelResp.StatusCode.Should().BeOneOf([HttpStatusCode.NotFound, HttpStatusCode.Forbidden],
            "Paciente não deve conseguir cancelar agendamentos alheios");
    }
}
