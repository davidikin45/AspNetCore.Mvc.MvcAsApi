using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace AspNetCore.Mvc.MvcAsApi.Factories
{
    public static class ProblemDetailsTraceFactory
    {
        private static readonly string TraceIdentifierKey = "traceId";
        private static readonly string TimeGeneratedKey = "timeGenerated";

        public static ValidationProblemDetails GetValidationProblemDetails(HttpContext httpContext, ModelStateDictionary modelState, int? status)
        {
            var apiBehaviorOptions = httpContext.RequestServices.GetService<IOptions<ApiBehaviorOptions>>()?.Value;

            var problemDetails = new ValidationProblemDetails(modelState)
            {
                //Title = "One or more validation errors occurred.", Unprocessable Entity
                Instance = httpContext.Request.Path,
                Detail = "Please refer to the errors property for additional details.",
                Status = status
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

        public static ValidationProblemDetails GetValidationProblemDetails(HttpContext httpContext, IDictionary<string, string[]> errors, int? status)
        {
            var apiBehaviorOptions = httpContext.RequestServices.GetService<IOptions<ApiBehaviorOptions>>()?.Value;

            var problemDetails = new ValidationProblemDetails(errors)
            {
                //Title = "One or more validation errors occurred.", Unprocessable Entity
                Instance = httpContext.Request.Path,
                Detail = "Please refer to the errors property for additional details.",
                Status = status
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
