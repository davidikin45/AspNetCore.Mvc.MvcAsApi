using AspNetCore.Mvc.MvcAsApi.Middleware;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using System;

namespace AspNetCore.Mvc.MvcAsApi.Attributes
{
    //When Endpoiint Routing is used. app.UseEndPointRouting(2.2) or app.UseRouting(3.0) filter only gets applied if valid action is matched.
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
    public class ApiGenerateAntiForgeryTokenAttribute : MiddlewareFilterAttribute
    {
        public ApiGenerateAntiForgeryTokenAttribute() : base(typeof(ApiGenerateAntiForgeryTokenPipeline))
        {
            
        }
    }

    public class ApiGenerateAntiForgeryTokenPipeline
    {
        public void Configure(IApplicationBuilder app, IOptions<ApiGenerateAntiForgeryTokenOptions> options)
        {
            app.UseApiGenerateAntiforgeryTokenMiddleware(options.Value);
        }
    }
}
