using AspNetCore.Mvc.MvcAsApi.Attributes;
using Microsoft.AspNetCore.Mvc.ApplicationModels;
using System;

namespace AspNetCore.Mvc.MvcAsApi.Conventions
{
    public class ApiConvention : IApplicationModelConvention
    {
        private readonly ApiConventionOptions _options;

        public ApiConvention(Action<ApiConventionOptions> setupAction = null)
        {
            _options = new ApiConventionOptions();
            if (setupAction != null)
                setupAction(_options);
        }

        public void Apply(ApplicationModel application)
        {
            new ApiErrorFilterConvention(options => { options.ApplyToMvcActions = false; options.ApplyToApiControllerActions = true; options.ApiErrorOptions = _options.ApiErrorOptions; }).Apply(application);
            //Return problem details in json/xml if an exception is thrown via Api
            new ApiExceptionFilterConvention(options => { options.ApplyToMvcActions = false; options.ApplyToApiControllerActions = true; options.ApiExceptionOptions = _options.ApiExceptionOptions; }).Apply(application);
            //Post data to MVC Controller from API
            new FromBodyAndOtherSourcesConvention(options => { options.ApplyToMvcActions = false; options.ApplyToApiControllerActions = true; options.EnableForParametersWithNoBinding = true; options.EnableForParametersWithFormRouteQueryBinding = true; options.ChangeFromBodyBindingsToFromBodyFormAndRouteQueryBinding = true;}).Apply(application);
            //Return data uisng output formatter when acccept header is application/json or application/xml
            new ConvertViewResultToObjectResultConvention(options => { options.ApplyToMvcActions = false; options.ApplyToApiControllerActions = true;}).Apply(application);
        }
    }

    public class ApiConventionOptions
    {
        public Action<ApiErrorFilterOptions> ApiErrorOptions { get; set; }
        public Action<ApiExceptionFilterOptions> ApiExceptionOptions { get; set; }

    }
}
