using AspNetCore.Mvc.MvcAsApi.Factories;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using System;
using System.Threading.Tasks;
using WebApiContrib.Core.Results;

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
            return services.AddProblemDetailsClientErrorAndExceptionFactory(((actionContext, exception) => showExceptionDetails));
        }

        public static IServiceCollection AddProblemDetailsClientErrorAndExceptionFactory(this IServiceCollection services, Func<ActionContext, Exception, bool> showExceptionDetails)
        {
            return services.AddProblemDetailsClientErrorFactory(options => options.ShowExceptionDetails = showExceptionDetails);
        }

        public static IServiceCollection AddProblemDetailsClientErrorFactory(this IServiceCollection services)
        {
            return services.AddSingleton<IClientErrorFactory, DelegateClientErrorFactory>();
        }

        public static IServiceCollection AddProblemDetailsClientErrorFactory(this IServiceCollection services, Action<DelegateClientErrorFactoryOptions> setupAction)
        {
            services.AddProblemDetailsClientErrorFactory();
            services.Configure(setupAction);
            return services;
        }

        public static async Task WriteProblemDetailsResultAsync(this HttpContext context, ProblemDetails problemDetails)
        {
            var apiBehaviorOptions = context.RequestServices.GetService<IOptions<ApiBehaviorOptions>>()?.Value;

            if (apiBehaviorOptions != null)
            {
                if (problemDetails.Status is int statusCode && apiBehaviorOptions != null && apiBehaviorOptions.ClientErrorMapping.TryGetValue(statusCode, out var errorData))
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
                var message = JsonConvert.SerializeObject(problemDetails);
                context.Response.StatusCode = problemDetails.Status.HasValue ? problemDetails.Status.Value : 400;
                context.Response.ContentType = "application/problem+json";
                await context.Response.WriteAsync(message).ConfigureAwait(false);
            }
        }
    }
}
