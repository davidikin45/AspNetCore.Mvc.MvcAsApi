using AspNetCore.Mvc.MvcAsApi.Extensions;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace AspNetCore.Mvc.MvcAsApi.Attributes
{
    //IgnoreAntiforgeryTokenAttribute
    // ValidateAntiforgeryTokenAttribute relies on order to determine if it's the effective policy.
    // When two antiforgery filters of the same order are added to the application model, the effective policy is determined
    // by whatever appears later in the list (closest to the action).

    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
    public class AutoValidateFormAntiforgeryTokenAttribute : TypeFilterAttribute, IOrderedFilter
    {

        public new int Order { get; set; } = 1000;
        public new bool IsReusable => true;

        public AutoValidateFormAntiforgeryTokenAttribute(bool enableForAllEnvironments = false) : base(typeof(AutoValidateFormAntiforgeryTokenAuthorizationFilterImpl))
        {
            Arguments = new object[] { enableForAllEnvironments };
        }

        private class AutoValidateFormAntiforgeryTokenAuthorizationFilterImpl : IAsyncAuthorizationFilter, IAntiforgeryPolicy
        {
            private readonly IAntiforgery _antiforgery;
            private readonly ILogger _logger;
            private readonly IHostingEnvironment _environment;

            private readonly bool _enableForAllEnvironments;

            public AutoValidateFormAntiforgeryTokenAuthorizationFilterImpl(IAntiforgery antiforgery, ILoggerFactory loggerFactory, IHostingEnvironment environment, bool enableForAllEnvironments)
            {
                if (antiforgery == null)
                {
                    throw new ArgumentNullException(nameof(antiforgery));
                }

                _antiforgery = antiforgery;
                _logger = loggerFactory.CreateLogger(GetType());
                _environment = environment;
                _enableForAllEnvironments = enableForAllEnvironments;
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

                //POST, PUT, PATCH and DELETE
                //!context.HttpContext.Request.HasFormContentType
                if ((_enableForAllEnvironments || _environment.IsDevelopment()) && context.HttpContext.Request.IsApi())
                {
                    return false;
                }

                // Anything else requires a token.
                return true;
            }
        }
    }
}
