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

            if ((isApiController && _options.ApplyToApiControllerActions))
            {
                var mvcErrorFilterAttribute = new MvcErrorFilterAttribute(_options.MvcErrorOptions);
                action.Filters.Insert(0, mvcErrorFilterAttribute);
            }
            else if ((!isApiController && _options.ApplyToMvcActions))
            {
                var mvcErrorFilterAttribute = new MvcErrorFilterAttribute(_options.MvcErrorOptions);
                action.Filters.Insert(0, mvcErrorFilterAttribute);
            }
        }
    }

    public class MvcErrorFilterConventionOptions
    {
        public bool ApplyToMvcActions { get; set; } = true;

        public bool ApplyToApiControllerActions { get; set; } = true;
        public Action<MvcErrorFilterOptions> MvcErrorOptions { get; set; }
    }
}
