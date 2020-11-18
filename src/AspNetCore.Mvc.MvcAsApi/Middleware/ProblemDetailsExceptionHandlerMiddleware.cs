using AspNetCore.Mvc.MvcAsApi.Extensions;
using AspNetCore.Mvc.MvcAsApi.Factories;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Net.Http.Headers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.ExceptionServices;
using System.Threading.Tasks;

namespace AspNetCore.Mvc.MvcAsApi.Middleware
{
    public class ProblemDetailsExceptionHandlerMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ProblemDetailsExceptionHandlerOptions _options;
        private readonly ILogger _logger;
        private readonly Func<object, Task> _clearCacheHeadersDelegate;

        public ProblemDetailsExceptionHandlerMiddleware(RequestDelegate next, IOptions<ProblemDetailsExceptionHandlerOptions> options, ILogger<ProblemDetailsExceptionHandlerMiddleware> logger)
        {
            _next = next;
            _options = options.Value;
            _logger = logger;
            _clearCacheHeadersDelegate = OnResponseStarting;
        }

        public async Task Invoke(HttpContext context)
        {
            ExceptionDispatchInfo edi = null;
            try
            {
                await _next(context);
            }
            catch (Exception exception)
            {
                // Get the Exception, but don't continue processing in the catch block as its bad for stack usage.
                edi = ExceptionDispatchInfo.Capture(exception);
            }

            await HandleExceptionAsync(context, edi);
        }

        private async Task HandleExceptionAsync(HttpContext context, ExceptionDispatchInfo edi)
        {
            if (!_options.HandleException(context, _options, edi.SourceException))
            {
                return;
            }

            if (context.Response.HasStarted)
            {
                //This is caused by the first write or flush to the response body.
                _logger.ResponseStartedErrorHandler();
                edi.Throw();
            }

            try
            {
                bool showExceptionDetails = false;
                if (edi.SourceException != null && (_options.ShowExceptionDetails || _options.ShowExceptionDetailsDelegate(context, edi.SourceException)))
                {
                    showExceptionDetails = true;
                }

                if (edi.SourceException != null && edi.SourceException is ProblemDetailsException ex)
                {
                    // The user has already provided a valid problem details object.
                    await context.WriteProblemDetailsResultAsync(ex.ProblemDetails).ConfigureAwait(false);
                    return;
                }

                //clear response
                var statusCode = context.Response.StatusCode;
                context.Response.Clear();
                context.Response.StatusCode = statusCode;

                context.Response.OnStarting(_clearCacheHeadersDelegate, context.Response);

                var types = edi.SourceException == null ? new[] { typeof(Exception) } : edi.SourceException.GetType().GetTypeAndInterfaceHierarchy();
                foreach (var type in types)
                {
                    if (_options.ProblemDetailFactories.ContainsKey(type))
                    {
                        var factory = _options.ProblemDetailFactories[type];
                        var problemDetails = factory(context, _logger, edi.SourceException, showExceptionDetails);
                        if (problemDetails != null)
                        {
                            await context.WriteProblemDetailsResultAsync(problemDetails).ConfigureAwait(false);
                        }
                        return;
                    }
                }

                if (_options.DefaultProblemDetailFactory != null)
                {
                    var problemDetails = _options.DefaultProblemDetailFactory(context, _logger, edi.SourceException, showExceptionDetails);
                    if (problemDetails != null)
                    {
                        await context.WriteProblemDetailsResultAsync(problemDetails).ConfigureAwait(false);
                    }

                    return;
                }
            }
            catch (Exception ex2)
            {
                // Suppress secondary exceptions, re-throw the original.
                _logger.ErrorHandlerException(ex2);
            }

            edi.Throw(); // Re-throw the original if we couldn't handle it
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

    public class ProblemDetailsExceptionHandlerOptions
    {
        public bool ShowExceptionDetails { get; set; } = false;

        public Func<HttpContext, Exception, bool> ShowExceptionDetailsDelegate { get; set; } = ((context, exception) => false);

        public Func<HttpContext, ProblemDetailsExceptionHandlerOptions, Exception, bool> HandleException { get; set; } = ((context, options, exception) => options.DefaultProblemDetailFactory != null || exception.GetType().GetTypeAndInterfaceHierarchy().Any(type => options.ProblemDetailFactories.ContainsKey(type)));

        public delegate ProblemDetails ProblemDetailsFactoryDelegate(HttpContext context, ILogger logger, Exception exception, bool showExceptionDetails);

        public ProblemDetailsFactoryDelegate DefaultProblemDetailFactory = ((context, logger, exception, showExceptionDetails) =>
        {
            logger.UnhandledException(exception);

            //UseExceptionHandler logs error automatically
            ProblemDetails problemDetails = null;

#if NETCOREAPP3_0
            var problemDetailsFactory = context.RequestServices.GetService<ProblemDetailsFactory>();
            if (problemDetailsFactory != null)
            {
                problemDetails = problemDetailsFactory.CreateProblemDetails(context, StatusCodes.Status500InternalServerError, showExceptionDetails ? exception.Message : null, null, null, showExceptionDetails ? exception.ToString() : null);
            }
#endif
            if (problemDetails == null)
            {
                problemDetails = StaticProblemDetailsFactory.CreateProblemDetails(context, StatusCodes.Status500InternalServerError, showExceptionDetails ? exception.Message : null, null, showExceptionDetails ? exception.ToString() : null);
            }

            return problemDetails;
        });

        public Dictionary<Type, ProblemDetailsFactoryDelegate> ProblemDetailFactories { get; set; } = new Dictionary<Type, ProblemDetailsFactoryDelegate>()
        {

        };
    }
}