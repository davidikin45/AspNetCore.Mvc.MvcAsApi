using AspNetCore.Mvc.MvcAsApi.ActionResults;
using AspNetCore.Mvc.MvcAsApi.Extensions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;

namespace AspNetCore.Mvc.MvcAsApi.Attributes
{
    public class ApiExceptionFilterAttribute : TypeFilterAttribute
    {

        public ApiExceptionFilterAttribute(bool handleBrowserRequests = false)
            :this(handleBrowserRequests, null)
        {
            
        }

        public ApiExceptionFilterAttribute(bool handleBrowserRequests, Action<ApiExceptionFilterOptions> setupAction)
       :base(typeof(ApiExceptionFilterImpl))
        {
            var options = new ApiExceptionFilterOptions();
            if (setupAction != null)
                setupAction(options);

            Arguments = new object[] { handleBrowserRequests, options };
        }

        //https://github.com/aspnet/AspNetCore/blob/c565386a3ed135560bc2e9017aa54a950b4e35dd/src/Mvc/Mvc.Core/src/Infrastructure/ClientErrorResultFilter.cs#L44
        private class ApiExceptionFilterImpl : IExceptionFilter, IOrderedFilter
        {
            private readonly ILogger _logger;
            internal const int FilterOrder = -2000;

            private readonly bool _handleBrowerRequests;
            private readonly ApiExceptionFilterOptions _options;
            public int Order => FilterOrder;

            public ApiExceptionFilterImpl(ILoggerFactory loggerFactory, bool handleBrowerRequests, ApiExceptionFilterOptions options)
            {
                _logger = loggerFactory.CreateLogger<ApiExceptionFilterAttribute>();
                _handleBrowerRequests = handleBrowerRequests;
                _options = options;
            }

            public void OnException(ExceptionContext context)
            {
                if ((!_handleBrowerRequests && context.HttpContext.Request.IsBrowser()) || !_options.HandleException(context, _options))
                {
                    return;
                }

                HandleException(context);
            }

            //https://andrewlock.net/using-cancellationtokens-in-asp-net-core-mvc-controllers/
            private void HandleException(ExceptionContext context)
            {
                var exception = context.Exception;
                var types = exception == null ? new[] { typeof(Exception)} : exception.GetType().GetTypeAndInterfaceHierarchy();
                foreach (var type in types)
                {
                    if(_options.ActionResultFactories.ContainsKey(type))
                    {
                        var factory = _options.ActionResultFactories[type];
                        var result = factory(exception, _logger);
                        if(result != null)
                        {
                            context.Result = result;
                        }

                        return;
                    }
                }

                if(_options.DefaultActionResultFactory != null)
                {
                    var result = _options.DefaultActionResultFactory(exception, _logger);
                    if (result != null)
                    {
                        context.Result = result;
                    }
                }
            }
        }
    }

    public class ApiExceptionFilterOptions
    {
        public Func<ExceptionContext, ApiExceptionFilterOptions, bool> HandleException { get; set; } = ((context, options) => options.DefaultActionResultFactory != null || context.Exception.GetType().GetTypeAndInterfaceHierarchy().Any(type => options.ActionResultFactories.ContainsKey(type)));

        public delegate IActionResult ExceptionHandler(Exception exception, ILogger logger);

        public ExceptionHandler DefaultActionResultFactory = ((exception, logger) =>
        {
            if (exception != null)
                logger.LogError(exception, "Api error has occured.");
            else
                logger.LogError("Api error has occured.");

            return new ExceptionResult(exception, StatusCodes.Status500InternalServerError);
        });

        public Dictionary<Type, ExceptionHandler> ActionResultFactories { get; set; } = new Dictionary<Type, ExceptionHandler>() {
             {typeof(TimeoutException), ((exception, logger) => {
                 logger.LogInformation("Api request timed out.");
                 return new ExceptionResult(exception, StatusCodes.Status504GatewayTimeout);
            })},
            {typeof(OperationCanceledException), ((exception, logger) => {
                  logger.LogInformation("Api request was cancelled.");
                 return new ExceptionResult(exception, 499);
            })}
        };
    }
}
