using AspNetCore.Mvc.MvcAsApi.Factories;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using System;
using System.Threading.Tasks;
using WebApiContrib.Core.Results;

namespace AspNetCore.Mvc.MvcAsApi.Extensions
{
    //https://github.com/aspnet/AspNetCore/issues/4953

    //https://github.com/aspnet/AspNetCore/blob/c565386a3ed135560bc2e9017aa54a950b4e35dd/src/Mvc/Mvc.Core/src/ApplicationModels/ApiBehaviorApplicationModelProvider.cs

    //Errors
    //https://github.com/aspnet/AspNetCore/blob/f79f2e3b1200f8e672b77583a54e6157e49da9e4/src/Mvc/Mvc.Core/src/ApplicationModels/ClientErrorResultFilterConvention.cs
    //https://github.com/aspnet/AspNetCore/blob/c565386a3ed135560bc2e9017aa54a950b4e35dd/src/Mvc/Mvc.Core/src/Infrastructure/ClientErrorResultFilterFactory.cs
    //https://github.com/aspnet/AspNetCore/blob/c565386a3ed135560bc2e9017aa54a950b4e35dd/src/Mvc/Mvc.Core/src/Infrastructure/ClientErrorResultFilter.cs

    //https://github.com/aspnet/AspNetCore/blob/a8b67a2b98fefedf7de9902f255209110c83c658/src/Middleware/Diagnostics/src/DeveloperExceptionPage/DeveloperExceptionPageMiddleware.cs
    public static class ProblemDetailsClientErrorFactoryExtensions
    {
        public static IMvcBuilder AddMvcEnhancedProblemDetailsClientErrorFactory(this IMvcBuilder builder)
        {
            var services = builder.Services;

            services.AddSingleton<IClientErrorFactory, EnhancedProblemDetailsClientErrorFactory>();

            return builder;
        }

        public static IMvcBuilder AddMvcEnhancedProblemDetailsClientErrorFactory(this IMvcBuilder builder, Action<EnhancedClientErrorFactoryOptions> setupAction)
        {
            var services = builder.Services;

            builder.AddMvcEnhancedProblemDetailsClientErrorFactory();
            services.Configure(setupAction);

            return builder;
        }
    }
}
