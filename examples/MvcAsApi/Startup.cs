using AspNetCore.Mvc.MvcAsApi;
using AspNetCore.Mvc.MvcAsApi.Conventions;
using AspNetCore.Mvc.MvcAsApi.Factories;
using AspNetCore.Mvc.MvcAsApi.Middleware;
using AspNetCore.Mvc.MvcAsApi.ModelBinding;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using AspNetCore.Mvc.MvcAsApi.Extensions;

namespace MvcAsApi
{
    public class Startup
    {
        public Startup(IConfiguration configuration, IHostingEnvironment hostingEnvironment)
        {
            Configuration = configuration;
            HostingEnvironment = hostingEnvironment;
        }

        public IConfiguration Configuration { get; }
        public IHostingEnvironment HostingEnvironment { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.Configure<CookiePolicyOptions>(options =>
            {
                // This lambda determines whether user consent for non-essential cookies is needed for a given request.
                options.CheckConsentNeeded = context => true;
                options.MinimumSameSitePolicy = SameSiteMode.None;
            });

            services.AddMvc(options=> {
                options.ReturnHttpNotAcceptable = true; //If Browser sends Accept not containing */* the server will try to find a formatter that can produce a response in one of the formats specified by the accept header.
                options.RespectBrowserAcceptHeader = false; //If Browser sends Accept containing */* the server will ignore Accept header and use the first formatter that can format the object.

                if (HostingEnvironment.IsDevelopment())
                {
                    options.Conventions.Add(new MvcAsApiConvention());

                    //Return problem details in json/xml if an error response is returned via Api.
                    //options.Conventions.Add(new ApiErrorFilterConvention(true, true));
                    //Return problem details in json/xml if an exception is thrown via Api
                    //options.Conventions.Add(new ApiExceptionFilterConvention(true, true));
                    //Post data to MVC Controller from API
                    //options.Conventions.Add(new FromBodyAndOtherSourcesConvention(true, true, true));
                    //Return data uisng output formatter when acccept header is application/json or application/xml
                    //options.Conventions.Add(new ConvertViewResultToObjectResultConvention());
                }
            }).SetCompatibilityVersion(CompatibilityVersion.Version_2_2)
            //Optional
            .AddDynamicModelBinder();

            //Optional
            if (HostingEnvironment.IsDevelopment())
            {
                //Overrides the default IClientErrorFactory implementation which adds traceId, timeGenerated and exception details to the ProblemDetails response.
                services.AddProblemDetailsClientErrorAndExceptionFactory(true);
                //Overrides the default InvalidModelStateResponseFactory, adds traceId and timeGenerated to the ProblemDetails response. 
                services.ConfigureProblemDetailsInvalidModelStateFactory(true);
            }
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            if (env.IsDevelopment())
            {
                // Non Api
                app.UseWhen(context => context.Request.IsMvc(),
                    appBranch =>
                    {
                        appBranch.UseDeveloperExceptionPage();
                    }
               );

                // Web Api
                app.UseWhen(context => context.Request.IsApi(),
                   appBranch =>
                   {
                       appBranch.UseWebApiExceptionHandlerProblemDetails(true);
                        //The global error handler has logic inbuilt so if an error has been handled by MVC Filters it won't try and reprocess. 
                       appBranch.UseWebApiErrorHandlerProblemDetails();
                   }
                );

                app.UseDatabaseErrorPage();
            }
            else
            {
                // Non Api
                app.UseWhen(context => context.Request.IsMvc(),
                     appBranch =>
                     {
                         appBranch.UseExceptionHandler("/Home/Error");
                     }
                );

                // Web Api
                    app.UseWhen(context => context.Request.IsApi(),
                       appBranch =>
                       {
                           appBranch.UseWebApiExceptionHandlerProblemDetails(false);
                            //The global error handler has logic inbuilt so if an error has been handled by MVC Filters it won't try and reprocess. 
                            appBranch.UseWebApiErrorHandlerProblemDetails();
                       }
                  );

                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseStaticFiles();
            app.UseCookiePolicy();

            app.UseMvc(routes =>
            {
                routes.MapRoute(
                    name: "default",
                    template: "{controller=Home}/{action=Index}/{id?}");
            });
        }
    }
}
