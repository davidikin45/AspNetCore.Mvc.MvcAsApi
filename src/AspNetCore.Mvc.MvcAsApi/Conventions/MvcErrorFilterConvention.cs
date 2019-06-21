using AspNetCore.Mvc.MvcAsApi.Attributes;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ApplicationModels;
using System;
using System.Linq;

namespace AspNetCore.Mvc.MvcAsApi.Conventions
{
    public class MvcErrorFilterConvention : IActionModelConvention, IApplicationModelConvention
    {

        private readonly Action<MvcErrorFilterOptions> _setupAction;

        public MvcErrorFilterConvention()
            :this(null)
        {

        }

        public MvcErrorFilterConvention(Action<MvcErrorFilterOptions> setupAction)
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

            if((!isApiController))
            {
                var apiErrorFilterAttribute = new MvcErrorFilterAttribute(false, _setupAction);
                action.Filters.Insert(0, apiErrorFilterAttribute);
            }
        }
    }
}
