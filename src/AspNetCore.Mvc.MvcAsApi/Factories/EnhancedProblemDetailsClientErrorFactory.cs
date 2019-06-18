using AspNetCore.Mvc.MvcAsApi.ActionResults;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using System;

namespace AspNetCore.Mvc.MvcAsApi.Factories
{
    //https://github.com/aspnet/AspNetCore/issues/4953

    //https://github.com/aspnet/AspNetCore/blob/c565386a3ed135560bc2e9017aa54a950b4e35dd/src/Mvc/Mvc.Core/src/ApplicationModels/ApiBehaviorApplicationModelProvider.cs

    //Errors
    //https://github.com/aspnet/AspNetCore/blob/f79f2e3b1200f8e672b77583a54e6157e49da9e4/src/Mvc/Mvc.Core/src/ApplicationModels/ClientErrorResultFilterConvention.cs
    //https://github.com/aspnet/AspNetCore/blob/c565386a3ed135560bc2e9017aa54a950b4e35dd/src/Mvc/Mvc.Core/src/Infrastructure/ClientErrorResultFilterFactory.cs
    //https://github.com/aspnet/AspNetCore/blob/c565386a3ed135560bc2e9017aa54a950b4e35dd/src/Mvc/Mvc.Core/src/Infrastructure/ClientErrorResultFilter.cs

    //https://github.com/aspnet/AspNetCore/blob/a8b67a2b98fefedf7de9902f255209110c83c658/src/Middleware/Diagnostics/src/DeveloperExceptionPage/DeveloperExceptionPageMiddleware.cs
    public static class ProblemDetailsServiceCollectionExtensions
    {
        public static IServiceCollection AddEnhancedProblemDetailsClientErrorFactory(this IServiceCollection services, bool showExceptionDetails)
        {
            return AddEnhancedProblemDetailsClientErrorFactory(services, ((actionContext, exception) => showExceptionDetails));
        }

        public static IServiceCollection AddEnhancedProblemDetailsClientErrorFactory(this IServiceCollection services, Func<ActionContext, Exception, bool> showExceptionDetails)
        {
            return services.AddSingleton<IClientErrorFactory>(sp => new EnhancedProblemDetailsClientErrorFactory(sp.GetRequiredService<IOptions<ApiBehaviorOptions>>(), showExceptionDetails));
        }
    }

    public class EnhancedProblemDetailsClientErrorFactory : IClientErrorFactory
    {
        private readonly ApiBehaviorOptions _options;
        private readonly Func<ActionContext, Exception, bool> _showExceptionDetails;
        public EnhancedProblemDetailsClientErrorFactory(IOptions<ApiBehaviorOptions> options, Func<ActionContext, Exception, bool> showExceptionDetails)
        {
            _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
            _showExceptionDetails = showExceptionDetails;
        }

        public IActionResult GetClientError(ActionContext actionContext, IClientErrorActionResult clientError)
        {
            string detail = null;
            if (clientError is ExceptionResult exceptionResult)
            {
                if(_showExceptionDetails != null && _showExceptionDetails(actionContext, exceptionResult.Error))
                {
                    detail = exceptionResult.Error.ToString();
                }
            }

            var problemDetails = ProblemDetailsFactory.GetProblemDetails(actionContext.HttpContext, "", clientError.StatusCode, detail);

            if (clientError.StatusCode is int statusCode &&
                _options.ClientErrorMapping.TryGetValue(statusCode, out var errorData))
            {
                problemDetails.Title = errorData.Title;
                problemDetails.Type = errorData.Link;
            }

            actionContext.HttpContext.Items["mvcErrorHandled"] = true;
            return new ObjectResult(problemDetails)
            {
                StatusCode = problemDetails.Status,
                ContentTypes =
                {
                    "application/problem+json",
                    "application/problem+xml",
                },
            };
        }
    }
}
