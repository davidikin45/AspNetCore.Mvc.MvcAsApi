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
            new MvcErrorFilterConvention(options => { options.HandleNonBrowserRequests = false; options.MvcErrorOptions = _options.MvcErrorOptions; }).Apply(application);
            //Intercepts OperationCanceledException, all other exceptions are logged/handled by UseExceptionHandler/UseDeveloperExceptionPage.
            new MvcExceptionFilterConvention(options => { options.HandleNonBrowserRequests = false; options.MvcExceptionOptions = _options.MvcExceptionOptions; }).Apply(application);
            //Return problem details in json/xml if an error response is returned via Api.
            new ApiErrorFilterConvention(options => { options.ApplyToMvcActions = true; options.ApplyToApiControllerActions = true;  options.ApiErrorOptions = _options.ApiErrorOptions; }).Apply(application);
            //Return problem details in json/xml if an exception is thrown via Api
            new ApiExceptionFilterConvention(options => { options.ApplyToMvcActions = true; options.ApplyToApiControllerActions = true; options.ApiExceptionOptions = _options.ApiExceptionOptions; }).Apply(application);
            //Post data to MVC Controller from API
            new FromBodyAndOtherSourcesConvention(options => { options.ApplyToMvcActions = true; options.ApplyToApiControllerActions = true; options.EnableForParametersWithNoBinding = true; options.EnableForParametersWithFormRouteQueryBinding = true; options.ChangeFromBodyBindingsToFromBodyFormAndRouteQueryBinding = true; }).Apply(application);
            //Return data uisng output formatter when acccept header is application/json or application/xml
            new ConvertViewResultToObjectResultConvention(options => { options.ApplyToMvcActions = true; options.ApplyToApiControllerActions = true;}).Apply(application);
        }
    }

    public class MvcAsApiConventionOptions
    {
        public Action<MvcErrorFilterOptions> MvcErrorOptions;
        public Action<MvcExceptionFilterOptions> MvcExceptionOptions;

        public Action<ApiErrorFilterOptions> ApiErrorOptions;
        public Action<ApiExceptionFilterOptions> ApiExceptionOptions;
    }
}
