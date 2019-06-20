using AspNetCore.Mvc.MvcAsApi.Extensions;
using AspNetCore.Mvc.MvcAsApi.Factories;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IO;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using static AspNetCore.Mvc.MvcAsApi.Middleware.ProblemDetailsErrorResponseHandlerOptions;

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

                        if (context.Request.HttpContext.Items.ContainsKey("mvcErrorHandled") || !_errorResponseoptions.HandleError(context, _errorResponseoptions))
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

                if (context.Request.HttpContext.Items.ContainsKey("mvcErrorHandled") || !_errorResponseoptions.HandleError(context, _errorResponseoptions))
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

            ProblemDetailFactory factory = _errorResponseoptions.ProblemDetailFactories.ContainsKey(context.Response.StatusCode) ? _errorResponseoptions.ProblemDetailFactories[context.Response.StatusCode] : _errorResponseoptions.DefaultProblemDetailFactory ?? null;

            if(factory != null)
            {
                var problemDetails = factory(context, _logger);
                if (problemDetails != null)
                {
                    await context.WriteProblemDetailsResultAsync(problemDetails).ConfigureAwait(false);
                }
            }
        }
    }

    public class ProblemDetailsErrorResponseHandlerOptions
    {
        public bool InterceptResponseStream { get; set; } = true;
        public Func<HttpContext, ProblemDetailsErrorResponseHandlerOptions, bool> HandleError { get; set; } = ((context, options) => ((context.Response.StatusCode >= 400 && options.DefaultProblemDetailFactory != null) || options.ProblemDetailFactories.ContainsKey(context.Response.StatusCode)));

        public delegate ProblemDetails ProblemDetailFactory(HttpContext context, ILogger logger);

        public ProblemDetailFactory DefaultProblemDetailFactory { get; set; } = ((context, logger) =>
        {
            var problemDetails = ProblemDetailsTraceFactory.GetProblemDetails(context, "", context.Response.StatusCode, null);
            return problemDetails;
        });
        public Dictionary<int, ProblemDetailFactory> ProblemDetailFactories { get; set; } = new Dictionary<int, ProblemDetailFactory>() {
         
        };
    }
}
