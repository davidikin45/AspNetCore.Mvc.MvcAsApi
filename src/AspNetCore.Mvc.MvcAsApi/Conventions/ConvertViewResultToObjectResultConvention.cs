using AspNetCore.Mvc.MvcAsApi.Attributes;
using Microsoft.AspNetCore.Mvc.ApplicationModels;

namespace AspNetCore.Mvc.MvcAsApi.Conventions
{
    public class ConvertViewResultToObjectResultConvention : IActionModelConvention, IApplicationModelConvention
    {
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
            action.Filters.Add(new ConvertViewResultToObjectResultAttribute());
        }
    }
}
