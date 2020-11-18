using AspNetCore.Mvc.MvcAsApi.Extensions;
using AspNetCore.Mvc.MvcAsApi.Factories;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.Extensions.Logging;
using Microsoft.IO;
using Microsoft.Net.Http.Headers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace AspNetCore.Mvc.MvcAsApi.Middleware
{
    //https://github.com/khellang/Middleware/blob/master/src/ProblemDetails/ProblemDetailsMiddleware.cs
    //https://github.com/aspnet/AspNetCore/blob/bbf7ed290786498e20f7ff6e4f21451fa7d58885/src/Middleware/Diagnostics/src/ExceptionHandler/ExceptionHandlerMiddleware.cs
    public class ProblemDetailsErrorResponseHandlerMiddleware
    {
        private readonly RequestDelegate _next;

        private readonly ILogger _logger;
        private readonly ProblemDetailsErrorResponseHandlerOptions _options;
        private readonly RecyclableMemoryStreamManager _recyclableMemoryStreamManager;
        private readonly Func<object, Task> _clearCacheHeadersDelegate;

        public ProblemDetailsErrorResponseHandlerMiddleware(RequestDelegate next, ILogger<ProblemDetailsErrorResponseHandlerMiddleware> logger, IServiceProvider serviceProvider, IOptions<ProblemDetailsErrorResponseHandlerOptions> options)
        {
            _next = next;
            _logger = logger;
            _options = options.Value;
            _recyclableMemoryStreamManager = new RecyclableMemoryStreamManager();
            _clearCacheHeadersDelegate = OnResponseStarting;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            if (_options.HandleContentResponses)
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

                        if (!HandleResponse(context))
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

                //response has now been written to body

                if (!HandleResponse(context))
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

            var factory = _options.ProblemDetailFactories.ContainsKey(context.Response.StatusCode) ? _options.ProblemDetailFactories[context.Response.StatusCode] : _options.DefaultProblemDetailFactory ?? null;

            if (factory == null)
            {
                return;
            }

            var problemDetails = factory(context, _logger);
            if (problemDetails == null)
            {
                return;
            }

            //clear response
            var statusCode = context.Response.StatusCode;
            context.Response.Clear();
            context.Response.StatusCode = statusCode;

            context.Response.OnStarting(_clearCacheHeadersDelegate, context.Response);

            _logger.TransformingStatusCodeToProblemDetails(problemDetails.Status);

            //write problem details
            await context.WriteProblemDetailsResultAsync(problemDetails).ConfigureAwait(false);
        }

        private bool HandleResponse(HttpContext context)
        {
            if (!string.IsNullOrEmpty(context.Response.ContentType) && context.Response.ContentType.Contains("application/problem"))
            {
                if (!_options.HandleProblemDetailResponses)
                {
                    return false;
                }
            }
            else if (!_options.HandleContentResponses && (!string.IsNullOrEmpty(context.Response.ContentType) || context.Response.ContentLength.HasValue))
            {
                return false;
            }
            else if (!_options.HandleNoContentResponses && string.IsNullOrEmpty(context.Response.ContentType))
            {
                return false;
            }

            if (!_options.HandleError(context, _options))
            {
                return false;
            }

            if(_options.IgnoreResponsesWithContextItemKeys.Any(key => context.Items.ContainsKey(key)))
            {
                return false;
            }

            return true;
        }

        private static Task OnResponseStarting(object state)
        {
            var headers = ((HttpResponse)state).Headers;
            headers[HeaderNames.CacheControl] = "no-cache, no-store, must-revalidate";
            headers[HeaderNames.Pragma] = "no-cache";
            headers[HeaderNames.Expires] = "-1";
            headers.Remove(HeaderNames.ETag);
            return Task.CompletedTask;
        }
    }

    public class ProblemDetailsErrorResponseHandlerOptions
    {
        public List<string> IgnoreResponsesWithContextItemKeys { get; set; } = new List<string>()
        {
            "SkipProblemDetailsErrorResponseHandler"
        };

        public bool HandleNoContentResponses { get; set; } = true;
        public bool HandleContentResponses { get; set; } = false;
        public bool HandleProblemDetailResponses { get; set; } = false;
        public Func<HttpContext, ProblemDetailsErrorResponseHandlerOptions, bool> HandleError { get; set; } = ((context, options) => ((context.Response.StatusCode >= 400 && options.DefaultProblemDetailFactory != null) || options.ProblemDetailFactories.ContainsKey(context.Response.StatusCode)));

        public delegate ProblemDetails ProblemDetailsFactoryDelegate(HttpContext context, ILogger logger);

        public ProblemDetailsFactoryDelegate DefaultProblemDetailFactory { get; set; } = ((context, logger) =>
        {
            ProblemDetails problemDetails = null;

#if NETCOREAPP3_0
            var problemDetailsFactory = context.RequestServices.GetService<ProblemDetailsFactory>();
            if (problemDetailsFactory != null)
            {
                problemDetails = problemDetailsFactory.CreateProblemDetails(context, context.Response.StatusCode);
            }
#endif

            if (problemDetails == null)
            {
                problemDetails = StaticProblemDetailsFactory.CreateProblemDetails(context, context.Response.StatusCode);
            }

            return problemDetails;
        });

        public Dictionary<int, ProblemDetailsFactoryDelegate> ProblemDetailFactories { get; set; } = new Dictionary<int, ProblemDetailsFactoryDelegate>()
        {

        };
    }
}