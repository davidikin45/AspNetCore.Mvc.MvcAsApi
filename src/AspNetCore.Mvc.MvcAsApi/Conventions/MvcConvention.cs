using AspNetCore.Mvc.MvcAsApi.Attributes;
using Microsoft.AspNetCore.Mvc.ApplicationModels;
using System;

namespace AspNetCore.Mvc.MvcAsApi.Conventions
{
    public class MvcConvention : IApplicationModelConvention
    {
        private readonly MvcConventionOptions _options;

        public MvcConvention(Action<MvcConventionOptions> setupAction = null)
        {
            _options = new MvcConventionOptions();
            if (setupAction != null)
                setupAction(_options);
        }

        public void Apply(ApplicationModel application)
        {
            new MvcErrorFilterConvention(options => { options.HandleNonBrowserRequests = true; options.MvcErrorOptions = _options.MvcErrorOptions; }).Apply(application);
            new MvcExceptionFilterConvention(options => { options.HandleNonBrowserRequests = true; options.MvcExceptionOptions = _options.MvcExceptionOptions; }).Apply(application);
        }
    }

    public class MvcConventionOptions
    {
        public Action<MvcErrorFilterOptions> MvcErrorOptions;
        public Action<MvcExceptionFilterOptions> MvcExceptionOptions;
    }
}
