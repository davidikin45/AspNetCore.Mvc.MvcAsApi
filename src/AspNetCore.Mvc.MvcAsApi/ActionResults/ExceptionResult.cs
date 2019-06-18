using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using System;

namespace AspNetCore.Mvc.MvcAsApi.ActionResults
{
    public class ExceptionResult : StatusCodeResult
    {
        public ExceptionResult(Exception error) : this(error, StatusCodes.Status500InternalServerError)
        {
        }

        public ExceptionResult(Exception error, int statusCode) : base(statusCode)
        {
            Error = error ?? throw new ArgumentNullException(nameof(error));
        }

        public Exception Error { get; }
    }
}
