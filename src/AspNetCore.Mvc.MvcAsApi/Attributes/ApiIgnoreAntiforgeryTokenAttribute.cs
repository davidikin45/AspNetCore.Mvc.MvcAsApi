using AspNetCore.Mvc.MvcAsApi.Extensions;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Options;
using System;

namespace AspNetCore.Mvc.MvcAsApi.Attributes
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
    public class ApiIgnoreAntiforgeryTokenAttribute : TypeFilterAttribute
    {
        public ApiIgnoreAntiforgeryTokenAttribute()
            : base(typeof(ApiIgnoreAntiforgeryTokenAttributeImpl))
        {

        }

        private class ApiIgnoreAntiforgeryTokenAttributeImpl : IAuthorizationFilter, IOrderedFilter
        {
            private readonly IAntiforgery _antiforgery;
            private readonly AntiforgeryOptions _options;

            public ApiIgnoreAntiforgeryTokenAttributeImpl(IAntiforgery antiforgery, IOptions<AntiforgeryOptions> options)
            {
                _antiforgery = antiforgery;
                _options = options.Value;
            }

            public int Order => 900;

            public void OnAuthorization(AuthorizationFilterContext context)
            {
                if (!string.IsNullOrEmpty(_options.HeaderName))
                {
                    bool addAntiforgeryTokens = true;
                    var method = context.HttpContext.Request.Method;
                    if (string.Equals("GET", method, StringComparison.OrdinalIgnoreCase) ||
                        string.Equals("HEAD", method, StringComparison.OrdinalIgnoreCase) ||
                        string.Equals("TRACE", method, StringComparison.OrdinalIgnoreCase) ||
                        string.Equals("OPTIONS", method, StringComparison.OrdinalIgnoreCase))
                    {
                        addAntiforgeryTokens = false;
                    }

                    if (addAntiforgeryTokens && context.HttpContext.Request.Headers[_options.HeaderName].Count == 0 && context.HttpContext.Request.IsApi())
                    {
                        var tokens = _antiforgery.GetAndStoreTokens(context.HttpContext);
                        context.HttpContext.Request.Headers[_options.HeaderName] = tokens.RequestToken;
                    }
                }
            }
        }
    }
}


