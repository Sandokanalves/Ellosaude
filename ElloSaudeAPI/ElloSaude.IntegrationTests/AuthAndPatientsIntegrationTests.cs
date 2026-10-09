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

public class AuthAndPatientsIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public AuthAndPatientsIntegrationTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                var descriptors = services.Where(d =>
                    d.ServiceType == typeof(DbContextOptions<ApplicationDbContext>) ||
                    d.ServiceType == typeof(DbContextOptions) ||
                    d.ServiceType == typeof(ApplicationDbContext)
                ).ToList();

                foreach (var descriptor in descriptors)
                {
                    services.Remove(descriptor);
                }

                var internalServiceProvider = new ServiceCollection()
                    .AddEntityFrameworkInMemoryDatabase()
                    .BuildServiceProvider();

                var dbName = "IntegrationDb_" + Guid.NewGuid();
                services.AddDbContext<ApplicationDbContext>(options =>
                {
                    options.UseInMemoryDatabase(dbName)
                           .UseInternalServiceProvider(internalServiceProvider);
                });

                services.AddScoped<IApplicationDbContext>(provider =>
                    provider.GetRequiredService<ApplicationDbContext>());

                var busDescriptor = services.SingleOrDefault(d => d.ServiceType == typeof(IMessageBusService));
                if (busDescriptor != null) services.Remove(busDescriptor);

                services.AddSingleton<IMessageBusService, TestMessageBusService>();
            });
        });
    }

    [Fact]
    public async Task Complete_Flow_Login_CreatePatient_GetAllPatients()
    {
        var client = _factory.CreateClient();

        // 1. Authenticate with seeded admin user
        var loginResponse = await client.PostAsJsonAsync("/api/auth/login", new
        {
            Email = "admin@ellosaude.com",
            Password = "Senha123!"
        });

        if (loginResponse.StatusCode != HttpStatusCode.OK)
        {
            var content = await loginResponse.Content.ReadAsStringAsync();
            throw new Exception($"Login failed with {loginResponse.StatusCode}: {content}");
        }

        var loginContent = await loginResponse.Content.ReadFromJsonAsync<JsonElement>();
        var token = loginContent.GetProperty("token").GetString();
        token.Should().NotBeNullOrEmpty();

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var createPatientResponse = await client.PostAsJsonAsync("/api/patients", new
        {
            Name = "Paciente Teste Integração",
            Email = "integracao@paciente.com",
            Cpf = "999.111.222-33",
            BirthDate = "1992-04-12T00:00:00Z"
        });

        createPatientResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        var getPatientsResponse = await client.GetAsync("/api/patients");
        getPatientsResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var patientsJson = await getPatientsResponse.Content.ReadAsStringAsync();
        patientsJson.Should().Contain("Paciente Teste Integração");
    }
}
