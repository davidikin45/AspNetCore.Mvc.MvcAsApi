using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.Extensions.Logging;
using System;

namespace AspNetCore.Mvc.MvcAsApi.Attributes
{
    public class ApiErrorFilterAttribute : TypeFilterAttribute
    {
        public ApiErrorFilterAttribute(bool handleBrowserRequests = false)
            :this(handleBrowserRequests, null)
        {

        }

        public ApiErrorFilterAttribute(bool handleBrowserRequests, Action<ApiErrorFilterOptions> setupAction)
       :base(typeof(ApiErrorFilterImpl))
        {
            var options = new ApiErrorFilterOptions();
            if (setupAction != null)
                setupAction(options);

            Arguments = new object[] { handleBrowserRequests, options};
        }

        //https://github.com/aspnet/AspNetCore/blob/c565386a3ed135560bc2e9017aa54a950b4e35dd/src/Mvc/Mvc.Core/src/Infrastructure/ClientErrorResultFilter.cs#L44
        private class ApiErrorFilterImpl : IAlwaysRunResultFilter, IOrderedFilter
        {
            private readonly bool _handleBrowserRequests;
            private readonly ILogger _logger;
            internal const int FilterOrder = -2000;
            private readonly IClientErrorFactory _clientErrorFactory;

            private readonly ApiErrorFilterOptions _options;

            public int Order => FilterOrder;

            private static readonly Action<ILogger, Type, int?, Type, Exception> _transformingClientError = LoggerMessage.Define<Type, int?, Type>(
               LogLevel.Trace,
                new EventId(49, "ApiErrorFilterAttribute"),
                "Replacing {InitialActionResultType} with status code {StatusCode} with {ReplacedActionResultType}.");

            public ApiErrorFilterImpl(IClientErrorFactory clientErrorFactory, ILoggerFactory loggerFactory, bool handleBrowserRequests, ApiErrorFilterOptions options)
            {
                _clientErrorFactory = clientErrorFactory ?? throw new ArgumentNullException(nameof(clientErrorFactory));
                _logger = loggerFactory.CreateLogger<ApiErrorFilterAttribute>();
                _handleBrowserRequests = handleBrowserRequests;
                _options = options;
            }

            public void OnResultExecuting(ResultExecutingContext context)
            {
                if ((!_handleBrowserRequests && context.HttpContext.Request.IsBrowser()) || !(context.Result is IClientErrorActionResult clientError))
                {
                    return;
                }

                // We do not have an upper bound on the allowed status code. This allows this filter to be used
                // for 5xx and later status codes.
                if (!_options.HandleError(clientError))
                {
                    return;
                }

                var result = _clientErrorFactory.GetClientError(context, clientError);
                if (result == null)
                {
                    return;
                }

                _transformingClientError(_logger, context.Result.GetType(), clientError.StatusCode, result?.GetType(), null);
                context.HttpContext.Items["mvcErrorHandled"] = true;
                context.Result = result;
            }

            public void OnResultExecuted(ResultExecutedContext context)
            {
               
            }
        }
    }

    public class ApiErrorFilterOptions
    {
        public Func<IClientErrorActionResult, bool> HandleError { get; set; } = ((clientError) => clientError.StatusCode >= 400);

    }
}
