using AspNetCore.Mvc.MvcAsApi.Attributes;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ApplicationModels;
using System;
using System.Linq;

namespace AspNetCore.Mvc.MvcAsApi.Conventions
{
    public class MvcErrorFilterConvention : IActionModelConvention, IApplicationModelConvention
    {
        private readonly MvcErrorFilterConventionOptions _options;

        public MvcErrorFilterConvention(Action<MvcErrorFilterConventionOptions> setupAction = null)
        {
            _options = new MvcErrorFilterConventionOptions();
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

            if((!isApiController))
            {
                var apiErrorFilterAttribute = new MvcErrorFilterAttribute(_options.HandleNonBrowserRequests, _options.MvcErrorOptions);
                action.Filters.Insert(0, apiErrorFilterAttribute);
            }
        }
    }

    public class MvcErrorFilterConventionOptions
    {
        public bool HandleNonBrowserRequests { get; set; } = false;
        public Action<MvcErrorFilterOptions> MvcErrorOptions { get; set; }
    }
}
