using AspNetCore.Mvc.MvcAsApi.Attributes;
using Microsoft.AspNetCore.Mvc.ApplicationModels;
using System;

namespace AspNetCore.Mvc.MvcAsApi.Conventions
{
    public class MvcAsApiConvention : IApplicationModelConvention
    {
        private readonly MvcAsApiConventionOptions _options;

        public MvcAsApiConvention(Action<MvcAsApiConventionOptions> setupAction = null)
        {
            _options = new MvcAsApiConventionOptions();
            if (setupAction != null)
                setupAction(_options);
        }

        public void Apply(ApplicationModel application)
        {
            //Does nothing by default.
            new MvcErrorFilterConvention(options => { options.ApplyToMvcActions = _options.ApplyToMvcActions; options.ApplyToApiControllerActions = _options.ApplyToApiControllerActions; options.MvcErrorOptions = _options.MvcErrorOptions; }).Apply(application);
            //Intercepts OperationCanceledException, all other exceptions are logged/handled by UseExceptionHandler/UseDeveloperExceptionPage.
            new MvcExceptionFilterConvention(options => { options.ApplyToMvcActions = _options.ApplyToMvcActions; options.ApplyToApiControllerActions = _options.ApplyToApiControllerActions; options.MvcExceptionOptions = _options.MvcExceptionOptions; }).Apply(application);
            //Return problem details in json/xml if an error response is returned via Api.
            new ApiErrorFilterConvention(options => { options.ApplyToMvcActions = _options.ApplyToMvcActions; options.ApplyToApiControllerActions = _options.ApplyToApiControllerActions;  options.ApiErrorOptions = _options.ApiErrorOptions; }).Apply(application);
            //Return problem details in json/xml if an exception is thrown via Api
            new ApiExceptionFilterConvention(options => { options.ApplyToMvcActions = _options.ApplyToMvcActions; options.ApplyToApiControllerActions = _options.ApplyToApiControllerActions; options.ApiExceptionOptions = _options.ApiExceptionOptions; }).Apply(application);
            //Post data to MVC Controller from API
            new FromBodyAndOtherSourcesConvention(options => { options.DisableAntiForgeryForApiRequestsInDevelopmentEnvironment = _options.DisableAntiForgeryForApiRequestsInDevelopmentEnvironment; options.DisableAntiForgeryForApiRequestsInAllEnvironments = _options.DisableAntiForgeryForApiRequestsInAllEnvironments; options.ApplyToMvcActions = _options.ApplyToMvcActions; options.ApplyToApiControllerActions = _options.ApplyToApiControllerActions; options.EnableForParametersWithNoBinding = true; options.EnableForParametersWithFormRouteQueryBinding = true; options.ChangeFromBodyBindingsToFromBodyFormAndRouteQueryBinding = true; }).Apply(application);
            //Return data using output formatter when acccept header is application/json or application/xml
            new ConvertViewResultToObjectResultConvention(options => { options.ApplyToMvcActions = _options.ApplyToMvcActions; options.ApplyToApiControllerActions = _options.ApplyToApiControllerActions; }).Apply(application);
        }
    }

    public class MvcAsApiConventionOptions
    {
        public bool ApplyToMvcActions { get; set; } = true;

        public bool ApplyToApiControllerActions { get; set; } = false; //This allows Swagger to show Json Body field rather than query string.

        public Action<MvcErrorFilterOptions> MvcErrorOptions;
        public Action<MvcExceptionFilterOptions> MvcExceptionOptions;

        public Action<ApiErrorFilterOptions> ApiErrorOptions;
        public Action<ApiExceptionFilterOptions> ApiExceptionOptions;

        public bool DisableAntiForgeryForApiRequestsInDevelopmentEnvironment { get; set; } = true;
        public bool DisableAntiForgeryForApiRequestsInAllEnvironments { get; set; } = false;
    }
}
