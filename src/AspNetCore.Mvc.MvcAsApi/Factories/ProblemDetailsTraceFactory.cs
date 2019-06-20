using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using System;
using System.Diagnostics;

namespace AspNetCore.Mvc.MvcAsApi.Factories
{
    public static class ProblemDetailsTraceFactory
    {
        private static readonly string TraceIdentifierKey = "traceId";
        private static readonly string TimeGeneratedKey = "timeGenerated";

        public static ProblemDetails GetProblemDetails(HttpContext httpContext, string title, int? status, string detail = null)
        {
            var apiBehaviorOptions = httpContext.RequestServices.GetService<IOptions<ApiBehaviorOptions>>()?.Value;

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

            if (status is int statusCode && apiBehaviorOptions != null && apiBehaviorOptions.ClientErrorMapping.TryGetValue(statusCode, out var errorData))
            {
                problemDetails.Title = errorData.Title;
                problemDetails.Type = errorData.Link;
            }

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
