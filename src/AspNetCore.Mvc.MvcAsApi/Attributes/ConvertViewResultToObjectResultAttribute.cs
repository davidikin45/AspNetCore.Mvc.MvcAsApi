using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.IO;
using System.Text;
using AspNetCore.Mvc.MvcAsApi;

namespace AspNetCore.Mvc.MvcAsApi.Attributes
{
    //https://github.com/aspnet/AspNetCore/blob/c565386a3ed135560bc2e9017aa54a950b4e35dd/src/Mvc/Mvc.ViewFeatures/src/ViewResultExecutor.cs
    //https://github.com/aspnet/AspNetCore/blob/c565386a3ed135560bc2e9017aa54a950b4e35dd/src/Mvc/Mvc.ViewFeatures/src/ViewExecutor.cs

    //https://github.com/aspnet/AspNetCore/blob/c565386a3ed135560bc2e9017aa54a950b4e35dd/src/Mvc/Mvc.Core/src/Infrastructure/ObjectResultExecutor.cs
    //https://github.com/aspnet/AspNetCore/blob/c565386a3ed135560bc2e9017aa54a950b4e35dd/src/Mvc/Mvc.Core/src/Infrastructure/DefaultOutputFormatterSelector.cs

    //Works with [FromBodyRouteQueryAttribute]

    public class ConvertViewResultToObjectResultAttribute : TypeFilterAttribute
    {
        public bool Enabled { get; set; } = true;

        public ConvertViewResultToObjectResultAttribute() : base(typeof(ConvertViewResultToObjectResultAttributeImpl))
        {
            Arguments = new object[] { Enabled };
        }

        private class ConvertViewResultToObjectResultAttributeImpl : ActionFilterAttribute
        {
            private readonly ILogger _logger;
            private readonly MvcOptions _mvcOptions;
            private readonly bool _enabled;
            private readonly ApiBehaviorOptions _apiBehaviorOptions;

            public ConvertViewResultToObjectResultAttributeImpl(OutputFormatterSelector formatterSelector, ILoggerFactory loggerFactory, IOptions<MvcOptions> mvcOptions, IOptions<ApiBehaviorOptions> apiBehaviorOptions, bool enabled)
            {
                _logger = loggerFactory.CreateLogger<ConvertViewResultToObjectResultAttribute>();
                _mvcOptions = mvcOptions.Value;
                _apiBehaviorOptions = apiBehaviorOptions.Value;
                _enabled = enabled;
            }

            public override void OnResultExecuting(ResultExecutingContext context)
            {
                if (_enabled && context.Result is ViewResult && !context.HttpContext.Request.IsBrowser())
                {
                    var viewResult = context.Result as ViewResult;

                    var responseContentType = context.HttpContext.Response.ContentType;

                    //If no response content type has been set we can convert ViewResult > ObjectResult
                    if (string.IsNullOrEmpty(responseContentType))
                    {
                        var result = new ObjectResult(viewResult.Model);

                        var objectType = result.DeclaredType;
                        if (objectType == null || objectType == typeof(object))
                        {
                            objectType = result.Value?.GetType();
                        }

                        //If RespectBrowserAcceptHeader is true this will return alot of accept headers.
                        var sortedAcceptHeaders = context.HttpContext.Request.GetAcceptableMediaTypes();

                        IOutputFormatter selectedFormatter = null;
                        var selectFormatterWithoutRegardingAcceptHeader = false;
                        if (sortedAcceptHeaders.Count > 0)
                        {
                            selectedFormatter = context.HttpContext.Request.SelectFormatterUsingSortedAcceptHeaders(objectType, result.Value, sortedAcceptHeaders);
                        }
                        else
                        {
                            //[ApiControler] Browser Requests or [ApiController] requests without accept header will hit here.

                            if(!context.HttpContext.Request.HasAcceptHeaders())
                            {
                                //We could use the default output formatter when no accept header is sent but think for conversion to occur an explicit accept header should be set.
                                //selectFormatterWithoutRegardingAcceptHeader = true;
                            }
                        }

                        if(selectFormatterWithoutRegardingAcceptHeader)
                        {
                            selectedFormatter = context.HttpContext.Request.SelectFormatterNotUsingContentType(objectType, result.Value);
                        }

                        if (selectedFormatter == null)
                        {
                            if(sortedAcceptHeaders.Count > 0)
                            {
                                _logger.LogInformation($"Failed converting ViewResult > ObjectResult. No output formatter found for Accept Header.");
                            }
                            else
                            {
                                //No conversion needs to occcur.
                            }
                        }
                        else
                        {
                            _logger.LogInformation($"Successfully converted ViewResult > ObjectResult.");

                            if (!context.ModelState.IsValid)
                            {
                                context.Result = _apiBehaviorOptions.InvalidModelStateResponseFactory(context);
                            }
                            else
                            {
                                context.Result = new ObjectResult(viewResult.Model);
                            }
                        }
                    }
                }
            }
        }
    }
}
