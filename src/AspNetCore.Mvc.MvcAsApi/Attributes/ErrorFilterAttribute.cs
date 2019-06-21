using AspNetCore.Mvc.MvcAsApi.Extensions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using static AspNetCore.Mvc.MvcAsApi.Attributes.ErrorFilterOptions;

namespace AspNetCore.Mvc.MvcAsApi.Attributes
{
    public class ApiErrorFilterAttribute : ErrorFilterAttribute
    {
        public ApiErrorFilterAttribute(bool handleBrowserRequests = false)
         : this(handleBrowserRequests, null)
        {

        }

        public ApiErrorFilterAttribute(bool handleBrowserRequests, Action<ApiErrorFilterOptions> setupAction)
        : base(handleBrowserRequests, true, ConfigureOptions(setupAction))
        {

        }

        private static ApiErrorFilterOptions ConfigureOptions(Action<ApiErrorFilterOptions> setupAction)
        {
            var options = new ApiErrorFilterOptions();
            if (setupAction != null)
                setupAction(options);

            return options;
        }
    }

    public class MvcErrorFilterAttribute : ErrorFilterAttribute
    {
        public MvcErrorFilterAttribute(bool handleNonBrowserRequests = false)
         : this(handleNonBrowserRequests, null)
        {

        }

        public MvcErrorFilterAttribute(bool handleNonBrowserRequests, Action<MvcErrorFilterOptions> setupAction)
        : base(true, handleNonBrowserRequests, ConfigureOptions(setupAction))
        {

        }

        private static MvcErrorFilterOptions ConfigureOptions(Action<MvcErrorFilterOptions> setupAction)
        {
            var options = new MvcErrorFilterOptions();
            if (setupAction != null)
                setupAction(options);

            return options;
        }
    }

    public abstract class ErrorFilterAttribute : TypeFilterAttribute
    {
        public ErrorFilterAttribute(bool handleBrowserRequests, bool handleNonBrowserRequests, ErrorFilterOptions options)
       :base(typeof(ErrorFilterImpl))
        {
            Arguments = new object[] { handleBrowserRequests, handleNonBrowserRequests, options};
        }

        //https://github.com/aspnet/AspNetCore/blob/c565386a3ed135560bc2e9017aa54a950b4e35dd/src/Mvc/Mvc.Core/src/Infrastructure/ClientErrorResultFilter.cs#L44
        private class ErrorFilterImpl : IAlwaysRunResultFilter, IOrderedFilter
        {
            private readonly bool _handleBrowserRequests;
            private readonly bool _handleNonBrowserRequests;
            private readonly ILogger _logger;
            internal const int FilterOrder = -2000;
            private readonly IClientErrorFactory _clientErrorFactory;

            private readonly ErrorFilterOptions _options;

            public int Order => FilterOrder;

            private static readonly Action<ILogger, Type, int?, Type, Exception> _transformingClientError = LoggerMessage.Define<Type, int?, Type>(
               LogLevel.Trace,
                new EventId(49, "ApiErrorFilterAttribute"),
                "Replacing {InitialActionResultType} with status code {StatusCode} with {ReplacedActionResultType}.");

            public ErrorFilterImpl(IClientErrorFactory clientErrorFactory, ILoggerFactory loggerFactory, bool handleBrowserRequests, bool handleNonBrowserRequests, ErrorFilterOptions options)
            {
                _clientErrorFactory = clientErrorFactory ?? throw new ArgumentNullException(nameof(clientErrorFactory));
                _logger = loggerFactory.CreateLogger<ErrorFilterAttribute>();
                _handleBrowserRequests = handleBrowserRequests;
                _handleNonBrowserRequests = handleNonBrowserRequests;
                _options = options;
            }

            public void OnResultExecuting(ResultExecutingContext context)
            {
                if ((!_handleBrowserRequests && context.HttpContext.Request.IsBrowser()) || (!_handleNonBrowserRequests && !context.HttpContext.Request.IsBrowser()) ||!(context.Result is IClientErrorActionResult clientError))
                {
                    return;
                }

                // We do not have an upper bound on the allowed status code. This allows this filter to be used
                // for 5xx and later status codes.
                if (!_options.HandleError(context, _options, clientError))
                {
                    return;
                }

                ActionResultFactory factory = (clientError.StatusCode.HasValue && _options.ActionResultFactories.ContainsKey(clientError.StatusCode.Value)) ? _options.ActionResultFactories[clientError.StatusCode.Value] : _options.DefaultActionResultFactory ?? null;

                if (factory == null)
                {
                    return;
                }

                var result = factory(context, _logger, clientError);
                if (result == null)
                {
                    return;
                }

                _logger.TransformingClientError(context.Result.GetType(), result?.GetType(), clientError.StatusCode);

                context.HttpContext.Items["mvcErrorHandled"] = true;
                context.Result = result;
            }

            public void OnResultExecuted(ResultExecutedContext context)
            {
               
            }
        }
    }

    public abstract class ErrorFilterOptions
    {
        public Func<ResultExecutingContext, ErrorFilterOptions, IClientErrorActionResult, bool> HandleError { get; set; } = ((context, options, clientError) => ((clientError.StatusCode >= 400 && options.DefaultActionResultFactory != null) || (clientError.StatusCode.HasValue && options.ActionResultFactories.ContainsKey(clientError.StatusCode.Value))));

        public delegate IActionResult ActionResultFactory(ResultExecutingContext context, ILogger logger, IClientErrorActionResult clientError);

        public virtual ActionResultFactory DefaultActionResultFactory { get; set; } = null;
        public virtual Dictionary<int, ActionResultFactory> ActionResultFactories { get; set; } = new Dictionary<int, ActionResultFactory>()
        {

        };
    }

    public class MvcErrorFilterOptions : ErrorFilterOptions
    {

    }

    public class ApiErrorFilterOptions : ErrorFilterOptions
    {
        public override ActionResultFactory DefaultActionResultFactory { get; set; } = ((context, logger, clientError) =>
        {
            var clientErrorFactory = context.HttpContext.RequestServices.GetService<IClientErrorFactory>();
            return clientErrorFactory.GetClientError(context, clientError);
        });

        public override Dictionary<int, ActionResultFactory> ActionResultFactories { get; set; } = new Dictionary<int, ActionResultFactory>()
        {

        };
    }
}
