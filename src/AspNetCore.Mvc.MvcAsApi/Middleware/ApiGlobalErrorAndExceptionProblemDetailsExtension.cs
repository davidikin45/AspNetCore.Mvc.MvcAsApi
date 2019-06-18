using AspNetCore.Mvc.MvcAsApi.Factories;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using WebApiContrib.Core.Results;

namespace AspNetCore.Mvc.MvcAsApi.Middleware
{
    public static class ApiGlobalErrorAndExceptionProblemDetailsExtension
    {
        public static IApplicationBuilder UseWebApiErrorHandlerProblemDetails(this IApplicationBuilder app, Action<ApiGlobalErrorResponseProblemDetailsOptions> configureOptions = null)
        {
            var options = new ApiGlobalErrorResponseProblemDetailsOptions();
            if(configureOptions != null)
            {
                configureOptions(options);
            }

            return app.UseMiddleware<ApiGlobalErrorResponseProblemDetailsMiddleware>(options);
        }

        public static IApplicationBuilder UseWebApiExceptionHandlerProblemDetails(this IApplicationBuilder app, bool showExceptionDetails)
        {
            return UseWebApiExceptionHandlerProblemDetails(app, ((options) => options.showExceptionDetails = ((context) => showExceptionDetails)));
        }

       public static IApplicationBuilder UseWebApiExceptionHandlerProblemDetails(this IApplicationBuilder app, Action<ApiGlobalExceptionProblemDetailsOptions> configureOptions = null)
        {
            var options = new ApiGlobalExceptionProblemDetailsOptions();
            if (configureOptions != null)
            {
                configureOptions(options);
            }

            var loggerFactory = app.ApplicationServices.GetRequiredService<ILoggerFactory>();

            var apiOptions = app.ApplicationServices.GetService<IOptions<ApiBehaviorOptions>>();

            return app.UseExceptionHandler(HandleApiException(loggerFactory, apiOptions?.Value, options));
        }

        public static Action<IApplicationBuilder> HandleApiException(ILoggerFactory loggerFactory, ApiBehaviorOptions options, ApiGlobalExceptionProblemDetailsOptions exceptionOptions)
        {
            return appBuilder =>
            {
                appBuilder.Run(async context =>
                {
                    if (!exceptionOptions.handleException(context))
                    {
                        return;
                    }

                    var exceptionHandlerFeature = context.Features.Get<IExceptionHandlerFeature>();

                    var logger = loggerFactory.CreateLogger("Global Api exception logger");

                    if (exceptionHandlerFeature != null)
                    {
                        logger.LogError(exceptionHandlerFeature.Error, "Api error has occured.");

                        bool showException = false;
                        if(exceptionOptions.showExceptionDetails(context))
                        {
                            showException = true;
                        }

                        var problemDetails = ProblemDetailsFactory.GetProblemDetails(context, "An error has occured.", StatusCodes.Status500InternalServerError, showException ? exceptionHandlerFeature.Error.ToString() : null);

                        if (options != null && options.ClientErrorMapping.TryGetValue(StatusCodes.Status500InternalServerError, out var errorData))
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
                    else
                    {
                        logger.LogError("Api error has occured.");

                        var problemDetails = ProblemDetailsFactory.GetProblemDetails(context, "An error has occured.", StatusCodes.Status500InternalServerError, null);

                        if (options != null && options.ClientErrorMapping.TryGetValue(StatusCodes.Status500InternalServerError, out var errorData))
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
                });
            };
        }
    }

    public class ApiGlobalExceptionProblemDetailsOptions
    {
        public Func<HttpContext, bool> showExceptionDetails { get; set; } = ((context) => false);
        public Func<HttpContext, bool> handleException { get; set; } = ((context) => true);
    }
}
