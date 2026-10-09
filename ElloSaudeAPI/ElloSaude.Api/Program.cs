using System.Text;
using ElloSaude.Api.Middlewares;
using ElloSaude.Application;
using ElloSaude.Application.Common.Interfaces;
using ElloSaude.Infrastructure.Identity;
using ElloSaude.Infrastructure.Persistence;
using ElloSaude.Infrastructure.Documents;
using ElloSaude.Infrastructure.Persistence.Repositories;
using ElloSaude.Infrastructure.Messaging;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

// --- SERVIÇOS ---

builder.Services.AddControllers();

// Health Checks
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
if (string.IsNullOrWhiteSpace(connectionString))
    throw new InvalidOperationException("ConnectionStrings:DefaultConnection deve ser configurada.");

builder.Services.AddHealthChecks()
    .AddSqlServer(connectionString, name: "sqlserver", tags: new[] { "ready" });

// Endpoints Api Explorer & OpenAPI
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddOpenApi();

// CORS - Restrito para origens seguras do frontend
var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
    ?? (builder.Environment.IsDevelopment()
        ? new[] { "http://localhost:5173", "http://localhost:3000", "http://localhost:80", "http://localhost:4173" }
        : Array.Empty<string>());

if (allowedOrigins.Length == 0)
    throw new InvalidOperationException("Cors:AllowedOrigins deve conter ao menos uma origem permitida.");

builder.Services.AddCors(options =>
{
    options.AddPolicy("FrontEndPolicy", policy =>
    {
        policy.WithOrigins(allowedOrigins)
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials();
    });
});

// --- AUTENTICAÇÃO JWT ---
var jwtKey = builder.Configuration["Jwt:Key"];
if (string.IsNullOrWhiteSpace(jwtKey))
    throw new InvalidOperationException("Jwt:Key deve ser configurada.");
if (!builder.Environment.IsDevelopment()
    && string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("Jwt__Key")))
{
    throw new InvalidOperationException("Em produção, Jwt:Key deve ser fornecida por variável de ambiente.");
}

var keyBytes = Encoding.UTF8.GetBytes(jwtKey);
if (keyBytes.Length < 32)
    throw new InvalidOperationException("Jwt:Key deve ter ao menos 32 bytes.");

var jwtIssuer = builder.Configuration["Jwt:Issuer"] ?? "ElloSaudeAPI";
var jwtAudience = builder.Configuration["Jwt:Audience"] ?? "ElloSaudeClientes";

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.RequireHttpsMetadata = !builder.Environment.IsDevelopment();
    options.SaveToken = true;
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(keyBytes),
        ValidateIssuer = true,
        ValidIssuer = jwtIssuer,
        ValidateAudience = true,
        ValidAudience = jwtAudience,
        ValidateLifetime = true,
        ClockSkew = TimeSpan.Zero
    };
});

// --- CONFIGURAÇÃO DO SWAGGER ---
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "ElloSaúde API", Version = "v1" });
    
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "Insira o token JWT no formato: Bearer {seu_token}",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

// --- INJEÇÃO DE DEPENDÊNCIAS ---

builder.Services.AddHttpContextAccessor();

// 1. Serviços da camada de Aplicação (AutoMapper, FluentValidation, MediatR com ValidationBehavior)
builder.Services.AddApplicationServices();

// 2. Serviços de Infraestrutura e Identidade
builder.Services.AddScoped<ITenantService, TenantService>();
builder.Services.AddScoped<IHashService, HashService>();
builder.Services.AddScoped<UserService>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddSingleton<IMessageBusService, RabbitMqService>();
builder.Services.AddScoped<IPrescriptionPdfService, QuestPdfPrescriptionPdfService>();

// 3. Repositórios e Unit of Work
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
builder.Services.AddScoped(typeof(IRepository<>), typeof(Repository<>));

// 4. Configuração do Banco de Dados
builder.Services.AddDbContext<ApplicationDbContext>((serviceProvider, options) =>
{
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"));
});

builder.Services.AddScoped<IApplicationDbContext>(provider =>
    provider.GetRequiredService<ApplicationDbContext>());

var app = builder.Build();

QuestPDF.Settings.License = QuestPDF.Infrastructure.LicenseType.Community;

// --- MIGRATIONS E SEED DE DESENVOLVIMENTO ---
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var dbContext = services.GetRequiredService<ApplicationDbContext>();
    if (dbContext.Database.IsRelational())
    {
        await dbContext.Database.MigrateAsync();
    }
    else if (app.Environment.IsDevelopment())
    {
        await dbContext.Database.EnsureCreatedAsync();
    }
    else
    {
        throw new InvalidOperationException("A inicialização de produção requer um provedor relacional com migrations.");
    }

    if (app.Environment.IsDevelopment()
        && builder.Configuration.GetValue<bool>("DevelopmentSeed:Enabled"))
    {
        var seedPassword = builder.Configuration["DevelopmentSeed:Password"];
        if (string.IsNullOrWhiteSpace(seedPassword) || seedPassword.Length < 16)
            throw new InvalidOperationException("DevelopmentSeed:Password deve conter ao menos 16 caracteres.");

        var hashService = services.GetRequiredService<IHashService>();
        await DbInitializer.SeedAsync(dbContext, hashService, seedPassword);
    }
}

// --- MIDDLEWARE PIPELINE ---

app.UseMiddleware<ExceptionMiddleware>();

app.UseCors("FrontEndPolicy");

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "ElloSaúde API v1");
    });
    app.MapOpenApi();
    app.MapScalarApiReference(options =>
    {
        options.WithTitle("ElloSaude API v1")
               .WithTheme(ScalarTheme.Moon);
    });
}

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();

// Mapeamento de Health Checks
app.MapHealthChecks("/health");
app.MapHealthChecks("/health/ready", new HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("ready")
});

app.MapControllers();

app.Run();

public partial class Program { }