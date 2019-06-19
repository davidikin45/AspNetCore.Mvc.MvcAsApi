﻿using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.Extensions.Logging;
using System;

namespace AspNetCore.Mvc.MvcAsApi.Attributes
{
    public class ApiErrorFilterAttribute : TypeFilterAttribute
    {
        public ApiErrorFilterAttribute(bool handleBrowserRequests = false)
            :this(handleBrowserRequests, ((clientError) => clientError.StatusCode >= 400))
        {

        }

        public ApiErrorFilterAttribute(bool handleBrowserRequests, Func<IClientErrorActionResult, bool> handleError)
       :base(typeof(ApiErrorFilterImpl))
        {
            Arguments = new object[] { handleBrowserRequests, handleError };
        }

        //https://github.com/aspnet/AspNetCore/blob/c565386a3ed135560bc2e9017aa54a950b4e35dd/src/Mvc/Mvc.Core/src/Infrastructure/ClientErrorResultFilter.cs#L44
        private class ApiErrorFilterImpl : IAlwaysRunResultFilter, IOrderedFilter
        {
            private readonly bool _handleBrowserRequests;
            private readonly ILogger _logger;
            internal const int FilterOrder = -2000;
            private readonly IClientErrorFactory _clientErrorFactory;

            private readonly Func<IClientErrorActionResult, bool> _handleError;

            public int Order => FilterOrder;

            private static readonly Action<ILogger, Type, int?, Type, Exception> _transformingClientError = LoggerMessage.Define<Type, int?, Type>(
               LogLevel.Trace,
                new EventId(49, "ApiErrorFilterAttribute"),
                "Replacing {InitialActionResultType} with status code {StatusCode} with {ReplacedActionResultType}.");

            public ApiErrorFilterImpl(IClientErrorFactory clientErrorFactory, ILoggerFactory loggerFactory, bool handleBrowserRequests, Func<IClientErrorActionResult, bool> handleError)
            {
                _clientErrorFactory = clientErrorFactory ?? throw new ArgumentNullException(nameof(clientErrorFactory));
                _logger = loggerFactory.CreateLogger<ApiErrorFilterAttribute>();
                _handleBrowserRequests = handleBrowserRequests;
                _handleError = handleError;
            }

            public void OnResultExecuting(ResultExecutingContext context)
            {
                if ((!_handleBrowserRequests && context.HttpContext.Request.IsBrowser()) || !(context.Result is IClientErrorActionResult clientError))
                {
                    return;
                }

                // We do not have an upper bound on the allowed status code. This allows this filter to be used
                // for 5xx and later status codes.
                if (!_handleError(clientError))
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
}