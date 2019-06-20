using AspNetCore.Mvc.MvcAsApi.Attributes;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ApplicationModels;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using System;
using System.Linq;

namespace AspNetCore.Mvc.MvcAsApi.Conventions
{
    public class ApiErrorFilterConvention : IActionModelConvention, IApplicationModelConvention
    {
        private readonly bool _applyToMvcActions;
        private readonly bool _applyToApiControllerActions;
        private readonly Action<ApiErrorFilterOptions> _setupAction;

        public ApiErrorFilterConvention(bool applyToMvcActions, bool applyToApiControllerActions)
            :this(applyToMvcActions, applyToApiControllerActions, null)
        {

        }

        public ApiErrorFilterConvention(bool applyToMvcActions, bool applyToApiControllerActions, Action<ApiErrorFilterOptions> setupAction)
        {
            _applyToMvcActions = applyToMvcActions;
            _applyToApiControllerActions = applyToApiControllerActions;
            _setupAction = setupAction;
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

            if ((isApiController && _applyToApiControllerActions))
            {
                var apiErrorFilterAttribute = new ApiErrorFilterAttribute(true, _setupAction);

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
            else if((!isApiController && _applyToMvcActions))
            {
                var apiErrorFilterAttribute = new ApiErrorFilterAttribute(false, _setupAction);
                action.Filters.Insert(0, apiErrorFilterAttribute);
            }
        }
    }
}
