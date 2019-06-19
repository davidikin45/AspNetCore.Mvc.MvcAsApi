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
        private readonly Func<IClientErrorActionResult, bool> _handleError;
        public ApiErrorFilterConvention(bool applyToMvcActions, bool applyToApiControllerActions, Func<IClientErrorActionResult, bool> handleError = null)
        {
            _applyToMvcActions = applyToMvcActions;
            _applyToApiControllerActions = applyToApiControllerActions;
            _handleError = handleError;
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
                var apiErrorFilterAttribute = _handleError == null ? new ApiErrorFilterAttribute(true) : new ApiErrorFilterAttribute(true, _handleError);

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
                var apiErrorFilterAttribute = _handleError == null ? new ApiErrorFilterAttribute(false) : new ApiErrorFilterAttribute(false, _handleError);
                action.Filters.Insert(0, apiErrorFilterAttribute);
            }
        }
    }
}
