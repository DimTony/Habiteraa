using Habitera.DTOs;
using System.Net;
using System.Text.Json;

namespace Habitera.Middleware
{
    public class GlobalExceptionHandlerMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<GlobalExceptionHandlerMiddleware> _logger;

        public GlobalExceptionHandlerMiddleware(
            RequestDelegate next,
            ILogger<GlobalExceptionHandlerMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An unhandled exception occurred");
                await HandleExceptionAsync(context, ex);
            }
        }

        private static Task HandleExceptionAsync(HttpContext context, Exception exception)
        {
            context.Response.ContentType = "application/json";

            var response = exception switch
            {
                UnauthorizedAccessException => OperationResult<object>.Failure(
                    "Unauthorized access",
                    (int)HttpStatusCode.Unauthorized,
                    exception.Message
                ),
                KeyNotFoundException => OperationResult<object>.Failure(
                    "Resource not found",
                    (int)HttpStatusCode.NotFound,
                    exception.Message
                ),
                ArgumentException => OperationResult<object>.Failure(
                    "Invalid argument",
                    (int)HttpStatusCode.BadRequest,
                    exception.Message
                ),
                InvalidOperationException => OperationResult<object>.Failure(
                    "Invalid operation",
                    (int)HttpStatusCode.BadRequest,
                    exception.Message
                ),
                _ => OperationResult<object>.Failure(
                    "An internal server error occurred",
                    (int)HttpStatusCode.InternalServerError,
                    exception.Message
                )
            };

            context.Response.StatusCode = response.StatusCode;

            var options = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };

            return context.Response.WriteAsync(JsonSerializer.Serialize(response, options));
        }
    }

    public static class GlobalExceptionHandlerMiddlewareExtensions
    {
        public static IApplicationBuilder UseGlobalExceptionHandler(
            this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<GlobalExceptionHandlerMiddleware>();
        }
    }
}