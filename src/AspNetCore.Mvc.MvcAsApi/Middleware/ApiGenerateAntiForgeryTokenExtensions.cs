using Microsoft.AspNetCore.Builder;
using System;

namespace AspNetCore.Mvc.MvcAsApi.Middleware
{
    public static class ApiGenerateAntiForgeryTokenExtensions
    {
        public static IApplicationBuilder UseApiGenerateAntiforgeryTokenMiddleware(this IApplicationBuilder app, Action<ApiGenerateAntiForgeryTokenOptions> configureOptions = null)
        {
            var options = new ApiGenerateAntiForgeryTokenOptions();
            if (configureOptions != null)
            {
                configureOptions(options);
            }

            return app.UseApiGenerateAntiforgeryTokenMiddleware(options);
        }

        public static IApplicationBuilder UseApiGenerateAntiforgeryTokenMiddleware(this IApplicationBuilder app, ApiGenerateAntiForgeryTokenOptions options)
        {
            return app.UseMiddleware<ApiGenerateAntiForgeryTokenMiddleware>(options);
        }
    }
}
