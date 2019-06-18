using AspNetCore.Mvc.HybridModelBindingAndViewToObjectResult;
using AspNetCore.Mvc.HybridModelBindingAndViewToObjectResult.Conventions;
using AspNetCore.Mvc.HybridModelBindingAndViewToObjectResult.Factories;
using AspNetCore.Mvc.HybridModelBindingAndViewToObjectResult.Middleware;
using AspNetCore.Mvc.HybridModelBindingAndViewToObjectResult.ModelBinding;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

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
                if (HostingEnvironment.IsDevelopment())
                {
                    //Return problem details in json/xml if an error response is returned via Api.
                    options.Conventions.Add(new ApiErrorFilterConvention());
                    //Return problem details in json/xml if an exception is thrown via Api
                    options.Conventions.Add(new ApiExceptionFilterConvention());
                    //Post data to MVC Controller from API
                    options.Conventions.Add(new FromBodyAndOtherSourcesConvention(true, true, true));
                    //Return data uisng output formatter when acccept header is application/json or application/xml
                    options.Conventions.Add(new ConvertViewResultToObjectResultConvention());
                }
            }).SetCompatibilityVersion(CompatibilityVersion.Version_2_2)
            .AddDynamicModelBinder();

            //Optional
            if (HostingEnvironment.IsDevelopment())
            {
                //Overrides the default IClientErrorFactory implementation which adds traceId, timeGenerated and exception details to the ProblemDetails response.
                services.AddEnhancedProblemDetailsClientErrorFactory(true);

                services.Configure<ApiBehaviorOptions>(options =>
                {
                    //Overrides the default InvalidModelStateResponseFactory, adds traceId and timeGenerated to the ProblemDetails response. 
                    options.EnableEnhancedValidationProblemDetails();
                });
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
