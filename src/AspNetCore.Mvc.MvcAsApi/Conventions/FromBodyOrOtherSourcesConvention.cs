using AspNetCore.Mvc.MvcAsApi.Attributes;
using AspNetCore.Mvc.MvcAsApi.BindingSources;
using AspNetCore.Mvc.MvcAsApi.ModelBinding;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ApplicationModels;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using System.Linq;

namespace AspNetCore.Mvc.MvcAsApi.Conventions
{
    public class FromBodyOrOtherSourcesConvention : IParameterModelConvention, IActionModelConvention, IControllerModelConvention, IApplicationModelConvention
    {
        private readonly bool _enableForParametersWithNoBinding;
        private readonly bool _enableForParametersWithFormRouteQueryBinding;
        private readonly bool _changeFromBodyBindingsToFromBodyOrFormRouteQueryBinding;

        public FromBodyOrOtherSourcesConvention(bool enableForParametersWithNoBinding, bool enableForParametersWithFormRouteQueryBinding,  bool changeFromBodyBindingsToFromBodyOrFormRouteQueryBinding)
        {
            _enableForParametersWithNoBinding = enableForParametersWithNoBinding;
            _enableForParametersWithFormRouteQueryBinding = enableForParametersWithFormRouteQueryBinding;
            _changeFromBodyBindingsToFromBodyOrFormRouteQueryBinding = changeFromBodyBindingsToFromBodyOrFormRouteQueryBinding;
        }

        public void Apply(ApplicationModel application)
        {
            foreach (var controler in application.Controllers)
            {
                foreach (var action in controler.Actions)
                {
                    foreach (var paramater in action.Parameters)
                    {
                        Apply(paramater);
                    }
                }
            }
        }

        public void Apply(ActionModel action)
        {
            var antiForgeryTokenFilters = action.Filters.Where(f => f is ValidateAntiForgeryTokenAttribute || f is AutoValidateAntiforgeryTokenAttribute).ToList();
            if(antiForgeryTokenFilters.Any())
            {
                antiForgeryTokenFilters.ForEach(af => action.Filters.Remove(af));
                action.Filters.Add(new AutoValidateFormAntiforgeryTokenAttribute());
            }

            Apply(action.Controller);
        }

        public void Apply(ControllerModel controller)
        {

            var antiForgeryTokenFilters = controller.Filters.Where(f => f is ValidateAntiForgeryTokenAttribute || f is AutoValidateAntiforgeryTokenAttribute).ToList();
            if (antiForgeryTokenFilters.Any())
            {
                antiForgeryTokenFilters.ForEach(af => controller.Filters.Remove(af));
                controller.Filters.Add(new AutoValidateFormAntiforgeryTokenAttribute());
            }
        }

        public void Apply(ParameterModel parameter)
        {
            //A parameter can only have one binding ource so to allow multiple you need to not set a bindingsource.

            if (parameter.BindingInfo == null)
            {
                if(_enableForParametersWithNoBinding)
                {
                    parameter.BindingInfo = new BindingInfo();
                    parameter.BindingInfo.BinderType = typeof(BodyOrOtherSourcesModelBinder);

                    Apply(parameter.Action);
                }
            }
            else if (parameter.BindingInfo.BinderType == null && (parameter.BindingInfo.BindingSource == null || parameter.BindingInfo.BindingSource == BindingSource.Form || parameter.BindingInfo.BindingSource == BindingSource.Path || parameter.BindingInfo.BindingSource == BindingSource.Query || parameter.BindingInfo.BindingSource == BindingSource.ModelBinding))
            {
                if (_enableForParametersWithFormRouteQueryBinding)
                {

                    if(parameter.BindingInfo.BindingSource == BindingSource.Form)
                        parameter.BindingInfo.BindingSource = BodyOrBindingSource.BodyOrForm;
                    else if(parameter.BindingInfo.BindingSource == BindingSource.Path)
                        parameter.BindingInfo.BindingSource = BodyOrBindingSource.BodyOrPath;
                    else if (parameter.BindingInfo.BindingSource == BindingSource.Query)
                        parameter.BindingInfo.BindingSource = BodyOrBindingSource.BodyOrQuery;
                    else if (parameter.BindingInfo.BindingSource == BindingSource.ModelBinding)
                        parameter.BindingInfo.BindingSource = BodyOrBindingSource.BodyOrModelBinding;

                    parameter.BindingInfo.BinderType = typeof(BodyOrOtherSourcesModelBinder);

                    Apply(parameter.Action);
                }
           }
            else if(parameter.BindingInfo.BinderType == null && parameter.BindingInfo.BindingSource == BindingSource.Body)
            {
                if(_changeFromBodyBindingsToFromBodyOrFormRouteQueryBinding)
                {
                    parameter.BindingInfo.BindingSource = BodyOrBindingSource.BodyOrModelBinding;
                    parameter.BindingInfo.BinderType = typeof(BodyOrOtherSourcesModelBinder);

                    Apply(parameter.Action);
                }
            }
        }
    }
}
