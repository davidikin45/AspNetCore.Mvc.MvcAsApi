using AspNetCore.Mvc.MvcAsApi.Factories;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Threading.Tasks;
using WebApiContrib.Core.Results;

namespace AspNetCore.Mvc.MvcAsApi.Middleware
{
    public class ProblemDetailsErrorResponseHandlerMiddleware
    {
        private readonly RequestDelegate _next;

        private readonly ILogger _logger;
        private readonly ApiBehaviorOptions _options;
        private readonly ProblemDetailsErrorResponseHandlerOptions _errorResponseoptions;

        private static readonly Action<ILogger, Exception> _responseStartedErrorHandler =
           LoggerMessage.Define(LogLevel.Warning, new EventId(2, "ResponseStarted"), "The response has already started, the error handler will not be executed.");

        public ProblemDetailsErrorResponseHandlerMiddleware(RequestDelegate next, ILogger<ProblemDetailsErrorResponseHandlerMiddleware> logger, IServiceProvider serviceProvider, ProblemDetailsErrorResponseHandlerOptions errorResponseoptions)
        {
            _next = next;
            _logger = logger;
            _options = serviceProvider.GetService<IOptions<ApiBehaviorOptions>>()?.Value;
            _errorResponseoptions = errorResponseoptions;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            await _next(context);

            if(context.Request.HttpContext.Items.ContainsKey("mvcErrorHandled") || !_errorResponseoptions.handleError(context))
            {
                return;
            }

            if (context.Response.HasStarted)
            {
                _responseStartedErrorHandler(_logger, null);
                return;
            }

            var problemDetails = ProblemDetailsFactory.GetProblemDetails(context, "", context.Response.StatusCode, null);

            if (_options!= null && _options.ClientErrorMapping.TryGetValue(context.Response.StatusCode, out var errorData))
            {
                problemDetails.Title = errorData.Title;
                problemDetails.Type = errorData.Link;
            }

            var result = new ObjectResult(problemDetails)
            {
                StatusCode = problemDetails.Status,
                ContentTypes =
                {
                    "application/problem+json",
                    "application/problem+xml",
                },
            };

            await context.WriteActionResult(result);
        }
    }

    public class ProblemDetailsErrorResponseHandlerOptions
    {
        public Func<HttpContext, bool> handleError = ((context) => context.Response.StatusCode >= 400);
    }
}
