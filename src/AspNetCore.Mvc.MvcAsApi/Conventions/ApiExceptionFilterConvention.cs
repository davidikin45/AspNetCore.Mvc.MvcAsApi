using AspNetCore.Mvc.MvcAsApi.Attributes;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ApplicationModels;
using System;
using System.Linq;

namespace AspNetCore.Mvc.MvcAsApi.Conventions
{
    public class ApiExceptionFilterConvention : IActionModelConvention, IApplicationModelConvention
    {
        private readonly bool _applyToMvcActions;
        private readonly bool _applyToApiControllerActions;
        private readonly Action<ApiExceptionFilterOptions> _setupAction;

        public ApiExceptionFilterConvention(bool applyToMvcActions, bool applyToApiControllerActions)
            :this(applyToMvcActions, applyToApiControllerActions, null)
        {

        }

        public ApiExceptionFilterConvention(bool applyToMvcActions, bool applyToApiControllerActions, Action<ApiExceptionFilterOptions> setupAction)
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
                var apiExceptionFilterAttribute = new ApiExceptionFilterAttribute(true, _setupAction);
                action.Filters.Add(apiExceptionFilterAttribute);
            }
            else if ((!isApiController && _applyToMvcActions))
            {
                var apiExceptionFilterAttribute = new ApiExceptionFilterAttribute(false, _setupAction);
                action.Filters.Add(apiExceptionFilterAttribute);
            }
        }
    }
}
