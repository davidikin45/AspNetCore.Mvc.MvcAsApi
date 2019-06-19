using AspNetCore.Mvc.MvcAsApi.Factories;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IO;
using Newtonsoft.Json;
using System;
using System.IO;
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
        private readonly RecyclableMemoryStreamManager _recyclableMemoryStreamManager;

        private static readonly Action<ILogger, Exception> _responseStartedErrorHandler =
           LoggerMessage.Define(LogLevel.Warning, new EventId(2, "ResponseStarted"), "The response has already started, the error handler will not be executed.");

        public ProblemDetailsErrorResponseHandlerMiddleware(RequestDelegate next, ILogger<ProblemDetailsErrorResponseHandlerMiddleware> logger, IServiceProvider serviceProvider, ProblemDetailsErrorResponseHandlerOptions errorResponseoptions)
        {
            _next = next;
            _logger = logger;
            _options = serviceProvider.GetService<IOptions<ApiBehaviorOptions>>()?.Value;
            _errorResponseoptions = errorResponseoptions;
            _recyclableMemoryStreamManager = new RecyclableMemoryStreamManager();
        }

        public async Task InvokeAsync(HttpContext context)
        {
            if(_errorResponseoptions.InterceptResponseStream)
            {
                //Copy a pointer to the original response body stream
                var originalBodyStream = context.Response.Body;

                try
                {
                    //Create a new memory stream...
                    using (var responseBody = _recyclableMemoryStreamManager.GetStream())
                    {

                        //...and use that for the temporary response body
                        context.Response.Body = responseBody;

                        //Continue down the Middleware pipeline, eventually returning to this class
                        await _next(context);

                        if (context.Request.HttpContext.Items.ContainsKey("mvcErrorHandled") || !_errorResponseoptions.handleError(context))
                        {
                            await responseBody.CopyToAsync(originalBodyStream).ConfigureAwait(false);
                            return;
                        }

                    }
                }
                finally
                {
                    context.Response.Body = originalBodyStream;
                }
            }
            else
            {
                await _next(context);

                if (context.Request.HttpContext.Items.ContainsKey("mvcErrorHandled") || !_errorResponseoptions.handleError(context))
                {
                    return;
                }
            }

            if (context.Response.HasStarted)
            {
                //This is caused by the first write or flush to the response body.
                _responseStartedErrorHandler(_logger, null);
                return;
            }

            var problemDetails = ProblemDetailsFactory.GetProblemDetails(context, "", context.Response.StatusCode, null);

            if (_options != null && _options.ClientErrorMapping.TryGetValue(context.Response.StatusCode, out var errorData))
            {
                problemDetails.Title = errorData.Title;
                problemDetails.Type = errorData.Link;
            }

            await WriteResultAsync(context, _options, problemDetails).ConfigureAwait(false);
        }

        private async Task WriteResultAsync(HttpContext context, ApiBehaviorOptions options, ProblemDetails problemDetails)
        {
            if (options != null)
            {
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
            else
            {
                var message = JsonConvert.SerializeObject(problemDetails);
                context.Response.StatusCode = problemDetails.Status.Value;
                context.Response.ContentType = "application/problem+json";
                await context.Response.WriteAsync(message).ConfigureAwait(false);
            }
        }
    }

    public class ProblemDetailsErrorResponseHandlerOptions
    {
        public bool InterceptResponseStream { get; set; } = true;
        public Func<HttpContext, bool> handleError { get; set; } = ((context) => context.Response.StatusCode >= 400);
    }
}
