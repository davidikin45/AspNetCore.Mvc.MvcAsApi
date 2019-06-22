using AspNetCore.Mvc.MvcAsApi.ErrorHandling;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace AspNetCore.Mvc.MvcAsApi.Factories
{
    public static class ProblemDetailsFactory
    {
        private static readonly string TraceIdentifierKey = "traceId";
        private static readonly string TimeGeneratedKey = "timeGenerated";

        public static ValidationProblemDetails GetValidationProblemDetails(HttpContext httpContext, ModelStateDictionary modelState, int? status = StatusCodes.Status422UnprocessableEntity, bool addAngularFormattedErrors = false)
        {
            var apiBehaviorOptions = httpContext.RequestServices.GetService<IOptions<ApiBehaviorOptions>>()?.Value;

            var problemDetails = new ValidationProblemDetails(modelState)
            {
                //Title = "One or more validation errors occurred.", Unprocessable Entity
                Instance = httpContext.Request.Path,
                Detail = "Please refer to the errors property for additional details.",
                Status = status
            };

            if (addAngularFormattedErrors)
            {
                AddAngularFormatteErrors(problemDetails);
            }

            SetTraceId(httpContext, problemDetails);
            SetTimeGenerated(problemDetails);

            if (status is int statusCode && apiBehaviorOptions != null && apiBehaviorOptions.ClientErrorMapping.TryGetValue(statusCode, out var errorData))
            {
                problemDetails.Title = errorData.Title;
                problemDetails.Type = errorData.Link;
            }

            return problemDetails;
        }

        public static ValidationProblemDetails GetValidationProblemDetails(HttpContext httpContext, IDictionary<string, string[]> errors, int? status = StatusCodes.Status422UnprocessableEntity, bool addAngularFormattedErrors = false)
        {
            var apiBehaviorOptions = httpContext.RequestServices.GetService<IOptions<ApiBehaviorOptions>>()?.Value;

            var problemDetails = new ValidationProblemDetails(errors)
            {
                //Title = "One or more validation errors occurred.", Unprocessable Entity
                Instance = httpContext.Request.Path,
                Detail = "Please refer to the errors property for additional details.",
                Status = status
            };

            if(addAngularFormattedErrors)
            {
                AddAngularFormatteErrors(problemDetails);
            }

            SetTraceId(httpContext, problemDetails);
            SetTimeGenerated(problemDetails);

            if (status is int statusCode && apiBehaviorOptions != null && apiBehaviorOptions.ClientErrorMapping.TryGetValue(statusCode, out var errorData))
            {
                problemDetails.Title = errorData.Title;
                problemDetails.Type = errorData.Link;
            }

            return problemDetails;
        }

        private static void AddAngularFormatteErrors(ValidationProblemDetails problemDetails)
        {
            var angularErrors = new SerializableDictionary<string, List<AngularFormattedValidationError>>();
            foreach (var kvp in problemDetails.Errors)
            {
                var propertyMessages = new List<AngularFormattedValidationError>();
                foreach (var errorMessage in kvp.Value)
                {
                    var keyAndMessage = errorMessage.Split('|');
                    if (keyAndMessage.Count() > 1)
                    {
                        //Formatted for Angular Binding
                        //e.g required|Error Message
                        propertyMessages.Add(new AngularFormattedValidationError(
                            keyAndMessage[1],
                            keyAndMessage[0]));
                    }
                    else
                    {
                        propertyMessages.Add(new AngularFormattedValidationError(
                            keyAndMessage[0]));
                    }
                }

                angularErrors.Add(kvp.Key, propertyMessages);
            }
            problemDetails.Extensions["angularErrors"] = angularErrors;
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
