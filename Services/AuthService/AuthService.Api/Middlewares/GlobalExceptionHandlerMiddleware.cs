using AuthService.Api.Contracts.Errors;
using AuthService.Api.Exceptions;
using AuthService.Domain.Common;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;
using System.Net;
using System.Text.Json;

namespace AuthService.Api.Middlewares
{
    public class GlobalExceptionHandlerMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly IHostEnvironment _environment;
        private static readonly JsonSerializerOptions _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        public GlobalExceptionHandlerMiddleware(RequestDelegate next, IHostEnvironment environment)
        {
            _next = next;
            _environment = environment;
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
            var errorResponse = MapException(exception, _environment);

            context.Response.StatusCode = errorResponse.Status;
            context.Response.ContentType = "application/json";

            var json = JsonSerializer.Serialize(errorResponse, _jsonOptions);
            await context.Response.WriteAsync(json);
        }

        private static ErrorResponse MapException(Exception ex, IHostEnvironment env)
        {
            return ex switch
            {
                ValidationException ve => MapValidationException(ve),
                NotFoundException nfe => MapNotFoundException(nfe),
                DbUpdateException dbe => MapDatabaseException(dbe, env),
                _ => MapGenericException(ex, env)
            };
        }

        private static ErrorResponse MapValidationException(ValidationException ex)
        {
            var errors = ex.Errors.Select(f => new ValidationError(f.PropertyName, f.ErrorMessage)).ToList();

            return new ErrorResponse(
                title: "Validation Error",
                status: (int)HttpStatusCode.BadRequest,
                errors: errors,
                traceId: Activity.Current?.Id ?? Guid.NewGuid().ToString(),
                timestamp: DateTime.UtcNow,
                detail: null
            );
        }

        private static ErrorResponse MapNotFoundException(NotFoundException ex)
        {
            var errors = new List<ValidationError>
            {
                new(ex.ResourceName, ex.Message)
            };

            return new ErrorResponse(
                title: "Resource Not Found",
                status: (int)HttpStatusCode.NotFound,
                errors: errors,
                traceId: Activity.Current?.Id ?? Guid.NewGuid().ToString(),
                timestamp: DateTime.UtcNow,
                detail: null
            );
        }

        private static ErrorResponse MapDatabaseException(DbUpdateException ex, IHostEnvironment environment)
        {
            var errors = new List<ValidationError>();
            var status = HttpStatusCode.InternalServerError;
            string title;

            if (IsUniqueConstraintViolation(ex))
            {
                status = HttpStatusCode.Conflict;
                title = "Conflict";
                errors.Add(new ValidationError("Database", "A record with this value already exists."));
            }
            else if (IsForeignKeyConstraintViolation(ex))
            {
                status = HttpStatusCode.BadRequest;
                title = "Invalid Reference";
                errors.Add(new ValidationError("Database", "Referenced resource does not exist."));
            }
            else
            {
                title = "Internal Server Error";
                errors.Add(new ValidationError("General", "An unexpected error occurred."));
            }

            return new ErrorResponse(
                title: title,
                status: (int)status,
                errors: errors,
                traceId: Activity.Current?.Id ?? Guid.NewGuid().ToString(),
                timestamp: DateTime.UtcNow,
                detail: environment.IsDevelopment() ? ex.ToString() : null
            );
        }

        private static bool IsUniqueConstraintViolation(DbUpdateException ex)
        {
            var message = ex.InnerException?.Message ?? string.Empty;
            return message.Contains("duplicate key", StringComparison.OrdinalIgnoreCase) ||
                   message.Contains("UNIQUE constraint", StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsForeignKeyConstraintViolation(DbUpdateException ex)
        {
            var message = ex.InnerException?.Message ?? string.Empty;
            return message.Contains("foreign key", StringComparison.OrdinalIgnoreCase) ||
                   message.Contains("FOREIGN KEY constraint", StringComparison.OrdinalIgnoreCase);
        }

        private static ErrorResponse MapGenericException(Exception ex, IHostEnvironment env)
        {
            var errors = new List<ValidationError>
            {
                new("General", "An unexpected error occurred.")
            };

            return new ErrorResponse(
                title: "Internal Server Error",
                status: (int)HttpStatusCode.InternalServerError,
                errors: errors,
                traceId: Activity.Current?.Id ?? Guid.NewGuid().ToString(),
                timestamp: DateTime.UtcNow,
                detail: env.IsDevelopment() ? ex.ToString() : null
            );
        }
    }
}