using AspNetCore.Mvc.MvcAsApi.Filters;
using Microsoft.AspNetCore.Mvc.ApplicationModels;
using Microsoft.AspNetCore.Mvc.Filters;
using System;

namespace AspNetCore.Mvc.MvcAsApi.Conventions
{
    public class ApiExceptionFilterConvention : IActionModelConvention, IApplicationModelConvention
    {
        private readonly Func<ExceptionContext, bool> _handleException;
        public ApiExceptionFilterConvention(Func<ExceptionContext, bool> handleException = null)
        {
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
            action.Filters.Add(_handleException == null ? new ApiExceptionFilterAttribute() : new ApiExceptionFilterAttribute(_handleException));
        }
    }
}
