using AspNetCore.Mvc.MvcAsApi.Extensions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;

namespace AspNetCore.Mvc.MvcAsApi.Middleware
{
    //https://github.com/aspnet/AspNetCore/blob/bbf7ed290786498e20f7ff6e4f21451fa7d58885/src/Middleware/Diagnostics/src/ExceptionHandler/ExceptionHandlerMiddleware.cs

    public static class ProblemDetailsHandlerExtensions
    {
        public static IApplicationBuilder UseProblemDetailsErrorResponseHandler(this IApplicationBuilder app, Action<ProblemDetailsErrorResponseHandlerOptions> configureOptions = null)
        {
            var options = new ProblemDetailsErrorResponseHandlerOptions();
            if (configureOptions != null)
            {
                configureOptions(options);
            }

            return app.UseMiddleware<ProblemDetailsErrorResponseHandlerMiddleware>(Options.Create(options));
        }


        public static IApplicationBuilder UseProblemDetailsExceptionHandler(this IApplicationBuilder app, Action<ProblemDetailsExceptionHandlerOptions> configureOptions = null)
        {
            var options = new ProblemDetailsExceptionHandlerOptions();
            if (configureOptions != null)
            {
                configureOptions(options);
            }

            return app.UseMiddleware<ProblemDetailsErrorResponseHandlerMiddleware>(Options.Create(options));
        }

        public static IApplicationBuilder UseProblemDetailsExceptionHandler2(this IApplicationBuilder app, Action<ProblemDetailsExceptionHandlerOptions> configureOptions = null)
        {
            var options = new ProblemDetailsExceptionHandlerOptions();
            if (configureOptions != null)
            {
                configureOptions(options);
            }

            var loggerFactory = app.ApplicationServices.GetRequiredService<ILoggerFactory>();

            //UseExceptionHandler logs error automatically, Do we want to do this if we are handling them?
            //If a path is set the request is sent back down the pipeline again.
            //https://docs.microsoft.com/en-us/aspnet/core/web-api/handle-errors?view=aspnetcore-5.0
            return app.UseExceptionHandler(HandleException(loggerFactory, options));
        }

        public static Action<IApplicationBuilder> HandleException(ILoggerFactory loggerFactory, ProblemDetailsExceptionHandlerOptions exceptionOptions)
        {
            return appBuilder =>
            {

                //runtime
                appBuilder.Run(async context =>
                {
                    var exceptionHandlerFeature = context.Features.Get<IExceptionHandlerFeature>();
                    var exception = exceptionHandlerFeature?.Error;

                    if (!exceptionOptions.HandleException(context, exceptionOptions, exception))
                    {
                        return;
                    }

                    var logger = loggerFactory.CreateLogger<ExceptionHandlerMiddleware>();

                    bool showExceptionDetails = false;
                    if (exception != null && (exceptionOptions.ShowExceptionDetails || exceptionOptions.ShowExceptionDetailsDelegate(context, exception)))
                    {
                        showExceptionDetails = true;
                    }

                    if (exception != null && exception is ProblemDetailsException ex)
                    {
                        // The user has already provided a valid problem details object.
                        await context.WriteProblemDetailsResultAsync(ex.ProblemDetails).ConfigureAwait(false);
                        return;
                    }

                    var types = exception == null ? new[] { typeof(Exception) } : exception.GetType().GetTypeAndInterfaceHierarchy();
                    foreach (var type in types)
                    {
                        if (exceptionOptions.ProblemDetailFactories.ContainsKey(type))
                        {
                            var factory = exceptionOptions.ProblemDetailFactories[type];
                            var problemDetails = factory(context, logger, exception, showExceptionDetails);
                            if (problemDetails != null)
                            {
                                await context.WriteProblemDetailsResultAsync(problemDetails).ConfigureAwait(false);
                            }
                            return;
                        }
                    }

                    if (exceptionOptions.DefaultProblemDetailFactory != null)
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
}
