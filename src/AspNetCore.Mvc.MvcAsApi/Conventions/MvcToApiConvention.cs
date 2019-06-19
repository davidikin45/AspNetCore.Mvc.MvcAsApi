using Microsoft.AspNetCore.Mvc.ApplicationModels;

namespace AspNetCore.Mvc.MvcAsApi.Conventions
{
    public class MvcAsApiConvention : IApplicationModelConvention
    {
        public void Apply(ApplicationModel application)
        {
            new ApiErrorFilterConvention(true, true).Apply(application);
            //Return problem details in json/xml if an exception is thrown via Api
            new ApiExceptionFilterConvention(true, true).Apply(application);
            //Post data to MVC Controller from API
            new FromBodyAndOtherSourcesConvention(true, true, true).Apply(application);
            //Return data uisng output formatter when acccept header is application/json or application/xml
            new ConvertViewResultToObjectResultConvention().Apply(application);
        }
    }
}
