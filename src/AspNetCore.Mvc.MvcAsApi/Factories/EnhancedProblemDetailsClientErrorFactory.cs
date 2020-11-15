using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using System;

namespace AspNetCore.Mvc.MvcAsApi.Factories
{
    public class EnhancedProblemDetailsClientErrorFactory : IClientErrorFactory
    {
        private readonly ApiBehaviorOptions _options;
        private readonly EnhancedClientErrorFactoryOptions _enhancedClientErrorFactoryOptions;

        public EnhancedProblemDetailsClientErrorFactory(IOptions<ApiBehaviorOptions> options, IOptions<EnhancedClientErrorFactoryOptions> enhancedClientErrorFactoryOptions)
        {
            _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
            _enhancedClientErrorFactoryOptions = enhancedClientErrorFactoryOptions.Value;
        }

        public IActionResult GetClientError(ActionContext actionContext, IClientErrorActionResult clientError)
        {
            var result = _enhancedClientErrorFactoryOptions.DefaultErrorAndExceptionResponseFactory(_options, actionContext, clientError);
            if (result != null)
            {
                actionContext.HttpContext.Items["MvcErrorHandled"] = true;
            }

            return result;
        }
    }

    public class EnhancedClientErrorFactoryOptions
    {
        public Func<ActionContext, Exception, bool> ShowExceptionDetailsDelegate { get; set; } = ((actionContext, exception) => false);

        public Func<ApiBehaviorOptions, ActionContext, IClientErrorActionResult, IActionResult> DefaultErrorAndExceptionResponseFactory { get; set; } = (apiBehaviorOptions, actionContext, clientError) =>
            {
#if NETCOREAPP3_0
                var problemDetailsFactory = actionContext.HttpContext.RequestServices.GetRequiredService<ProblemDetailsFactory>();
                var problemDetails = problemDetailsFactory.CreateProblemDetails(actionContext.HttpContext, clientError.StatusCode);
#else
                var problemDetails = StaticProblemDetailsFactory.CreateProblemDetails(actionContext.HttpContext, clientError.StatusCode);
#endif

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
    }
}
