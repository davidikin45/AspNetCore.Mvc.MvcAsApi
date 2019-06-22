using Microsoft.AspNetCore.Mvc;
using System;

namespace AspNetCore.Mvc.MvcAsApi.Middleware
{
    public class ProblemDetailsException : Exception
    {
        public ProblemDetailsException(ProblemDetails problemDetails)
        {
            ProblemDetails = problemDetails;
        }

        public ProblemDetails ProblemDetails { get; }
    }
}
