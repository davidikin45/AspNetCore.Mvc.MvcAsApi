using AspNetCore.Mvc.MvcAsApi.Factories;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using System;

namespace AspNetCore.Mvc.MvcAsApi.Extensions
{
    public static class ProblemDetailsFactoryExtensions
    {
#if NETCOREAPP3_0
        public static IMvcBuilder AddMvcEnhancedProblemDetailsFactory(this IMvcBuilder builder)
        {
            var services = builder.Services;

            services.AddSingleton<ProblemDetailsFactory, EnhancedProblemDetailsFactory>();

            return builder;
        }

        public static IMvcBuilder AddMvcEnhancedProblemDetailsFactory(this IMvcBuilder builder, Action<EnhancedProblemDetailsFactoryOptions> setupAction)
        {
            var services = builder.Services;

            builder.AddMvcEnhancedProblemDetailsFactory();
            services.Configure(setupAction);

            return builder;
        }
#endif
    }
}
