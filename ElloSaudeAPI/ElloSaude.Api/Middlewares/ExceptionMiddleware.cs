using System.Text.Json;
using ElloSaude.Application.Common.Exceptions;
using Microsoft.AspNetCore.Mvc;

namespace ElloSaude.Api.Middlewares;

public class ExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionMiddleware> _logger;
    private readonly IHostEnvironment _env;

    public ExceptionMiddleware(RequestDelegate next, ILogger<ExceptionMiddleware> logger, IHostEnvironment env)
    {
        _next = next;
        _logger = logger;
        _env = env;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(context, ex);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        context.Response.ContentType = "application/problem+json";

        var (statusCode, title, detail, errors) = exception switch
        {
            ValidationException validationEx => (
                StatusCodes.Status400BadRequest,
                "Erro de Validação",
                "Ocorreram um ou mais erros de validação nos dados enviados.",
                validationEx.Errors as IDictionary<string, string[]>
            ),
            KeyNotFoundException keyNotFoundEx => (
                StatusCodes.Status404NotFound,
                "Recurso Não Encontrado",
                keyNotFoundEx.Message,
                null
            ),
            UnauthorizedAccessException unauthorizedEx => (
                StatusCodes.Status403Forbidden,
                "Acesso Não Autorizado",
                unauthorizedEx.Message,
                null
            ),
            InvalidOperationException invalidOpEx => (
                StatusCodes.Status409Conflict,
                "Conflito de Regra de Negócio",
                invalidOpEx.Message,
                null
            ),
            _ => (
                StatusCodes.Status500InternalServerError,
                "Erro Interno do Servidor",
                _env.IsDevelopment() ? exception.Message : "Ocorreu um erro interno ao processar a requisição.",
                null
            )
        };

        context.Response.StatusCode = statusCode;

        if (statusCode == StatusCodes.Status500InternalServerError)
        {
            _logger.LogError(exception, "Erro não tratado ao processar a requisição {Path}", context.Request.Path);
        }
        else
        {
            _logger.LogWarning("Falha na requisição {Path}: [{StatusCode}] {Title} - {Detail}",
                context.Request.Path, statusCode, title, detail);
        }

        object responseObj;

        if (errors != null)
        {
            responseObj = new ValidationProblemDetails(errors)
            {
                Status = statusCode,
                Title = title,
                Detail = detail,
                Instance = context.Request.Path
            };
        }
        else
        {
            responseObj = new ProblemDetails
            {
                Status = statusCode,
                Title = title,
                Detail = detail,
                Instance = context.Request.Path
            };
        }

        var options = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
        await context.Response.WriteAsync(JsonSerializer.Serialize(responseObj, options));
    }
}