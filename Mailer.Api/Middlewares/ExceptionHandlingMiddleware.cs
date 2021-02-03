using Mailer.Exceptions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Polly;
using System;
using System.ComponentModel.DataAnnotations;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

namespace Mailer.Api.Middlewares
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

        public async Task Invoke(HttpContext context)
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

        private Task HandleExceptionAsync(HttpContext context, Exception exception)
        {
            var (code, message) = exception switch
            {
                NotFoundException _ => (HttpStatusCode.NotFound, "Object not found"),
                InvalidVersionException _ => (HttpStatusCode.Conflict, "Data modified by third party"),
                ValidationException ve => (HttpStatusCode.BadRequest, ve.Message),
                MailerValidationException ve => (HttpStatusCode.BadRequest, ve.Message),
                ExecutionRejectedException _ => (HttpStatusCode.ServiceUnavailable, "External dependent service not available"),
                HttpRequestException _ => (HttpStatusCode.ServiceUnavailable, "External dependent service not available"),
                _ => (HttpStatusCode.InternalServerError, "Internal server error")
            };
            _logger.LogError(exception, exception.Message);
            context.Response.ContentType = "application/json";
            context.Response.StatusCode = (int)code;
            return context.Response.WriteAsync(JsonSerializer.Serialize(new { error = message }));
        }
    }
}