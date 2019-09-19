using AspNetCore.Mvc.MvcAsApi.Attributes;
using AspNetCore.Mvc.MvcAsApi.BindingSources;
using AspNetCore.Mvc.MvcAsApi.ModelBinding;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ApplicationModels;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using System;
using System.Linq;

namespace AspNetCore.Mvc.MvcAsApi.Conventions
{
    public class FromBodyAndOtherSourcesConvention : IParameterModelConvention, IActionModelConvention, IControllerModelConvention, IApplicationModelConvention
    {
        private readonly FromBodyAndOtherSourcesConventionOptions _options;
        public FromBodyAndOtherSourcesConvention(Action<FromBodyAndOtherSourcesConventionOptions> setupAction = null)
        {
            _options = new FromBodyAndOtherSourcesConventionOptions();
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
                        foreach (var paramater in action.Parameters)
                        {
                            Apply(paramater);
                        }
                    }
                }
            }
        }

        public void Apply(ActionModel action)
        {
            if(_options.DisableAntiForgeryForApiRequestsInDevelopmentEnvironment || _options.DisableAntiForgeryForApiRequestsInAllEnvironments)
            {
                if (!action.Filters.OfType<AutoValidateFormAntiforgeryTokenAttribute>().Any())
                {
                    var antiForgeryTokenFilters = action.Filters.Where(f => f is ValidateAntiForgeryTokenAttribute || f is AutoValidateAntiforgeryTokenAttribute).ToList();
                    if (antiForgeryTokenFilters.Any())
                    {
                        antiForgeryTokenFilters.ForEach(af => action.Filters.Remove(af));
                        action.Filters.Add(new AutoValidateFormAntiforgeryTokenAttribute(_options.DisableAntiForgeryForApiRequestsInAllEnvironments));
                    }
                }
            }

            Apply(action.Controller);
        }

        public void Apply(ControllerModel controller)
        {
            if (_options.DisableAntiForgeryForApiRequestsInDevelopmentEnvironment || _options.DisableAntiForgeryForApiRequestsInAllEnvironments)
            {
                if (!controller.Filters.OfType<AutoValidateFormAntiforgeryTokenAttribute>().Any())
                {
                    var antiForgeryTokenFilters = controller.Filters.Where(f => f is ValidateAntiForgeryTokenAttribute || f is AutoValidateAntiforgeryTokenAttribute).ToList();
                    if (antiForgeryTokenFilters.Any())
                    {
                        antiForgeryTokenFilters.ForEach(af => controller.Filters.Remove(af));
                        controller.Filters.Add(new AutoValidateFormAntiforgeryTokenAttribute(_options.DisableAntiForgeryForApiRequestsInAllEnvironments));
                    }
                }
            }
        }

        public void Apply(ParameterModel parameter)
        {
            if (parameter.BindingInfo == null)
            {
                if(_options.EnableForParametersWithNoBinding)
                {
                    parameter.BindingInfo = new BindingInfo();
                    parameter.BindingInfo.BinderType = typeof(BodyAndOtherSourcesModelBinder);

                    Apply(parameter.Action);
                }
            }
            else if (parameter.BindingInfo.BinderType == null && (parameter.BindingInfo.BindingSource == null || parameter.BindingInfo.BindingSource == BindingSource.Form || parameter.BindingInfo.BindingSource == BindingSource.Path || parameter.BindingInfo.BindingSource == BindingSource.Query || parameter.BindingInfo.BindingSource == BindingSource.ModelBinding))
            {
                if (_options.EnableForParametersWithFormRouteQueryBinding)
                {
                    if (parameter.BindingInfo.BindingSource == BindingSource.Form)
                    {
                        parameter.BindingInfo.BindingSource = BodyOrBindingSource.BodyOrForm;
                        parameter.BindingInfo.BinderType = typeof(BodyOrOtherSourcesModelBinder);
                    }
                    else if (parameter.BindingInfo.BindingSource == BindingSource.Path)
                    {
                        parameter.BindingInfo.BindingSource = BodyAndBindingSource.BodyAndPath;
                        parameter.BindingInfo.BinderType = typeof(BodyAndOtherSourcesModelBinder);
                    }
                    else if (parameter.BindingInfo.BindingSource == BindingSource.Query)
                    {
                        parameter.BindingInfo.BindingSource = BodyAndBindingSource.BodyAndQuery;
                        parameter.BindingInfo.BinderType = typeof(BodyAndOtherSourcesModelBinder);
                    }
                    else if (parameter.BindingInfo.BindingSource == BindingSource.ModelBinding)
                    {
                        parameter.BindingInfo.BindingSource = BodyAndBindingSource.BodyAndModelBinding;
                        parameter.BindingInfo.BinderType = typeof(BodyAndOtherSourcesModelBinder);
                    }
                    else
                    {
                        parameter.BindingInfo.BinderType = typeof(BodyAndOtherSourcesModelBinder);
                    }


                    Apply(parameter.Action);
                }
           }
            else if(parameter.BindingInfo.BinderType == null && parameter.BindingInfo.BindingSource == BindingSource.Body)
            {
                if(_options.ChangeFromBodyBindingsToFromBodyFormAndRouteQueryBinding)
                {
                    parameter.BindingInfo.BindingSource = BodyAndBindingSource.BodyAndModelBinding;
                    parameter.BindingInfo.BinderType = typeof(BodyAndOtherSourcesModelBinder);

                    Apply(parameter.Action);
                }
            }
        }
    }

    public class FromBodyAndOtherSourcesConventionOptions
    {
        public bool DisableAntiForgeryForApiRequestsInDevelopmentEnvironment { get; set; } = true;
        public bool DisableAntiForgeryForApiRequestsInAllEnvironments { get; set; } = false;
        public bool ApplyToMvcActions { get; set; } = true;
        public bool ApplyToApiControllerActions { get; set; } = true;

        public bool EnableForParametersWithNoBinding { get; set; } = true;
        public bool EnableForParametersWithFormRouteQueryBinding { get; set; } = true;
        public bool ChangeFromBodyBindingsToFromBodyFormAndRouteQueryBinding { get; set; } = true;
    }
}
