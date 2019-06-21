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
    public class ApiExceptionFilterAttribute : ExceptionFilterAttribute
    {
       public ApiExceptionFilterAttribute(bool handleBrowserRequests = false)
        :this(handleBrowserRequests, null)
        {

        }

        public ApiExceptionFilterAttribute(bool handleBrowserRequests, Action<ApiExceptionFilterOptions> setupAction)
        :base(handleBrowserRequests, true, ConfigureOptions(setupAction))
        {

        }

        private static ApiExceptionFilterOptions ConfigureOptions(Action<ApiExceptionFilterOptions> setupAction)
        {
            var options = new ApiExceptionFilterOptions();
            if (setupAction != null)
                setupAction(options);

            return options;
        }
    }

    public class MvcExceptionFilterAttribute : ExceptionFilterAttribute
    {
        public MvcExceptionFilterAttribute(bool handleNonBrowserRequests = false)
         :this(handleNonBrowserRequests, null)
        {

        }

        public MvcExceptionFilterAttribute(bool handleNonBrowserRequests, Action<MvcExceptionFilterOptions> setupAction)
        :base(true, handleNonBrowserRequests, ConfigureOptions(setupAction))
        {

        }

        private static MvcExceptionFilterOptions ConfigureOptions(Action<MvcExceptionFilterOptions> setupAction)
        {
            var options = new MvcExceptionFilterOptions();
            if (setupAction != null)
                setupAction(options);

            return options;
        }
    }

    public abstract class ExceptionFilterAttribute : TypeFilterAttribute
    {
        public ExceptionFilterAttribute(bool handleBrowserRequests, bool handleNonBrowserRequests, ExceptionFilterOptions options)
       : base(typeof(ExceptionFilterImpl))
        {
            Arguments = new object[] { handleBrowserRequests, handleNonBrowserRequests, options };
        }

        //https://github.com/aspnet/AspNetCore/blob/c565386a3ed135560bc2e9017aa54a950b4e35dd/src/Mvc/Mvc.Core/src/Infrastructure/ClientErrorResultFilter.cs#L44
        private class ExceptionFilterImpl : IExceptionFilter, IOrderedFilter
        {
            private readonly ILogger _logger;
            internal const int FilterOrder = -2000;

            private readonly bool _handleBrowerRequests;
            private readonly bool _handleNonBrowerRequests;
            private readonly ExceptionFilterOptions _options;
            public int Order => FilterOrder;

            public ExceptionFilterImpl(ILoggerFactory loggerFactory, bool handleBrowerRequests, bool handleNonBrowerRequests, ExceptionFilterOptions options)
            {
                _logger = loggerFactory.CreateLogger<ExceptionFilterAttribute>();
                _handleBrowerRequests = handleBrowerRequests;
                _handleNonBrowerRequests = handleNonBrowerRequests;
                _options = options;
            }

            public void OnException(ExceptionContext context)
            {
                if ((!_handleBrowerRequests && context.HttpContext.Request.IsBrowser()) || (!_handleNonBrowerRequests && !context.HttpContext.Request.IsBrowser()) || !_options.HandleException(context, _options))
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
                        var result = factory(context, exception, _logger);
                        if(result != null)
                        {
                            context.Result = result;
                        }

                        return;
                    }
                }

                if(_options.DefaultActionResultFactory != null)
                {
                    var result = _options.DefaultActionResultFactory(context, exception, _logger);
                    if (result != null)
                    {
                        context.Result = result;
                    }
                }
            }
        }
    }

    public abstract class ExceptionFilterOptions
    {
        public Func<ExceptionContext, ExceptionFilterOptions, bool> HandleException { get; set; } = ((context, options) => options.DefaultActionResultFactory != null || context.Exception.GetType().GetTypeAndInterfaceHierarchy().Any(type => options.ActionResultFactories.ContainsKey(type)));

        public delegate IActionResult ExceptionHandler(ActionContext context, Exception exception, ILogger logger);

        public virtual ExceptionHandler DefaultActionResultFactory { get; set; } = null;
        public virtual Dictionary<Type, ExceptionHandler> ActionResultFactories { get; set; } = new Dictionary<Type, ExceptionHandler>() {
 
        };
    }

    public class MvcExceptionFilterOptions : ExceptionFilterOptions
    {
        //Let exception flow through to UseExceptionHandler/UseDeveloperExceptionPage where it will be handled/logged.
        public override ExceptionHandler DefaultActionResultFactory { get; set; } = null;

        public override Dictionary<Type, ExceptionHandler> ActionResultFactories { get; set; } = new Dictionary<Type, ExceptionHandler>() {
           {typeof(OperationCanceledException), ((context, exception, logger) => {
                 logger.LogInformation("Request was cancelled.");
                 return new ExceptionResult(exception, 499);
            })}
        };
    }

    public class ApiExceptionFilterOptions : ExceptionFilterOptions
    {
        public override ExceptionHandler DefaultActionResultFactory { get; set; } = ((context, exception, logger) =>
        {
            //Log and swallow exception.
            logger.UnhandledException(exception);
            return new ExceptionResult(exception, StatusCodes.Status500InternalServerError);
        });

        public override Dictionary<Type, ExceptionHandler> ActionResultFactories { get; set; } = new Dictionary<Type, ExceptionHandler>() {
             {typeof(TimeoutException), ((context, exception, logger) => {
                 logger.LogInformation("Request timed out.");
                 return new ExceptionResult(exception, StatusCodes.Status504GatewayTimeout);
            })},
            {typeof(OperationCanceledException), ((context, exception, logger) => {
                  logger.LogInformation("Request was cancelled.");
                 return new ExceptionResult(exception, 499);
            })}
        };
    }
}
