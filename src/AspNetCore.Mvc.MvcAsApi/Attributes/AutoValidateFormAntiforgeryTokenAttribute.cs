using AspNetCore.Mvc.MvcAsApi.Extensions;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace AspNetCore.Mvc.MvcAsApi.Attributes
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
    public class AutoValidateFormAntiforgeryTokenAttribute : TypeFilterAttribute, IOrderedFilter
    {

        public new int Order { get; set; } = 1000;
        public new bool IsReusable => true;

        public AutoValidateFormAntiforgeryTokenAttribute() : base(typeof(AutoValidateFormAntiforgeryTokenAuthorizationFilterImpl))
        {

        }

        private class AutoValidateFormAntiforgeryTokenAuthorizationFilterImpl : IAsyncAuthorizationFilter, IAntiforgeryPolicy
        {
            private readonly IAntiforgery _antiforgery;
            private readonly ILogger _logger;

            public AutoValidateFormAntiforgeryTokenAuthorizationFilterImpl(IAntiforgery antiforgery, ILoggerFactory loggerFactory)
            {
                if (antiforgery == null)
                {
                    throw new ArgumentNullException(nameof(antiforgery));
                }

                _antiforgery = antiforgery;
                _logger = loggerFactory.CreateLogger(GetType());
            }

            public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
            {
                if (context == null)
                {
                    throw new ArgumentNullException(nameof(context));
                }

                if (!context.IsEffectivePolicy<IAntiforgeryPolicy>(this))
                {
                    _logger.NotMostEffectiveFilter(typeof(IAntiforgeryPolicy));
                    return;
                }

                if (ShouldValidate(context))
                {
                    try
                    {
                        await _antiforgery.ValidateRequestAsync(context.HttpContext);
                    }
                    catch (AntiforgeryValidationException exception)
                    {
                        _logger.AntiforgeryTokenInvalid(exception.Message, exception);
                        context.Result = new AntiforgeryValidationFailedResult();
                    }
                }
            }

            protected virtual bool ShouldValidate(AuthorizationFilterContext context)
            {
                if (context == null)
                {
                    throw new ArgumentNullException(nameof(context));
                }

                var method = context.HttpContext.Request.Method;
                if (string.Equals("GET", method, StringComparison.OrdinalIgnoreCase) ||
                    string.Equals("HEAD", method, StringComparison.OrdinalIgnoreCase) ||
                    string.Equals("TRACE", method, StringComparison.OrdinalIgnoreCase) ||
                    string.Equals("OPTIONS", method, StringComparison.OrdinalIgnoreCase))
                {
                    return false;
                }

                if (!context.HttpContext.Request.HasFormContentType)
                {
                    return false;
                }

                // Anything else requires a token.
                return true;
            }
        }
    }
}
