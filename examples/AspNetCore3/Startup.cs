using AspNetCore.Mvc.MvcAsApi.Conventions;
using AspNetCore.Mvc.MvcAsApi.Extensions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using AspNetCore.Mvc.MvcAsApi.Middleware;
using AspNetCore.Mvc.MvcAsApi.ModelBinding;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json.Serialization;
using System.Collections.Generic;

namespace AspNetCore3
{
    public class Startup
    {
        public Startup(IConfiguration configuration, IHostEnvironment hostEnvironment)
        {
            Configuration = configuration;
            HostingEnvironment = hostEnvironment;
        }

        public IConfiguration Configuration { get; }
        public IHostEnvironment HostingEnvironment { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.Configure<CookiePolicyOptions>(options =>
            {
                // This lambda determines whether user consent for non-essential cookies is needed for a given request.
                options.CheckConsentNeeded = context => true;
            });


            services.AddControllersWithViews(options => {

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
                    //options.Conventions.Add(new MvcErrorFilterConvention(o => { options.ApplyToMvcActions = true; options.ApplyToApiControllerActions = true; }));
                    //Intercepts OperationCanceledException, all other exceptions are logged/handled by UseExceptionHandler/UseDeveloperExceptionPage.
                    //options.Conventions.Add(new MvcExceptionFilterConvention(o => {options.ApplyToMvcActions = true; options.ApplyToApiControllerActions = true; }));
                    //Return problem details in json/xml if an error response is returned via Api.
                    //options.Conventions.Add(new ApiErrorFilterConvention(o => { o.ApplyToMvcActions = true; o.ApplyToApiControllerActions = true; }));
                    //Return problem details in json/xml if an exception is thrown via Api
                    //options.Conventions.Add(new ApiExceptionFilterConvention(o => { o.ApplyToMvcActions = true; o.ApplyToApiControllerActions = true; }));
                    //Post data to MVC Controller from API
                    //options.Conventions.Add(new FromBodyAndOtherSourcesConvention(o => { o.ApplyToMvcActions = true; o.ApplyToApiControllerActions = true; o.EnableForParametersWithNoBinding = true; o.EnableForParametersWithFormRouteQueryBinding = true; o.ChangeFromBodyBindingsToFromBodyFormAndRouteQueryBinding = true; }));
                    //Return data uisng output formatter when acccept header is application/json or application/xml
                    //options.Conventions.Add(new ConvertViewResultToObjectResultConvention(o => { o.ApplyToMvcActions = true; o.ApplyToApiControllerActions = true; }));
                }


               
            });
            services.AddRazorPages();

            //https://github.com/dotnet/corefx/blob/e8990ae04e4ef62f5ccb143352472c9b4d5d5968/src/System.Text.Json/docs/SerializerProgrammingModel.md
            services.Configure<JsonOptions>(options =>
            {
                //default
                options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;

                //BUG: Currently DictionaryKeyPolicy only works for deserialization but not serialization so model state errors are not camelCase!
                //https://github.com/dotnet/corefx/issues/38840
                options.JsonSerializerOptions.DictionaryKeyPolicy = JsonNamingPolicy.CamelCase;
            });

            if (HostingEnvironment.IsDevelopment())
            {
                //MVC Dynamic Model Binding
                services.AddDynamicModelBinder();

                //Api StatusCodeResult Enhanced Problem Details (traceId, timeGenerated, delegate factory)
                services.AddProblemDetailsClientErrorAndExceptionFactory(options => { options.ShowExceptionDetails = true; });

                //Api Invalid ModelState Enhanced Problem Details (traceId, timeGenerated, delegate factory)
                services.ConfigureProblemDetailsInvalidModelStateFactory(options => { options.EnableAngularErrors = true; });
            }
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            //If want to intercept content responses
            app.UseRouting(); //.NET Core 3.0

            if (env.IsDevelopment())
            {
                //IsMvc and IsApi require access to IEndpointFeature which is what app.UseEndpointRouting/app.UseRouting/app.UseMvc provide.
                //When using app.UseWhen the delegate is evaluated when the response comes in before hitting MVC so routing hasn't been evaluated.
                //Unless we want to intercept content responses we can delay delegate evaluation until it comes back through the pipeline.  
                app.UseOutbound(appBranch =>
                {
                    appBranch.UseWhen(context => context.Request.IsMvc(), mvcBranch => mvcBranch.UseDeveloperExceptionPage());
                    appBranch.UseWhen(context => context.Request.IsApi(), apiBranch =>
                    {
                        apiBranch.UseProblemDetailsExceptionHandler(options => options.ShowExceptionDetails = true);
                        apiBranch.UseProblemDetailsErrorResponseHandler(options => options.HandleContentResponses = false);
                    });
                });

                //If handling content responses.
                //app.UseWhen(context => context.Request.IsApi(), apiBranch => apiBranch.UseProblemDetailsErrorResponseHandler(options => options.HandleContentResponses = true));

                app.UseDatabaseErrorPage();
            }
            else
            {
                app.UseOutbound(appBranch =>
                {
                    appBranch.UseWhen(context => context.Request.IsMvc(), mvcBranch => mvcBranch.UseExceptionHandler("/Home/Error"));
                    appBranch.UseWhen(context => context.Request.IsApi(), apiBranch =>
                    {
                        apiBranch.UseProblemDetailsExceptionHandler(options => options.ShowExceptionDetails = false);
                        apiBranch.UseProblemDetailsErrorResponseHandler(options => options.HandleContentResponses = false);
                    });
                });

                //If handling content responses.
                //app.UseWhen(context => context.Request.IsApi(), apiBranch => apiBranch.UseProblemDetailsErrorResponseHandler(options => options.HandleContentResponses = true));

                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseStaticFiles();

            app.UseCookiePolicy();

            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllerRoute(
                    name: "default",
                    pattern: "{controller=Home}/{action=Index}/{id?}");
                endpoints.MapRazorPages();
            });
        }
    }
}
