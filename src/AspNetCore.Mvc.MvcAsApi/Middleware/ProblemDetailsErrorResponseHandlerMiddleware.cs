using AspNetCore.Mvc.MvcAsApi.Extensions;
using AspNetCore.Mvc.MvcAsApi.Factories;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Formatters.Internal;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IO;
using Microsoft.Net.Http.Headers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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
        private readonly Func<object, Task> _clearCacheHeadersDelegate;

        public ProblemDetailsErrorResponseHandlerMiddleware(RequestDelegate next, ILogger<ProblemDetailsErrorResponseHandlerMiddleware> logger, IServiceProvider serviceProvider, ProblemDetailsErrorResponseHandlerOptions errorResponseoptions)
        {
            _next = next;
            _logger = logger;
            _options = serviceProvider.GetService<IOptions<ApiBehaviorOptions>>()?.Value;
            _errorResponseoptions = errorResponseoptions;
            _recyclableMemoryStreamManager = new RecyclableMemoryStreamManager();
            _clearCacheHeadersDelegate = ClearCacheHeaders;
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

                        if ((!_errorResponseoptions.HandleProblemDetailResponses && context.Response.ContentType.Contains("application/problem")) || (!_errorResponseoptions.HandleMvcHandledResponses && context.Items.ContainsKey("mvcErrorHandled")) || !_errorResponseoptions.HandleError(context, _errorResponseoptions))
                        {
                            responseBody.Seek(0, SeekOrigin.Begin);
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

                if ((!_errorResponseoptions.HandleProblemDetailResponses && context.Response.ContentType.Contains("application/problem")) || (!_errorResponseoptions.HandleMvcHandledResponses && context.Items.ContainsKey("mvcErrorHandled")) || !_errorResponseoptions.HandleError(context, _errorResponseoptions))
                {
                    return;
                }
            }

            if (context.Response.HasStarted)
            {
                //This is caused by the first write or flush to the response body.
                _logger.ResponseStartedErrorHandler();
                return;
            }

            var factory = _errorResponseoptions.ProblemDetailFactories.ContainsKey(context.Response.StatusCode) ? _errorResponseoptions.ProblemDetailFactories[context.Response.StatusCode] : _errorResponseoptions.DefaultProblemDetailFactory ?? null;

            if(factory == null)
            {
                return;
            }

            var problemDetails = factory(context, _logger);
            if (problemDetails == null)
            {
                return;
            }

            context.Response.OnStarting(_clearCacheHeadersDelegate, context.Response);
            _logger.TransformingStatusCodeToProblemDetails(problemDetails.Status);

            await context.WriteProblemDetailsResultAsync(problemDetails).ConfigureAwait(false);
        }

        private static Task ClearCacheHeaders(object state)
        {
            var headers = ((HttpResponse)state).Headers;
            headers[HeaderNames.CacheControl] = "no-cache";
            headers[HeaderNames.Pragma] = "no-cache";
            headers[HeaderNames.Expires] = "-1";
            headers.Remove(HeaderNames.ETag);
            return Task.CompletedTask;
        }
    }

    public class ProblemDetailsErrorResponseHandlerOptions
    {
        public bool HandleMvcHandledResponses { get; set; } = false;
        public bool HandleProblemDetailResponses { get; set; } = false;
        public bool InterceptResponseStream { get; set; } = true;
        public Func<HttpContext, ProblemDetailsErrorResponseHandlerOptions, bool> HandleError { get; set; } = ((context, options) => ((context.Response.StatusCode >= 400 && options.DefaultProblemDetailFactory != null) || options.ProblemDetailFactories.ContainsKey(context.Response.StatusCode)));

        public delegate ProblemDetails ProblemDetailsFactoryDelegate(HttpContext context, ILogger logger);

        public ProblemDetailsFactoryDelegate DefaultProblemDetailFactory { get; set; } = ((context, logger) =>
        {
            var problemDetails = ProblemDetailsFactory.GetProblemDetails(context, "", context.Response.StatusCode, null);
            return problemDetails;
        });
        public Dictionary<int, ProblemDetailsFactoryDelegate> ProblemDetailFactories { get; set; } = new Dictionary<int, ProblemDetailsFactoryDelegate>() {
         
        };
    }
}