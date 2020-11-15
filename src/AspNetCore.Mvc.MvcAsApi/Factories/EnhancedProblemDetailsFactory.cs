using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.Extensions.Options;
using System;

namespace AspNetCore.Mvc.MvcAsApi.Factories
{
#if NETCOREAPP3_0
    public class EnhancedProblemDetailsFactory : ProblemDetailsFactory
    {
        private readonly EnhancedProblemDetailsFactoryOptions _options;

        public EnhancedProblemDetailsFactory(IOptions<EnhancedProblemDetailsFactoryOptions> options)
        {
            _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        }


        public override ProblemDetails CreateProblemDetails(HttpContext httpContext, int? statusCode = null, string title = null, string type = null, string detail = null, string instance = null)
        {
            return StaticProblemDetailsFactory.CreateProblemDetails(httpContext, statusCode, title, type, detail, instance);
        }

        public override ValidationProblemDetails CreateValidationProblemDetails(HttpContext httpContext, ModelStateDictionary modelStateDictionary, int? statusCode = null, string title = null, string type = null, string detail = null, string instance = null)
        {
            return StaticProblemDetailsFactory.CreateValidationProblemDetails(httpContext, modelStateDictionary, statusCode, title, type, detail, instance, _options.EnableAngularErrors);
        }
    }

    public class EnhancedProblemDetailsFactoryOptions
    {
        public bool EnableAngularErrors { get; set; } = false;
    }
#endif
}
