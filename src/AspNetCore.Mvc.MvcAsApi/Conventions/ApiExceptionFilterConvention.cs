using AspNetCore.Mvc.MvcAsApi.Attributes;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ApplicationModels;
using Microsoft.AspNetCore.Mvc.Filters;
using System;
using System.Linq;

namespace AspNetCore.Mvc.MvcAsApi.Conventions
{
    public class ApiExceptionFilterConvention : IActionModelConvention, IApplicationModelConvention
    {
        private readonly bool _applyToMvcActions;
        private readonly bool _applyToApiControllerActions;
        private readonly Func<ExceptionContext, bool> _handleException;
        public ApiExceptionFilterConvention(bool applyToMvcActions, bool applyToApiControllerActions, Func<ExceptionContext, bool> handleException = null)
        {
            _applyToMvcActions = applyToMvcActions;
            _applyToApiControllerActions = applyToApiControllerActions;
            _handleException = handleException;
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
                var apiExceptionFilterAttribute = _handleException == null ? new ApiExceptionFilterAttribute(true) : new ApiExceptionFilterAttribute(true, _handleException);
                action.Filters.Add(apiExceptionFilterAttribute);
            }
            else if ((!isApiController && _applyToMvcActions))
            {
                var apiExceptionFilterAttribute = _handleException == null ? new ApiExceptionFilterAttribute(false) : new ApiExceptionFilterAttribute(false, _handleException);
                action.Filters.Add(apiExceptionFilterAttribute);
            }
        }
    }
}
