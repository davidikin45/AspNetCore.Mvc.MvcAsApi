using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.Extensions.Options;
using System;

namespace AspNetCore.Mvc.MvcAsApi.Factories
{
    public class DelegateClientErrorFactory : IClientErrorFactory
    {
        private readonly ApiBehaviorOptions _options;
        private readonly Func<ApiBehaviorOptions, ActionContext, IClientErrorActionResult, IActionResult> _errorAndExceptionResponseFactory;

        public DelegateClientErrorFactory(IOptions<ApiBehaviorOptions> options, Func<ApiBehaviorOptions, ActionContext, IClientErrorActionResult, IActionResult> errorAndExceptionResponseFactory)
        {
            _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
            _errorAndExceptionResponseFactory = errorAndExceptionResponseFactory;
        }

        public IActionResult GetClientError(ActionContext actionContext, IClientErrorActionResult clientError)
        {
            var result = _errorAndExceptionResponseFactory(_options, actionContext, clientError);
            if (result != null)
            {
                actionContext.HttpContext.Items["mvcErrorHandled"] = true;
            }
            return result;
        }
    }
}
