using AspNetCore.Mvc.MvcAsApi.Attributes;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ApplicationModels;
using System;
using System.Linq;

namespace AspNetCore.Mvc.MvcAsApi.Conventions
{
    public class ApiExceptionFilterConvention : IActionModelConvention, IApplicationModelConvention
    {
        private readonly ApiExceptionFilterConventionOptions _options;

        public ApiExceptionFilterConvention(Action<ApiExceptionFilterConventionOptions> setupAction)
        {
            _options = new ApiExceptionFilterConventionOptions();
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
                var apiExceptionFilterAttribute = new ApiExceptionFilterAttribute(true, _options.ApiExceptionOptions);
                action.Filters.Add(apiExceptionFilterAttribute);
            }
            else if ((!isApiController && _options.ApplyToMvcActions))
            {
                var apiExceptionFilterAttribute = new ApiExceptionFilterAttribute(false, _options.ApiExceptionOptions);
                action.Filters.Add(apiExceptionFilterAttribute);
            }
        }
    }

    public class ApiExceptionFilterConventionOptions
    {
        public bool ApplyToMvcActions { get; set; } = true;
        public bool ApplyToApiControllerActions { get; set; } = true;
        public Action<ApiExceptionFilterOptions> ApiExceptionOptions { get; set; }
    }
}
