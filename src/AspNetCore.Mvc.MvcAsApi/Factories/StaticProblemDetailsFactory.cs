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
    public static class StaticProblemDetailsFactory
    {
        private static readonly string _traceIdentifierKey = "traceId";
        private static readonly string _timeGeneratedKey = "timeGenerated";

        public static ValidationProblemDetails CreateValidationProblemDetails(HttpContext httpContext, ModelStateDictionary modelState, int? statusCode = StatusCodes.Status422UnprocessableEntity, string title = null, string type = null, string detail = null, string instance = null, bool addAngularFormattedErrors = false)
        {
            ValidationProblemDetails problemDetails;

            if (addAngularFormattedErrors)
            {
                problemDetails = new AngularValidationProblemDetails(modelState)
                {
                    //Title = "One or more validation errors occurred.", "Unprocessable Entity"
                    Type = type,
                    Instance = instance ?? httpContext.Request.Path,
                    Detail = detail ?? "Please refer to the errors property for additional details.",
                    Status = statusCode ?? StatusCodes.Status422UnprocessableEntity
                };
            }
            else
            {
                problemDetails = new ValidationProblemDetails(modelState)
                {
                    //Title = "One or more validation errors occurred.", "Unprocessable Entity"
                    Type = type,
                    Instance = instance ?? httpContext.Request.Path,
                    Detail = detail ?? "Please refer to the errors property for additional details.",
                    Status = statusCode ?? StatusCodes.Status422UnprocessableEntity
                };
            }

            if (title != null)
            {
                // For validation problem details, don't overwrite the default title with null.
                problemDetails.Title = title;
            }

            ApplyProblemDetailsDefaults(httpContext, problemDetails, problemDetails.Status.Value);

            return problemDetails;
        }

        public static ValidationProblemDetails CreateValidationProblemDetails(HttpContext httpContext, IDictionary<string, string[]> errors, int? statusCode = StatusCodes.Status422UnprocessableEntity, string title = null, string type = null, string detail = null, string instance = null, bool addAngularFormattedErrors = false)
        {
            ValidationProblemDetails problemDetails;
            if (addAngularFormattedErrors)
            {
                problemDetails = new AngularValidationProblemDetails(errors)
                {
                    //Title = "One or more validation errors occurred.", "Unprocessable Entity"
                    Type = type,
                    Instance = instance,
                    Detail = "Please refer to the errors property for additional details.",
                    Status = statusCode ?? StatusCodes.Status422UnprocessableEntity
                };
            }
            else
            {
                problemDetails = new ValidationProblemDetails(errors)
                {
                    //Title = "One or more validation errors occurred.", "Unprocessable Entity"
                    Type = type,
                    Instance = instance,
                    Detail = "Please refer to the errors property for additional details.",
                    Status = statusCode ?? StatusCodes.Status422UnprocessableEntity
                };
            }

            if (title != null)
            {
                // For validation problem details, don't overwrite the default title with null.
                problemDetails.Title = title;
            }

            ApplyProblemDetailsDefaults(httpContext, problemDetails, problemDetails.Status.Value);

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

        public static ProblemDetails CreateProblemDetails(HttpContext httpContext, int? statusCode = StatusCodes.Status500InternalServerError, string title = null, string type = null, string detail = null, string instance = null)
        {
            var problemDetails = new ProblemDetails()
            {
                Type = type,
                Title = title,
                Instance = instance,
                Status = statusCode ?? StatusCodes.Status500InternalServerError,
                Detail = detail
            };

            ApplyProblemDetailsDefaults(httpContext, problemDetails, problemDetails.Status.Value);

            return problemDetails;
        }

        private static void ApplyProblemDetailsDefaults(HttpContext httpContext, ProblemDetails problemDetails, int statusCode)
        {
            var apiBehaviorOptions = httpContext.RequestServices.GetService<IOptions<ApiBehaviorOptions>>()?.Value;

            if (apiBehaviorOptions != null && apiBehaviorOptions.ClientErrorMapping.TryGetValue(statusCode, out var clientErrorData))
            {
                problemDetails.Title = problemDetails.Title ?? clientErrorData.Title;
                problemDetails.Type = problemDetails.Type ?? clientErrorData.Link;
            }

            problemDetails.Instance = problemDetails.Instance ?? httpContext.Request.Path;

            SetTraceId(httpContext, problemDetails);
            SetTimeGenerated(problemDetails);
        }

        private static void SetTraceId(HttpContext httpContext, ProblemDetails problemDetails)
        {
            var traceId = Activity.Current?.Id ?? httpContext.TraceIdentifier;
            problemDetails.Extensions[_traceIdentifierKey] = traceId;
        }

        private static void SetTimeGenerated(ProblemDetails problemDetails)
        {
            problemDetails.Extensions[_timeGeneratedKey] = DateTime.UtcNow;
        }
    }
}
