using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using System;
using System.Runtime.ExceptionServices;
using System.Threading.Tasks;

namespace AspNetCore.Mvc.MvcAsApi.Extensions
{
    public static class ApplicationBuilderExtensions
    {
        public static IApplicationBuilder UseOutbound(this IApplicationBuilder app, Action<IApplicationBuilder> configuration)
        {
            return app.UseOutboundWhen(_ => true, configuration);
        }

        public static IApplicationBuilder UseOutboundWhen(this IApplicationBuilder app, Func<HttpContext, bool> predicate, Action<IApplicationBuilder> configuration)
        {
            var outboundPipeline = app.New();

            outboundPipeline.UseWhen(predicate, appBranch => configuration(appBranch));
            outboundPipeline.Run(context =>
            {
                var exceptionHandlerFeature = context.Features.Get<IExceptionHandlerFeature>();
                var exception = exceptionHandlerFeature?.Error;
                if (exception != null)
                {
                    ExceptionDispatchInfo edi = ExceptionDispatchInfo.Capture(exception);
                    edi.Throw();
                }
                return Task.CompletedTask;
            });

            var outboundRequestDelegate = outboundPipeline.Build();

            app.UseExceptionHandler(appBranch =>
            {
                 appBranch.Run(async context =>
                 {
                     await outboundRequestDelegate(context);
                 });
            });

           return app.Use(async (context, next) =>
            {
                await next.Invoke();

                await outboundRequestDelegate(context);
            });
        }
    }
}
