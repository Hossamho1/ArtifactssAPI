using ArtifactsAPI.Application.DTOs;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Net;
using System.Threading.Tasks;

namespace ArtifactsAPI.Middlewares
{
    public class Middleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<Middleware> _logger;
        private readonly IHostEnvironment _env;

        public Middleware(RequestDelegate next, ILogger<Middleware> logger, IHostEnvironment env)
        {
            _next = next; // Represents the next request in the pipeline
            _logger = logger; // Used to log errors to the console
            _env = env; // Used to determine if we are in Development or Production environment
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                // Let the request continue normally to the Controller and Service
                await _next(context);
            }
            catch (Exception ex)
            {
                // If any class in the project throws an Exception, we catch it here immediately!
                _logger.LogError(ex, ex.Message);
                await HandleExceptionAsync(context, ex);
            }
        }

        private async Task HandleExceptionAsync(HttpContext context, Exception exception)
        {
            context.Response.ContentType = "application/json";

            context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;

            var response = new ErrorDetails
            {
                StatusCode = context.Response.StatusCode,
                Message = "An unexpected error occurred on the server. Please try again later.",
              
                Details = _env.IsDevelopment() ? exception.StackTrace?.ToString() : null
            };

            await context.Response.WriteAsync(response.ToString());
        }
    }
}