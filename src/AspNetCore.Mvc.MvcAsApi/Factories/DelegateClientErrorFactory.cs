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
            bool showExceptionDetails = false;
            Exception exception = null;
            if (clientError is ExceptionResult exceptionResult)
            {
                exception = exceptionResult.Error;
                showExceptionDetails = exception != null && _delegateClientErrorFactoryOptions.ShowExceptionDetails || _delegateClientErrorFactoryOptions.ShowExceptionDetailsDelegate(actionContext, exception);
            }

            var result = _delegateClientErrorFactoryOptions.DefaultErrorAndExceptionResponseFactory(_options, actionContext, clientError, exception, showExceptionDetails);
            if (result != null)
            {
                actionContext.HttpContext.Items["mvcErrorHandled"] = true;
            }

            return result;
        }
    }

    public class DelegateClientErrorFactoryOptions
    {
        public bool ShowExceptionDetails { get; set; } = false;
        public Func<ActionContext, Exception, bool> ShowExceptionDetailsDelegate { get; set; } = ((actionContext, exception) => false);

        public delegate IActionResult ErrorAndExceptionResponseFactoryDelegate(ApiBehaviorOptions coptions, ActionContext actionContext, IClientErrorActionResult clientError, Exception exception, bool showExceptionDetails);

        public ErrorAndExceptionResponseFactoryDelegate DefaultErrorAndExceptionResponseFactory { get; set; } = (apiBehaviorOptions, actionContext, clientError, exception, showExceptionDetails) =>
            {
                string detail = null;
                if (exception != null && showExceptionDetails)
                {
                    detail = exception.ToString();
                }

                var problemDetails = ProblemDetailsFactory.GetProblemDetails(actionContext.HttpContext, "", clientError.StatusCode, detail);

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
