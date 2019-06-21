using AspNetCore.Mvc.MvcAsApi.Attributes;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ApplicationModels;
using System;
using System.Linq;

namespace AspNetCore.Mvc.MvcAsApi.Conventions
{
    public class MvcExceptionFilterConvention : IActionModelConvention, IApplicationModelConvention
    {
        private readonly Action<ExceptionFilterOptions> _setupAction;

        public MvcExceptionFilterConvention()
            : this(null)
        {

        }

        public MvcExceptionFilterConvention(Action<ExceptionFilterOptions> setupAction)
        {
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

            if ((!isApiController))
            {
                var apiExceptionFilterAttribute = new MvcExceptionFilterAttribute(false, _setupAction);
                action.Filters.Add(apiExceptionFilterAttribute);
            }
        }
    }
}
