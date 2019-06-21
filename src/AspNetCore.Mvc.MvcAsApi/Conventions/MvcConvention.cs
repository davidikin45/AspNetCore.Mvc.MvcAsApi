using Microsoft.AspNetCore.Mvc.ApplicationModels;

namespace AspNetCore.Mvc.MvcAsApi.Conventions
{
    public class MvcConvention : IApplicationModelConvention
    {
        public void Apply(ApplicationModel application)
        {
            new MvcErrorFilterConvention().Apply(application);
            new MvcExceptionFilterConvention().Apply(application);
        }
    }
}
