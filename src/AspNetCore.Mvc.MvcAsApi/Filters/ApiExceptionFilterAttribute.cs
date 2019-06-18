using AspNetCore.Mvc.MvcAsApi.ActionResults;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.Extensions.Logging;
using System;

namespace AspNetCore.Mvc.MvcAsApi.Filters
{
    public class ApiExceptionFilterAttribute : TypeFilterAttribute
    {
        public ApiExceptionFilterAttribute()
            :this(((context) => true))
        {

        }

        public ApiExceptionFilterAttribute(Func<ExceptionContext, bool> handleException)
       :base(typeof(ApiExceptionFilterImpl))
        {
            Arguments = new object[] { handleException };
        }

        //https://github.com/aspnet/AspNetCore/blob/c565386a3ed135560bc2e9017aa54a950b4e35dd/src/Mvc/Mvc.Core/src/Infrastructure/ClientErrorResultFilter.cs#L44
        private class ApiExceptionFilterImpl : IExceptionFilter, IOrderedFilter
        {
            private readonly ILogger _logger;
            internal const int FilterOrder = -3000;

            private readonly Func<IClientErrorActionResult, bool> _handleError;
            private readonly Func<ExceptionContext, bool> _handleException;
            public int Order => FilterOrder;

            public ApiExceptionFilterImpl(ILoggerFactory loggerFactory, Func<ExceptionContext, bool> handleException)
            {
                _logger = loggerFactory.CreateLogger<ApiExceptionFilterAttribute>();
                _handleException = handleException;
            }

            public void OnException(ExceptionContext context)
            {
                if (context.HttpContext.Request.IsApi() && _handleException(context))
                {
                    HandleException(context);
                }
            }

            //https://andrewlock.net/using-cancellationtokens-in-asp-net-core-mvc-controllers/
            private void HandleException(ExceptionContext context)
            {
                if (context.Exception is OperationCanceledException)
                {
                    _logger.LogInformation("Api request was cancelled.");
                    context.ExceptionHandled = true;

                    //Will get handled by IClientErrorFactory
                    context.Result = new ExceptionResult(context.Exception, 499);
                }
                else if (context.Exception is TimeoutException)
                {
                    _logger.LogInformation("Api request timed out.");

                    context.ExceptionHandled = true;
                    context.Result = new ExceptionResult(context.Exception, StatusCodes.Status504GatewayTimeout);
                }
                else
                {
                    _logger.LogError(context.Exception, "Api error has occured.");

                    context.ExceptionHandled = true;

                    //Will get handled by IClientErrorFactory
                    context.Result = new ExceptionResult(context.Exception, StatusCodes.Status500InternalServerError);
                }
            }
        }
    }
}
