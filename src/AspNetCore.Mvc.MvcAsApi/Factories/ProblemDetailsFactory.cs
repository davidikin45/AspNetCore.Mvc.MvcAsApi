using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace AspNetCore.Mvc.MvcAsApi.Factories
{
    public static class ProblemDetailsFactory
    {
        private static readonly string TraceIdentifierKey = "traceId";
        private static readonly string TimeGeneratedKey = "timeGenerated";

        public static ProblemDetails GetProblemDetails(HttpContext httpContext, string title, int? status, string detail = null)
        {
            var problemDetails = new ProblemDetails()
            {
                Type = "about:blank",
                Title = title,
                Instance = httpContext.Request.Path,
                Status = status,
                Detail = detail
            };

            SetTraceId(httpContext, problemDetails);
            SetTimeGenerated(problemDetails);

            return problemDetails;
        }

        public static void SetTraceId(HttpContext httpContext, ProblemDetails problemDetails)
        {
            var traceId = Activity.Current?.Id ?? httpContext.TraceIdentifier;
            problemDetails.Extensions[TraceIdentifierKey] = traceId;
        }

        public static void SetTimeGenerated(ProblemDetails problemDetails)
        {
            problemDetails.Extensions[TimeGeneratedKey] = DateTime.UtcNow;
        }
    }
}
