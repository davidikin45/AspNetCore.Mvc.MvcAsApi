using AspNetCore.Mvc.MvcAsApi.Filters;
using Microsoft.AspNetCore.Mvc.ApplicationModels;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using System;

namespace AspNetCore.Mvc.MvcAsApi.Conventions
{
    public class ApiErrorFilterConvention : IActionModelConvention, IApplicationModelConvention
    {
        private readonly Func<IClientErrorActionResult, bool> _handleError;
        public ApiErrorFilterConvention(Func<IClientErrorActionResult, bool> handleError = null)
        {
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
            action.Filters.Add(_handleError == null ? new ApiErrorFilterAttribute() : new ApiErrorFilterAttribute(_handleError));
        }
    }
}
