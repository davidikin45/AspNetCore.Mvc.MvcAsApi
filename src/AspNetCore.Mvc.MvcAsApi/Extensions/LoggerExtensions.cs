using Microsoft.Extensions.Logging;
using System;

namespace AspNetCore.Mvc.MvcAsApi.Extensions
{
    public static class LoggerExtensions
    {
        // ExceptionHandlerMiddleware(UseProblemDetailsExceptionHandler), DeveloperExceptionPageMiddleware & ExceptionFilterAttribute
        private static readonly Action<ILogger, Exception> _unhandledException =
            LoggerMessage.Define(LogLevel.Error, new EventId(1, "UnhandledException"), "An unhandled exception has occurred while executing the request.");

        // ExceptionHandlerMiddleware(UseProblemDetailsExceptionHandler) && ProblemDetailsErrorResponseHandlerMiddleware
        private static readonly Action<ILogger, Exception> _responseStartedErrorHandler =
            LoggerMessage.Define(LogLevel.Warning, new EventId(2, "ResponseStarted"), "The response has already started, the error handler will not be executed.");

        private static readonly Action<ILogger, Exception> _errorHandlerException =
            LoggerMessage.Define(LogLevel.Error, new EventId(3, "Exception"), "An exception was thrown attempting to execute the error handler.");

        //ErrorFilterAttribute
        private static readonly Action<ILogger, Type, int?, Type, Exception> _transformingClientError = LoggerMessage.Define<Type, int?, Type>(
       LogLevel.Trace,
        new EventId(49, "ErrorFilterAttribute"),
        "Replacing {InitialActionResultType} with status code {StatusCode} with {ReplacedActionResultType}.");

        //ProblemDetailsErrorResponseHandlerMiddleware
        private static readonly Action<ILogger, int?, Exception> _transformingStatusCode = LoggerMessage.Define<int?>(
         LogLevel.Trace,
          new EventId(49, "ProblemDetailsErrorResponseHandlerMiddleware"),
          "Replacing response with status code {StatusCode} with problem details.");

        private static readonly Action<ILogger, Type, Exception> _notMostEffectiveFilter = LoggerMessage.Define<Type>(LogLevel.Debug,  
            new EventId(1, "NotMostEffectiveFilter"), 
            "Skipping the execution of current filter as its not the most effective filter implementing the policy {FilterPolicy}.");

        private static readonly Action<ILogger, string, Exception> _antiforgeryTokenInvalid = LoggerMessage.Define<string>(
               LogLevel.Information,
                new EventId(1, "AntiforgeryTokenInvalid"),
                "Antiforgery token validation failed. {Message}");

        public static void UnhandledException(this ILogger logger, Exception exception)
        {
            _unhandledException(logger, exception);
        }

        public static void ResponseStartedErrorHandler(this ILogger logger)
        {
            _responseStartedErrorHandler(logger, null);
        }

        public static void ErrorHandlerException(this ILogger logger, Exception exception)
        {
            _errorHandlerException(logger, exception);
        }


        public static void TransformingClientError(this ILogger logger, Type initialType, Type replacedType, int? statusCode)
        {
            _transformingClientError(logger, initialType, statusCode, replacedType, null);
        }

        public static void TransformingStatusCodeToProblemDetails(this ILogger logger, int? statusCode)
        {
            _transformingStatusCode(logger, statusCode, null);
        }

        public static void NotMostEffectiveFilter(this ILogger logger, Type policyType)
        {
            _notMostEffectiveFilter(logger, policyType, null);
        }

        public static void AntiforgeryTokenInvalid(this ILogger logger, string message, Exception exception)
        {
            _antiforgeryTokenInvalid(logger, message, exception);
        }
    }
}