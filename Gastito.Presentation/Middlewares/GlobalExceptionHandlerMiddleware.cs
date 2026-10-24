using System.Net.Mime;
using System.Text.Json;
using Gastito.BusinessLogic.Exceptions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Gastito.Presentation.Middlewares;

public class GlobalExceptionHandlerMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionHandlerMiddleware> _logger;
    private readonly IHostEnvironment _environment;

    public GlobalExceptionHandlerMiddleware(
        RequestDelegate next,
        ILogger<GlobalExceptionHandlerMiddleware> logger,
        IHostEnvironment environment)
    {
        _next = next;
        _logger = logger;
        _environment = environment;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (BusinessException ex)
        {
            _logger.LogWarning(ex, "Regla de negocio no cumplida: {Message} en {Path}", ex.Message, context.Request.Path);
            await HandleBusinessExceptionAsync(context, ex);
        }
        catch (KeyNotFoundException ex)
        {
            _logger.LogWarning(ex, "Recurso no encontrado: {Message} en {Path}", ex.Message, context.Request.Path);
            await HandleNotFoundExceptionAsync(context, ex);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Excepción no controlada al procesar la solicitud en {Path}: {Message}", context.Request.Path, ex.Message);
            await HandleUnhandledExceptionAsync(context, ex);
        }
    }

    private static async Task HandleBusinessExceptionAsync(HttpContext context, BusinessException ex)
    {
        context.Response.StatusCode = StatusCodes.Status400BadRequest;
        context.Response.ContentType = "application/problem+json";

        var problemDetails = new ProblemDetails
        {
            Type = "https://tools.ietf.org/html/rfc7231#section-6.5.1",
            Title = "Regla de negocio no cumplida",
            Status = StatusCodes.Status400BadRequest,
            Detail = ex.Message,
            Instance = context.Request.Path
        };

        problemDetails.Extensions["traceId"] = context.TraceIdentifier;

        var json = JsonSerializer.Serialize(problemDetails, JsonOptions);
        await context.Response.WriteAsync(json);
    }

    private static async Task HandleNotFoundExceptionAsync(HttpContext context, KeyNotFoundException ex)
    {
        context.Response.StatusCode = StatusCodes.Status404NotFound;
        context.Response.ContentType = "application/problem+json";

        var problemDetails = new ProblemDetails
        {
            Type = "https://tools.ietf.org/html/rfc7231#section-6.5.4",
            Title = "Recurso no encontrado",
            Status = StatusCodes.Status404NotFound,
            Detail = ex.Message,
            Instance = context.Request.Path
        };

        problemDetails.Extensions["traceId"] = context.TraceIdentifier;

        var json = JsonSerializer.Serialize(problemDetails, JsonOptions);
        await context.Response.WriteAsync(json);
    }

    private async Task HandleUnhandledExceptionAsync(HttpContext context, Exception ex)
    {
        context.Response.StatusCode = StatusCodes.Status500InternalServerError;
        context.Response.ContentType = "application/problem+json";

        var isDevelopment = _environment.IsDevelopment();
        var problemDetails = new ProblemDetails
        {
            Type = "https://tools.ietf.org/html/rfc7231#section-6.6.1",
            Title = "Error interno del servidor",
            Status = StatusCodes.Status500InternalServerError,
            Detail = isDevelopment ? ex.Message : "Ha ocurrido un error inesperado en el servidor.",
            Instance = context.Request.Path
        };

        if (isDevelopment && ex.StackTrace != null)
        {
            problemDetails.Extensions["stackTrace"] = ex.StackTrace;
        }

        problemDetails.Extensions["traceId"] = context.TraceIdentifier;

        var json = JsonSerializer.Serialize(problemDetails, JsonOptions);
        await context.Response.WriteAsync(json);
    }

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false
    };
}
