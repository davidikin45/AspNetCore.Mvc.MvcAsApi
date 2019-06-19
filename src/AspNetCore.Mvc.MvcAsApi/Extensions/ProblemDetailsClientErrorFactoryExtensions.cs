﻿using AspNetCore.Mvc.MvcAsApi.ActionResults;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using System;
using AspNetCore.Mvc.MvcAsApi.Factories;

namespace AspNetCore.Mvc.MvcAsApi.Extensions
{
    //https://github.com/aspnet/AspNetCore/issues/4953

    //https://github.com/aspnet/AspNetCore/blob/c565386a3ed135560bc2e9017aa54a950b4e35dd/src/Mvc/Mvc.Core/src/ApplicationModels/ApiBehaviorApplicationModelProvider.cs

    //Errors
    //https://github.com/aspnet/AspNetCore/blob/f79f2e3b1200f8e672b77583a54e6157e49da9e4/src/Mvc/Mvc.Core/src/ApplicationModels/ClientErrorResultFilterConvention.cs
    //https://github.com/aspnet/AspNetCore/blob/c565386a3ed135560bc2e9017aa54a950b4e35dd/src/Mvc/Mvc.Core/src/Infrastructure/ClientErrorResultFilterFactory.cs
    //https://github.com/aspnet/AspNetCore/blob/c565386a3ed135560bc2e9017aa54a950b4e35dd/src/Mvc/Mvc.Core/src/Infrastructure/ClientErrorResultFilter.cs

    //https://github.com/aspnet/AspNetCore/blob/a8b67a2b98fefedf7de9902f255209110c83c658/src/Middleware/Diagnostics/src/DeveloperExceptionPage/DeveloperExceptionPageMiddleware.cs
    public static class ProblemDetailsClientErrorFactoryExtensions
    {
        public static IServiceCollection AddProblemDetailsClientErrorAndExceptionFactory(this IServiceCollection services, bool showExceptionDetails)
        {
            return AddProblemDetailsClientErrorAndExceptionFactory(services, ((actionContext, exception) => showExceptionDetails));
        }

        public static IServiceCollection AddProblemDetailsClientErrorAndExceptionFactory(this IServiceCollection services, Func<ActionContext, Exception, bool> showExceptionDetails)
        {
            Func<ApiBehaviorOptions, ActionContext, IClientErrorActionResult, IActionResult> errorAndExceptionResponseFactory = (apiBehaviorOptions, actionContext, clientError) =>
            {
                string detail = null;
                if (clientError is ExceptionResult exceptionResult)
                {
                    if (showExceptionDetails != null && showExceptionDetails(actionContext, exceptionResult.Error))
                    {
                        detail = exceptionResult.Error.ToString();
                    }
                }

                var problemDetails = ProblemDetailsFactory.GetProblemDetails(actionContext.HttpContext, "", clientError.StatusCode, detail);

                if (clientError.StatusCode is int statusCode &&
                    apiBehaviorOptions.ClientErrorMapping.TryGetValue(statusCode, out var errorData))
                {
                    problemDetails.Title = errorData.Title;
                    problemDetails.Type = errorData.Link;
                }

                return new ObjectResult(problemDetails)
                {
                    StatusCode = problemDetails.Status,
                    ContentTypes =
                    {
                        "application/problem+json",
                        "application/problem+xml",
                    },
                };
            };

            return AddProblemDetailsClientErrorFactory(services, errorAndExceptionResponseFactory);
        }

        public static IServiceCollection AddProblemDetailsClientErrorFactory(this IServiceCollection services, Func<ApiBehaviorOptions, ActionContext, IClientErrorActionResult, IActionResult> errorAndExceptionResponseFactory)
        {
            return services.AddSingleton<IClientErrorFactory>(sp => new DelegateClientErrorFactory(sp.GetRequiredService<IOptions<ApiBehaviorOptions>>(), errorAndExceptionResponseFactory));
        }
    }
}