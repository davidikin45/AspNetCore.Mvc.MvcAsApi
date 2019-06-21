using AspNetCore.Mvc.MvcAsApi.Attributes;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ApplicationModels;
using System;
using System.Linq;

namespace AspNetCore.Mvc.MvcAsApi.Conventions
{
    public class ConvertViewResultToObjectResultConvention : IActionModelConvention, IApplicationModelConvention
    {
        private readonly ConvertViewResultToObjectResultConventionOptions _options;

        public ConvertViewResultToObjectResultConvention(Action<ConvertViewResultToObjectResultConventionOptions> setupAction = null)
        {
            _options = new ConvertViewResultToObjectResultConventionOptions();
            if (setupAction != null)
                setupAction(_options);
        }

        public void Apply(ApplicationModel application)
        {
            foreach (var controller in application.Controllers)
            {
                var isApiController = controller.Attributes.OfType<ApiControllerAttribute>().Any();

                if ((isApiController && _options.ApplyToApiControllerActions) || (!isApiController && _options.ApplyToMvcActions))
                {
                    foreach (var action in controller.Actions)
                    {
                        Apply(action);
                    }
                }
            }
        }

        public void Apply(ActionModel action)
        {
            if (!action.Filters.OfType<ConvertViewResultToObjectResultAttribute>().Any())
            {
                action.Filters.Add(new ConvertViewResultToObjectResultAttribute());
            }
        }
    }

    public class ConvertViewResultToObjectResultConventionOptions
    {
        public bool ApplyToMvcActions { get; set; } = true;
        public bool ApplyToApiControllerActions { get; set; } = true;
    }
}
