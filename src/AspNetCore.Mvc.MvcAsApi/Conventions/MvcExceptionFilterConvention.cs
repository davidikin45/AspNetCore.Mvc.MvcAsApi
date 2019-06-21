using AspNetCore.Mvc.MvcAsApi.Attributes;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ApplicationModels;
using System;
using System.Linq;

namespace AspNetCore.Mvc.MvcAsApi.Conventions
{
    public class MvcExceptionFilterConvention : IActionModelConvention, IApplicationModelConvention
    {
        private readonly MvcExceptionFilterConventionOptions _options;

        public MvcExceptionFilterConvention(Action<MvcExceptionFilterConventionOptions> setupAction)
        {
            _options = new MvcExceptionFilterConventionOptions();
            if (setupAction != null)
                setupAction(_options);
        }

        public void Apply(ApplicationModel application)
        {
            foreach (var controller in application.Controllers)
            {
                foreach (var action in controller.Actions)
                {
                    Apply(action);
                }
            }
        }

        public void Apply(ActionModel action)
        {
            var isApiController = action.Controller.Attributes.OfType<ApiControllerAttribute>().Any();

            if ((!isApiController))
            {
                var apiExceptionFilterAttribute = new MvcExceptionFilterAttribute(_options.HandleNonBrowserRequests, _options.MvcExceptionOptions);
                action.Filters.Add(apiExceptionFilterAttribute);
            }
        }
    }

    public class MvcExceptionFilterConventionOptions
    {
        public bool HandleNonBrowserRequests { get; set; } = false;
        public Action<MvcExceptionFilterOptions> MvcExceptionOptions { get; set; }
    }
}
