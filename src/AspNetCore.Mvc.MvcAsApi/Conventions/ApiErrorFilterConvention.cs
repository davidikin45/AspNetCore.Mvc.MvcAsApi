using AspNetCore.Mvc.MvcAsApi.Attributes;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ApplicationModels;
using System;
using System.Linq;

namespace AspNetCore.Mvc.MvcAsApi.Conventions
{
    public class ApiErrorFilterConvention : IActionModelConvention, IApplicationModelConvention
    {
        private readonly ApiErrorFilterConventionOptions _options;

        public ApiErrorFilterConvention(Action<ApiErrorFilterConventionOptions> setupAction = null)
        {
            _options = new ApiErrorFilterConventionOptions();
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
                var apiErrorFilterAttribute = new ApiErrorFilterAttribute(true, _options.ApiErrorOptions);

                var clientErrorResultFilter = action.Filters.Where(f => f.GetType().Name == "ClientErrorResultFilterFactory").FirstOrDefault();
                var clientErrorResultFilterIndex = action.Filters.IndexOf(clientErrorResultFilter);

                if(clientErrorResultFilterIndex >= 0)
                {
                    action.Filters[clientErrorResultFilterIndex] = apiErrorFilterAttribute;
                }
                else
                {
                    action.Filters.Insert(0, apiErrorFilterAttribute);
                }
            }
            else if((!isApiController && _options.ApplyToMvcActions))
            {
                var apiErrorFilterAttribute = new ApiErrorFilterAttribute(false, _options.ApiErrorOptions);
                action.Filters.Insert(0, apiErrorFilterAttribute);
            }
        }
    }

    public class ApiErrorFilterConventionOptions
    {
        public bool ApplyToMvcActions { get; set; } = true;
        public bool ApplyToApiControllerActions { get; set; } = true;
        public Action<ApiErrorFilterOptions> ApiErrorOptions { get; set; }
    }
}
