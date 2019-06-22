using AspNetCore.Mvc.MvcAsApi;
using AspNetCore.Mvc.MvcAsApi.Conventions;
using AspNetCore.Mvc.MvcAsApi.Extensions;
using AspNetCore.Mvc.MvcAsApi.Middleware;
using AspNetCore.Mvc.MvcAsApi.ModelBinding;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.Threading.Tasks;

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

            services.AddMvc(options =>
            {
                //Default = false. 
                //If the Request contains Accept header '*/*' the server ignores the Accept headers completely and uses the first output formatter that can format the object (usually json). 
                //For example when you hit an Api from a web browser.
                options.RespectBrowserAcceptHeader = false;

                //Default = false but good practice to set this to true.
                //If the Request does not contain Accept header '*/*' the server MUST find an output formatter based on accept header otherwise return statuscode 406 Not Acceptable. 
                //For example when making a json/xml/yaml request from postman. 
                //If this is left as false and request is sent in with accept header 'application/x-yaml', if the server doesn't have a yaml formatter it would use the first output formatter that can format the object (usually json) which is confusing for the client.
                options.ReturnHttpNotAcceptable = true;

                if (HostingEnvironment.IsDevelopment())
                {
                    //options.Conventions.Add(new MvcAsApiConvention());
                    // OR
                    options.Conventions.Add(new MvcAsApiConvention(o =>
                    {
                        o.MvcErrorOptions = (mvcErrorOptions) => {
                 
                        };
                        o.MvcExceptionOptions = (mvcExceptionOptions) => {

                        };
                        o.ApiErrorOptions = (apiErrorOptions) => {

                        };
                        o.ApiExceptionOptions = (apiExceptionOptions) => {

                        };
                    }));
                    // OR
                    //Does nothing by default.
                    //options.Conventions.Add(new MvcErrorFilterConvention(o => { o.HandleNonBrowserRequests = false; }));
                    //Intercepts OperationCanceledException, all other exceptions are logged/handled by UseExceptionHandler/UseDeveloperExceptionPage.
                    //options.Conventions.Add(new MvcExceptionFilterConvention(o => { o.HandleNonBrowserRequests = false; }));
                    //Return problem details in json/xml if an error response is returned via Api.
                    //options.Conventions.Add(new ApiErrorFilterConvention(o => { o.ApplyToMvcActions = true; o.ApplyToApiControllerActions = true; }));
                    //Return problem details in json/xml if an exception is thrown via Api
                    //options.Conventions.Add(new ApiExceptionFilterConvention(o => { o.ApplyToMvcActions = true; o.ApplyToApiControllerActions = true; }));
                    //Post data to MVC Controller from API
                    //options.Conventions.Add(new FromBodyAndOtherSourcesConvention(o => { o.ApplyToMvcActions = true; o.ApplyToApiControllerActions = true; o.EnableForParametersWithNoBinding = true; o.EnableForParametersWithFormRouteQueryBinding = true; o.ChangeFromBodyBindingsToFromBodyFormAndRouteQueryBinding = true; }));
                    //Return data uisng output formatter when acccept header is application/json or application/xml
                    //options.Conventions.Add(new ConvertViewResultToObjectResultConvention(o => { o.ApplyToMvcActions = true; o.ApplyToApiControllerActions = true; }));
                }
            }).SetCompatibilityVersion(CompatibilityVersion.Version_2_2);

            //Optional - These could be used independently of MvcAsApiConvention
            if (HostingEnvironment.IsDevelopment())
            {
                //MVC Dynamic Model Binding
                services.AddDynamicModelBinder();

                //Api StatusCodeResult Enhanced Problem Details (traceId, timeGenerated, delegate factory)
                services.AddProblemDetailsClientErrorAndExceptionFactory(options => { options.ShowExceptionDetails = true; });

                //Api Invalid ModelState Enhanced Problem Details (traceId, timeGenerated, delegate factory)
                services.ConfigureProblemDetailsInvalidModelStateFactory();
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
                       appBranch.UseProblemDetailsExceptionHandler(options => options.ShowExceptionDetails = true);
                        //The global error handler has logic inbuilt so if an error has been handled by MVC Filters it won't try and reprocess. 
                       appBranch.UseProblemDetailsErrorResponseHandler();
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
                           appBranch.UseProblemDetailsExceptionHandler(options => options.ShowExceptionDetails = false);
                            //The global error handler has logic inbuilt so if an error has been handled by MVC Filters it won't try and reprocess. 
                            appBranch.UseProblemDetailsErrorResponseHandler();
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
