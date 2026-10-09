using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using ElloSaude.Application.Common.Interfaces;
using ElloSaude.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace ElloSaude.IntegrationTests;

public class TestMessageBusService : IMessageBusService
{
    public void Publish(string queue, object message) { }
}

/// <summary>
/// Factory compartilhada que usa SQLite InMemory (Microsoft.EntityFrameworkCore.Sqlite)
/// ou um banco InMemory puro para testes de integração.
/// Classe responsável por substituir toda a infra de dados.
/// </summary>
public class TestWebAppFactory : WebApplicationFactory<Program>
{
    private static readonly string TestJwtKey =
        Convert.ToBase64String(System.Security.Cryptography.RandomNumberGenerator.GetBytes(48));
    private readonly string _dbName = "TestDb_" + Guid.NewGuid();
    public string SeedPassword { get; } =
        Convert.ToBase64String(System.Security.Cryptography.RandomNumberGenerator.GetBytes(32));

    static TestWebAppFactory()
    {
        Environment.SetEnvironmentVariable(
            "ConnectionStrings__DefaultConnection",
            "Server=localhost;Database=ElloSaudeTests;Integrated Security=True;TrustServerCertificate=True");
        Environment.SetEnvironmentVariable(
            "Jwt__Key",
            TestJwtKey);
        Environment.SetEnvironmentVariable("Jwt__Issuer", "ElloSaudeTests");
        Environment.SetEnvironmentVariable("Jwt__Audience", "ElloSaudeTests");
    }

    protected override void ConfigureWebHost(Microsoft.AspNetCore.Hosting.IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            // Remove TODOS os registros de DbContext e provedores de banco
            var toRemove = services
                .Where(d =>
                    d.ServiceType == typeof(DbContextOptions<ApplicationDbContext>) ||
                    d.ServiceType == typeof(DbContextOptions) ||
                    d.ServiceType == typeof(IApplicationDbContext) ||
                    (d.ServiceType == typeof(ApplicationDbContext)) ||
                    d.ServiceType.FullName?.Contains("EntityFrameworkCore") == true)
                .ToList();

            foreach (var descriptor in toRemove)
                services.Remove(descriptor);

            // Registra o DbContext com banco InMemory exclusivo para este conjunto de testes
            services.AddDbContext<ApplicationDbContext>(options =>
                options.UseInMemoryDatabase(_dbName));

            services.AddScoped<IApplicationDbContext>(p =>
                p.GetRequiredService<ApplicationDbContext>());

            // Substitui o MessageBus por stub
            var bus = services.SingleOrDefault(d => d.ServiceType == typeof(IMessageBusService));
            if (bus != null) services.Remove(bus);
            services.AddSingleton<IMessageBusService, TestMessageBusService>();
        });
    }
}

public class AuthAndPatientsIntegrationTests : IClassFixture<TestWebAppFactory>
{
    private readonly TestWebAppFactory _factory;
    private bool _seeded = false;

    public AuthAndPatientsIntegrationTests(TestWebAppFactory factory)
    {
        _factory = factory;
    }

    private async Task EnsureSeededAsync()
    {
        if (_seeded) return;

        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var hashService = scope.ServiceProvider.GetRequiredService<IHashService>();

        // InMemory não precisa de MigrateAsync
        await context.Database.EnsureCreatedAsync();
        await DbInitializer.SeedAsync(context, hashService, _factory.SeedPassword);
        _seeded = true;
    }

    [Fact]
    public async Task Complete_Flow_Login_CreatePatient_GetAllPatients()
    {
        await EnsureSeededAsync();

        var client = _factory.CreateClient();

        // 1. Autentica com o usuário admin criado no seed
        var loginResponse = await client.PostAsJsonAsync("/api/auth/login", new
        {
            Email = "admin@ellosaude.com",
            Password = _factory.SeedPassword
        });

        if (loginResponse.StatusCode != HttpStatusCode.OK)
        {
            var errorContent = await loginResponse.Content.ReadAsStringAsync();
            throw new Exception($"Login falhou com {loginResponse.StatusCode}: {errorContent}");
        }

        var loginContent = await loginResponse.Content.ReadFromJsonAsync<JsonElement>();
        var token = loginContent.GetProperty("token").GetString();
        token.Should().NotBeNullOrEmpty();

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // 2. Cria um novo paciente
        var createPatientResponse = await client.PostAsJsonAsync("/api/patients", new
        {
            Name = "Paciente Teste Integração",
            Email = "integracao@paciente.com",
            Cpf = "999.111.222-33",
            BirthDate = "1992-04-12T00:00:00Z"
        });

        createPatientResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        // 3. Lista todos os pacientes e verifica que o criado está presente
        var getPatientsResponse = await client.GetAsync("/api/patients");
        var patientsResponseBody = await getPatientsResponse.Content.ReadAsStringAsync();
        getPatientsResponse.StatusCode.Should().Be(
            HttpStatusCode.OK,
            $"the patient list endpoint returned: {patientsResponseBody}");

        patientsResponseBody.Should().Contain("Paciente Teste Integração");
    }

    [Fact]
    public async Task Secretaria_NaoPode_AcessarProntuarios_Returns403()
    {
        await EnsureSeededAsync();

        var client = _factory.CreateClient();

        // Login como Secretária
        var loginResponse = await client.PostAsJsonAsync("/api/auth/login", new
        {
            Email = "secretaria@ellosaude.com",
            Password = _factory.SeedPassword
        });

        loginResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var loginContent = await loginResponse.Content.ReadFromJsonAsync<JsonElement>();
        var token = loginContent.GetProperty("token").GetString();

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Tenta acessar endpoint de prontuários — deve receber 403 Forbidden
        var response = await client.GetAsync("/api/medicalrecords/patient/00000000-0000-0000-0000-000000000001");
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task PatientPortal_RequiresTemporaryPasswordChange_AndRestrictsAppointmentOwnership()
    {
        await EnsureSeededAsync();
        var client = _factory.CreateClient();
        var staffToken = await Login(client, "secretaria@ellosaude.com", _factory.SeedPassword);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", staffToken);

        var pendingPayments = await client.GetAsync("/api/financial/pending");
        pendingPayments.StatusCode.Should().Be(HttpStatusCode.OK);
        var detailedReport = await client.GetAsync("/api/financial/report");
        detailedReport.StatusCode.Should().Be(HttpStatusCode.Forbidden);

        var patientResponse = await client.PostAsJsonAsync("/api/patients", new
        {
            Name = "Paciente Portal Teste",
            Email = "portal@patient.example",
            Cpf = "123.456.789-00",
            BirthDate = "1993-04-12T00:00:00Z"
        });
        patientResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var patient = await patientResponse.Content.ReadFromJsonAsync<JsonElement>();
        var patientId = patient.GetProperty("id").GetGuid();

        var accountResponse = await client.PostAsync($"/api/patients/{patientId}/portal-account", null);
        accountResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        accountResponse.Headers.CacheControl?.NoStore.Should().BeTrue();
        var credentials = await accountResponse.Content.ReadFromJsonAsync<JsonElement>();
        var temporaryPassword = credentials.GetProperty("temporaryPassword").GetString();

        var patientToken = await Login(client, "portal@patient.example", temporaryPassword!);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", patientToken);
        (await client.GetAsync("/api/patient-portal/my-appointments")).StatusCode
            .Should().Be(HttpStatusCode.Forbidden);

        var newPassword = Convert.ToBase64String(System.Security.Cryptography.RandomNumberGenerator.GetBytes(32));
        var changed = await client.PostAsJsonAsync("/api/auth/change-password", new
        {
            currentPassword = temporaryPassword,
            newPassword
        });
        changed.StatusCode.Should().Be(HttpStatusCode.NoContent);

        patientToken = await Login(client, "portal@patient.example", newPassword);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", patientToken);
        var myAppointments = await client.GetAsync("/api/patient-portal/my-appointments");
        myAppointments.StatusCode.Should().Be(HttpStatusCode.OK);
        (await myAppointments.Content.ReadFromJsonAsync<JsonElement>()).GetArrayLength().Should().Be(0);

        var foreignAppointment = await client.GetAsync(
            $"/api/patient-portal/my-appointments/{Guid.NewGuid()}");
        foreignAppointment.StatusCode.Should().Be(HttpStatusCode.NotFound);

        var oldPasswordLogin = await client.PostAsJsonAsync("/api/auth/login", new
        {
            email = "portal@patient.example",
            password = temporaryPassword
        });
        oldPasswordLogin.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    private static async Task<string> Login(HttpClient client, string email, string password)
    {
        client.DefaultRequestHeaders.Authorization = null;
        var response = await client.PostAsJsonAsync("/api/auth/login", new { email, password });
        var responseBody = await response.Content.ReadAsStringAsync();
        response.StatusCode.Should().Be(HttpStatusCode.OK, $"the login endpoint returned: {responseBody}");
        using var document = JsonDocument.Parse(responseBody);
        var content = document.RootElement;
        return content.GetProperty("token").GetString()!;
    }
}
