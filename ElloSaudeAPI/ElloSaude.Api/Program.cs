using System.Reflection;
using System.Text;
using ElloSaude.Api.Middlewares;
using ElloSaude.Application.Common.Interfaces;
using ElloSaude.Infrastructure.Identity;
using ElloSaude.Infrastructure.Persistence;
using ElloSaude.Infrastructure.Persistence.Repositories;
using ElloSaude.Infrastructure.Messaging;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

// --- SERVIÇOS ---

builder.Services.AddControllers();

// Endpoints Api Explorer & OpenAPI
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddOpenApi();

// CORS - Liberado para integração com o Front-end
builder.Services.AddCors(options =>
{
    options.AddPolicy("FrontEndPolicy", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

// --- AUTENTICAÇÃO JWT ---
var jwtKey = builder.Configuration["Jwt:Key"] ?? "Chave_Super_Secreta_Com_Pelo_Menos_32_Caracteres";
var keyBytes = Encoding.ASCII.GetBytes(jwtKey);

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.RequireHttpsMetadata = false;
    options.SaveToken = true;
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(keyBytes),
        ValidateIssuer = false,
        ValidateAudience = false
    };
});

// --- CONFIGURAÇÃO DO SWAGGER ---
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "ElloSaúde API", Version = "v1" });
    
    // Inclusão do suporte a Bearer Token no Swagger UI
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

// 1. Serviços de Infraestrutura e Identidade
builder.Services.AddScoped<ITenantService, TenantService>();
builder.Services.AddScoped<IHashService, HashService>();
builder.Services.AddScoped<UserService>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddSingleton<IMessageBusService, RabbitMqService>();

// 2. Repositórios e Unit of Work
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
builder.Services.AddScoped(typeof(IRepository<>), typeof(Repository<>));

// 3. MediatR
builder.Services.AddMediatR(cfg =>
{
    cfg.RegisterServicesFromAssembly(typeof(IUnitOfWork).Assembly);
});

// 4. AutoMapper
builder.Services.AddAutoMapper(cfg =>
{
    cfg.AddMaps(typeof(IUnitOfWork).Assembly);
});

// --- CONFIGURAÇÃO DO BANCO DE DADOS ---
builder.Services.AddDbContext<ApplicationDbContext>((serviceProvider, options) =>
{
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"));
});

builder.Services.AddScoped<IApplicationDbContext>(provider =>
    provider.GetRequiredService<ApplicationDbContext>());

var app = builder.Build();

// --- SEED DATABASE EM DESENVOLVIMENTO / INICIALIZAÇÃO ---
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var dbContext = services.GetRequiredService<ApplicationDbContext>();
        var hashService = services.GetRequiredService<IHashService>();
        await DbInitializer.SeedAsync(dbContext, hashService);
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "Ocorreu um erro ao inicializar o banco de dados.");
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
app.MapControllers();

app.Run();

public partial class Program { }