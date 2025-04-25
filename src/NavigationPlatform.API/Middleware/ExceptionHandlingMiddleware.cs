using System.Net;
using System.Text.Json;
using FluentValidation;
using NavigationPlatform.Application.Common.Exceptions;
using NavigationPlatform.Application.Common.Models;

namespace NavigationPlatform.API.Middleware
{
    public class ExceptionHandlingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<ExceptionHandlingMiddleware> _logger;

        public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
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
                _logger.LogError(ex, "An unhandled exception has occurred: {Message}", ex.Message);
                await HandleExceptionAsync(context, ex);
            }
        }

        private static async Task HandleExceptionAsync(HttpContext context, Exception exception)
        {
            context.Response.ContentType = "application/json";
            
            ApiResponse errorResponse;
            
            switch (exception)
            {
                case ValidationException validationException:
                    context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                    var errors = validationException.Errors.Select(e => $"{e.PropertyName}: {e.ErrorMessage}");
                    errorResponse = ApiResponse.CreateFailure($"Validation failed: {string.Join(", ", errors)}");
                    break;
                
                case NotFoundException notFoundException:
                    context.Response.StatusCode = (int)HttpStatusCode.NotFound;
                    errorResponse = ApiResponse.CreateFailure(notFoundException.Message);
                    break;
                
                case ForbiddenAccessException forbiddenAccessException:
                    context.Response.StatusCode = (int)HttpStatusCode.Forbidden;
                    errorResponse = ApiResponse.CreateFailure(forbiddenAccessException.Message);
                    break;
                
                case GoneException goneException:
                    context.Response.StatusCode = (int)HttpStatusCode.Gone;
                    errorResponse = ApiResponse.CreateFailure(goneException.Message);
                    break;
                
                default:
                    context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
                    errorResponse = ApiResponse.CreateFailure("An error occurred while processing your request.");
                    break;
            }
            
            var result = JsonSerializer.Serialize(errorResponse, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });
            
            await context.Response.WriteAsync(result);
        }
    }
} 