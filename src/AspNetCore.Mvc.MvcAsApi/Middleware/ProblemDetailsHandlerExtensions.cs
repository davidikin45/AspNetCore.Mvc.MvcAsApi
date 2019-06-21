using AspNetCore.Mvc.MvcAsApi.Extensions;
using AspNetCore.Mvc.MvcAsApi.Factories;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;

namespace AspNetCore.Mvc.MvcAsApi.Middleware
{
    //https://github.com/aspnet/AspNetCore/blob/bbf7ed290786498e20f7ff6e4f21451fa7d58885/src/Middleware/Diagnostics/src/ExceptionHandler/ExceptionHandlerMiddleware.cs

    public static class ProblemDetailsHandlerExtensions
    {
        public static IApplicationBuilder UseProblemDetailsErrorResponseHandler(this IApplicationBuilder app, Action<ProblemDetailsErrorResponseHandlerOptions> configureOptions = null)
        {
            var options = new ProblemDetailsErrorResponseHandlerOptions();
            if(configureOptions != null)
            {
                configureOptions(options);
            }


            return app.UseMiddleware<ProblemDetailsErrorResponseHandlerMiddleware>(options);
        }

        public static IApplicationBuilder UseProblemDetailsExceptionHandler(this IApplicationBuilder app, bool showExceptionDetails)
        {
            return UseProblemDetailsExceptionHandler(app, ((options) => options.ShowExceptionDetails = ((context, exception) => showExceptionDetails)));
        }

       public static IApplicationBuilder UseProblemDetailsExceptionHandler(this IApplicationBuilder app, Action<ProblemDetailsExceptionHandlerOptions> configureOptions = null)
        {
            var options = new ProblemDetailsExceptionHandlerOptions();
            if (configureOptions != null)
            {
                configureOptions(options);
            }

            var loggerFactory = app.ApplicationServices.GetRequiredService<ILoggerFactory>();

            //UseExceptionHandler logs error automatically
            return app.UseExceptionHandler(HandleException(loggerFactory, options));
        }

        public static Action<IApplicationBuilder> HandleException(ILoggerFactory loggerFactory, ProblemDetailsExceptionHandlerOptions exceptionOptions)
        {
            return appBuilder =>
            {
                appBuilder.Run(async context =>
                {
                    var exceptionHandlerFeature = context.Features.Get<IExceptionHandlerFeature>();
                    var exception = exceptionHandlerFeature?.Error;

                    if (!exceptionOptions.HandleException(context, exceptionOptions, exception))
                    {
                        return;
                    }

                    bool showExceptionDetails = false;
                    if(exception != null && exceptionOptions.ShowExceptionDetails(context, exception))
                    {
                        showExceptionDetails = true;
                    }

                    var logger = loggerFactory.CreateLogger<ExceptionHandlerMiddleware>();

                    var types = exception == null ? new[] { typeof(Exception) } : exception.GetType().GetTypeAndInterfaceHierarchy();
                    foreach (var type in types)
                    {
                        if (exceptionOptions.ProblemDetailFactories.ContainsKey(type))
                        {
                            var factory = exceptionOptions.ProblemDetailFactories[type];
                            var problemDetails = factory(context, logger, exception, showExceptionDetails);
                            if(problemDetails != null)
                            {
                                await context.WriteProblemDetailsResultAsync(problemDetails).ConfigureAwait(false);
                            }
                            return;
                        }
                    }

                    if(exceptionOptions.DefaultProblemDetailFactory != null)
                    {
                        var problemDetails = exceptionOptions.DefaultProblemDetailFactory(context, logger, exception, showExceptionDetails);
                        if (problemDetails != null)
                        {
                            await context.WriteProblemDetailsResultAsync(problemDetails).ConfigureAwait(false);
                        }

                        return;
                    }
                });
            };
        }
    }


    public class ProblemDetailsExceptionHandlerOptions
    {
        public Func<HttpContext, Exception, bool> ShowExceptionDetails { get; set; } = ((context, exception) => false);

        public Func<HttpContext, ProblemDetailsExceptionHandlerOptions, Exception, bool> HandleException { get; set; } = ((context, options, exception) => options.DefaultProblemDetailFactory != null || exception.GetType().GetTypeAndInterfaceHierarchy().Any(type => options.ProblemDetailFactories.ContainsKey(type)));

        public delegate ProblemDetails ProblemDetailFactory(HttpContext context, ILogger logger, Exception exception, bool showExceptionDetails);

        public ProblemDetailFactory DefaultProblemDetailFactory = ((context, logger, exception, showExceptionDetails) =>
        {
            //UseExceptionHandler logs error automatically
            var problemDetails = ProblemDetailsTraceFactory.GetProblemDetails(context, "An error has occured.", StatusCodes.Status500InternalServerError, showExceptionDetails ? exception.ToString() : null);
            return problemDetails;
        });

        public Dictionary<Type, ProblemDetailFactory> ProblemDetailFactories { get; set; } = new Dictionary<Type, ProblemDetailFactory>() {

        };
    }
}
