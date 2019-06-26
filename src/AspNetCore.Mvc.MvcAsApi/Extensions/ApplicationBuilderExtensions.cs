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
                if (context.Items.ContainsKey("OutboundExceptionDispatchInfo"))
                {
                    var edi = (ExceptionDispatchInfo)context.Items["OutboundExceptionDispatchInfo"];
                    context.Items.Remove("OutboundExceptionDispatchInfo");
                    edi.Throw();
                }
                return Task.CompletedTask;
            });

            var outboundHandler = outboundPipeline.Build();

            return app.Use(async (context, next) =>
            {
                try
                {
                    await next.Invoke();
                }
                catch(Exception exception)
                {
                    var edi = ExceptionDispatchInfo.Capture(exception);
                    context.Items.Add("OutboundExceptionDispatchInfo", edi);
                }

                await outboundHandler(context);
            });
        }
    }
}
