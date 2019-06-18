using AspNetCore.Mvc.MvcAsApi.ErrorHandling;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;

namespace AspNetCore.Mvc.MvcAsApi.Factories
{
    //https://github.com/aspnet/AspNetCore/blob/c565386a3ed135560bc2e9017aa54a950b4e35dd/src/Mvc/Mvc.Core/src/ApplicationModels/ApiBehaviorApplicationModelProvider.cs

    //ModelState
    //https://github.com/aspnet/AspNetCore/blob/c565386a3ed135560bc2e9017aa54a950b4e35dd/src/Mvc/Mvc.Core/src/ApplicationModels/InvalidModelStateFilterConvention.cs
    //https://github.com/aspnet/AspNetCore/blob/c565386a3ed135560bc2e9017aa54a950b4e35dd/src/Mvc/Mvc.Core/src/Infrastructure/ModelStateInvalidFilterFactory.cs
    //https://github.com/aspnet/AspNetCore/blob/c565386a3ed135560bc2e9017aa54a950b4e35dd/src/Mvc/Mvc.Core/src/Infrastructure/ModelStateInvalidFilter.cs
    public static class ApiBehaviorOptionsExtensions
    {
        //Needs to be after AddMvc or use ConfigureApiBehaviourOptions
        public static void EnableEnhancedValidationProblemDetails(this ApiBehaviorOptions options, bool enableAngularErrors = false)
        {

            //400
            //401
            //403
            //404
            //406
            //409
            //415
            //422
            options.ClientErrorMapping[StatusCodes.Status500InternalServerError] = new ClientErrorData
            {
                Link = "about:blank",
                Title = "An error has occured.",
            };

            options.ClientErrorMapping[499] = new ClientErrorData
            {
                Link = "about:blank",
                Title = "The request was cancelled.",
            };

            options.ClientErrorMapping[StatusCodes.Status504GatewayTimeout] = new ClientErrorData
            {
                Link = "about:blank",
                Title = "The request timed out.",
            };

            options.InvalidModelStateResponseFactory = actionContext =>
            {
                var actionExecutingContext =
                    actionContext as Microsoft.AspNetCore.Mvc.Filters.ActionExecutingContext;

                var problemDetails = new ValidationProblemDetails(actionContext.ModelState)
                {
                    Instance = actionContext.HttpContext.Request.Path,
                    Detail = "Please refer to the errors property for additional details."
                };

                // if there are modelstate errors & all keys were correctly
                // found/parsed we're dealing with validation errors
                if (actionContext.ModelState.ErrorCount > 0
                    && actionExecutingContext?.ActionArguments.Count == actionContext.ActionDescriptor.Parameters.Count)
                {
                    problemDetails.Type = "https://tools.ietf.org/html/rfc4918#section-11.2";
                    problemDetails.Status = StatusCodes.Status422UnprocessableEntity;
                }
                else
                {
                    // if one of the keys wasn't correctly found / couldn't be parsed
                    // we're dealing with null/unparsable input
                    problemDetails.Type = "https://tools.ietf.org/html/rfc7231#section-6.5.1";
                    problemDetails.Status = StatusCodes.Status400BadRequest;
                }

                if(enableAngularErrors)
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

                ProblemDetailsFactory.SetTraceId(actionContext.HttpContext, problemDetails);
                ProblemDetailsFactory.SetTimeGenerated(problemDetails);

                var result = new ObjectResult(problemDetails)
                {
                    StatusCode = problemDetails.Status,
                    ContentTypes =
                            {
                                "application/problem+json",
                                "application/problem+xml",
                            },
                };

                return result;
            };
        }

    }
}
