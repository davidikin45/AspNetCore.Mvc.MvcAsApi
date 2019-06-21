using AspNetCore.Mvc.MvcAsApi.ErrorHandling;
using AspNetCore.Mvc.MvcAsApi.Factories;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;

namespace AspNetCore.Mvc.MvcAsApi.Extensions
{
    //https://github.com/aspnet/AspNetCore/blob/c565386a3ed135560bc2e9017aa54a950b4e35dd/src/Mvc/Mvc.Core/src/ApplicationModels/ApiBehaviorApplicationModelProvider.cs

    //ModelState
    //https://github.com/aspnet/AspNetCore/blob/c565386a3ed135560bc2e9017aa54a950b4e35dd/src/Mvc/Mvc.Core/src/ApplicationModels/InvalidModelStateFilterConvention.cs
    //https://github.com/aspnet/AspNetCore/blob/c565386a3ed135560bc2e9017aa54a950b4e35dd/src/Mvc/Mvc.Core/src/Infrastructure/ModelStateInvalidFilterFactory.cs
    //https://github.com/aspnet/AspNetCore/blob/c565386a3ed135560bc2e9017aa54a950b4e35dd/src/Mvc/Mvc.Core/src/Infrastructure/ModelStateInvalidFilter.cs
    public static class ProblemDetailsInvalidModelStateFactoryExtensions
    {
        public static IServiceCollection ConfigureProblemDetailsInvalidModelStateFactory(this IServiceCollection services, Action<ProblemDetailsInvalidModelStateFactoryOptions> setupAction = null)
        {
            var problemDetailsInvalidModelStateFactoryOptions = new ProblemDetailsInvalidModelStateFactoryOptions();
            if (setupAction != null)
                setupAction(problemDetailsInvalidModelStateFactoryOptions);

            services.Configure<ApiBehaviorOptions>(options =>
            {
                //Overrides the default InvalidModelStateResponseFactory, adds traceId and timeGenerated to the ProblemDetails response. 
                options.ConfigureProblemDetailsInvalidModelStateFactory(problemDetailsInvalidModelStateFactoryOptions);
            });


            if (problemDetailsInvalidModelStateFactoryOptions.ConfigureApiBehaviorOptions != null)
            {
                services.Configure(problemDetailsInvalidModelStateFactoryOptions.ConfigureApiBehaviorOptions);
            }

            return services;
        }

        //Needs to be after AddMvc or use ConfigureApiBehaviourOptions
        public static void ConfigureProblemDetailsInvalidModelStateFactory(this ApiBehaviorOptions options, ProblemDetailsInvalidModelStateFactoryOptions problemDetailsInvalidModelStateFactoryOptions)
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

            options.ClientErrorMapping[StatusCodes.Status422UnprocessableEntity] = new ClientErrorData
            {
                Link = "https://tools.ietf.org/html/rfc4918#section-11.2",
                Title = "One or more validation errors occurred.", //Unprocessable Entity
            };

            options.InvalidModelStateResponseFactory = actionContext =>
            {
                var actionExecutingContext =
                    actionContext as Microsoft.AspNetCore.Mvc.Filters.ActionExecutingContext;

                var problemDetails = new ValidationProblemDetails(actionContext.ModelState)
                {
                    //Title = "One or more validation errors occurred.", Unprocessable Entity
                    Instance = actionContext.HttpContext.Request.Path,
                    Detail = "Please refer to the errors property for additional details."
                };

                // if there are modelstate errors & all keys were correctly
                // found/parsed we're dealing with validation errors
                if (actionContext.ModelState.ErrorCount > 0
                    && actionExecutingContext?.ActionArguments.Count == actionContext.ActionDescriptor.Parameters.Count)
                {
                    problemDetails.Status = StatusCodes.Status422UnprocessableEntity;
                }
                else
                {
                    // if one of the keys wasn't correctly found / couldn't be parsed
                    // we're dealing with null/unparsable input
                    problemDetails.Status = StatusCodes.Status400BadRequest;
                }

                if (problemDetails.Status is int statusCode && options.ClientErrorMapping.TryGetValue(statusCode, out var errorData))
                {
                    problemDetails.Title = errorData.Title;
                    problemDetails.Type = errorData.Link;
                }

                if (problemDetailsInvalidModelStateFactoryOptions.EnableAngularErrors)
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

                ProblemDetailsTraceFactory.SetTraceId(actionContext.HttpContext, problemDetails);
                ProblemDetailsTraceFactory.SetTimeGenerated(problemDetails);

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

    public class ProblemDetailsInvalidModelStateFactoryOptions
    {
        public Action<ApiBehaviorOptions> ConfigureApiBehaviorOptions { get; set; }

        public bool EnableAngularErrors { get; set; } = false;
    }
}
