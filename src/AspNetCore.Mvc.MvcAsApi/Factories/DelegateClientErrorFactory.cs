using AspNetCore.Mvc.MvcAsApi.ActionResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.Extensions.Options;
using System;

namespace AspNetCore.Mvc.MvcAsApi.Factories
{
    public class DelegateClientErrorFactory : IClientErrorFactory
    {
        private readonly ApiBehaviorOptions _options;
        private readonly DelegateClientErrorFactoryOptions _delegateClientErrorFactoryOptions;

        public DelegateClientErrorFactory(IOptions<ApiBehaviorOptions> options, IOptions<DelegateClientErrorFactoryOptions> delegateClientErrorFactoryOptions)
        {
            _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
            _delegateClientErrorFactoryOptions = delegateClientErrorFactoryOptions.Value;
        }

        public IActionResult GetClientError(ActionContext actionContext, IClientErrorActionResult clientError)
        {
            var result = _delegateClientErrorFactoryOptions.ErrorAndExceptionResponseFactory(_options, actionContext, clientError, _delegateClientErrorFactoryOptions.ShowExceptionDetails);
            if (result != null)
            {
                actionContext.HttpContext.Items["mvcErrorHandled"] = true;
            }
            return result;
        }
    }

    public class DelegateClientErrorFactoryOptions
    {
        public Func<ActionContext, Exception, bool> ShowExceptionDetails { get; set; } = ((actionContext, exception) => false);
        public Func<ApiBehaviorOptions, ActionContext, IClientErrorActionResult, Func<ActionContext, Exception, bool>, IActionResult> ErrorAndExceptionResponseFactory { get; set; } = (apiBehaviorOptions, actionContext, clientError, showExceptionDetails) =>
            {
                string detail = null;
                if (clientError is ExceptionResult exceptionResult)
                {
                    if (showExceptionDetails(actionContext, exceptionResult.Error))
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
    }
}
