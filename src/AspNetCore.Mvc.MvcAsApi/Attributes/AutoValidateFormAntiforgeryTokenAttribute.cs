using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.ViewFeatures.Internal;
using Microsoft.Extensions.Logging;
using System;

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

        private class AutoValidateFormAntiforgeryTokenAuthorizationFilterImpl : ValidateAntiforgeryTokenAuthorizationFilter
        {
            public AutoValidateFormAntiforgeryTokenAuthorizationFilterImpl(IAntiforgery antiforgery, ILoggerFactory loggerFactory)
                : base(antiforgery, loggerFactory)
            {
            }

            protected override bool ShouldValidate(AuthorizationFilterContext context)
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
