﻿using AspNetCore.Mvc.MvcAsApi.Factories;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.Extensions.DependencyInjection;
using System;
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
        public static IMvcBuilder ConfigureMvcProblemDetailsInvalidModelStateFactory(this IMvcBuilder builder, Action<ProblemDetailsInvalidModelStateFactoryOptions> setupAction = null)
        {
            var services = builder.Services;

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

            return builder;
        }

        //Needs to be after AddMvc or use ConfigureApiBehaviourOptions
        public static void ConfigureProblemDetailsInvalidModelStateFactory(this ApiBehaviorOptions options, ProblemDetailsInvalidModelStateFactoryOptions problemDetailsInvalidModelStateFactoryOptions)
        {
            //.Net Core 2.2
            //400
            //401
            //403
            //404
            //406
            //409
            //415
            //422

            //.Net Core 3.0
            //400
            //401
            //403
            //404
            //406
            //409
            //415
            //422
            //500

            options.ClientErrorMapping[StatusCodes.Status422UnprocessableEntity] = new ClientErrorData
            {
                Link = "https://tools.ietf.org/html/rfc4918#section-11.2",
                Title = "One or more validation errors occurred.", //Unprocessable Entity
            };

            options.ClientErrorMapping[499] = new ClientErrorData
            {
                Link = "about:blank",
                Title = "The request was cancelled.",
            };

            options.ClientErrorMapping[StatusCodes.Status500InternalServerError] = new ClientErrorData
            {
                Link = "https://tools.ietf.org/html/rfc7231#section-6.6.1",
                Title = "An error occurred while processing your request.",
            };

            options.ClientErrorMapping[StatusCodes.Status504GatewayTimeout] = new ClientErrorData
            {
                Link = "about:blank",
                Title = "The request timed out.",
            };

#if NETCOREAPP3_0
            options.InvalidModelStateResponseFactory = problemDetailsInvalidModelStateFactoryOptions.InvalidModelStateResponseAbstractFactory();
#else
            options.InvalidModelStateResponseFactory = problemDetailsInvalidModelStateFactoryOptions.InvalidModelStateResponseAbstractFactory(problemDetailsInvalidModelStateFactoryOptions.EnableAngularErrors);
#endif
        }

    }

    public class ProblemDetailsInvalidModelStateFactoryOptions
    {
        public Action<ApiBehaviorOptions> ConfigureApiBehaviorOptions { get; set; }

#if NETCOREAPP3_0
        public Func<Func<ActionContext, IActionResult>> InvalidModelStateResponseAbstractFactory { get; set; } = () =>
        {
            return (context) =>
            {
                var actionExecutingContext =
                    context as Microsoft.AspNetCore.Mvc.Filters.ActionExecutingContext;

                int status;

                // if there are modelstate errors & all keys were correctly
                // found/parsed we're dealing with validation errors
                var bodyParams = context.ActionDescriptor.Parameters.Where(p => p.BindingInfo.BindingSource == null || p.BindingInfo.BindingSource.CanAcceptDataFrom(BindingSource.Body)).ToList();
                var bodyParamKeys = bodyParams.Select(p => p.Name).ToList();

                if (context.ModelState.ErrorCount > 0
                   & bodyParams.Count > 0 && actionExecutingContext?.ActionArguments.Keys.Where(key => bodyParamKeys.Contains(key)).Count() == bodyParams.Count)
                {
                    status = StatusCodes.Status422UnprocessableEntity;
                }
                else
                {
                    // if one of the keys wasn't correctly found / couldn't be parsed
                    // we're dealing with null/unparsable input
                    status = StatusCodes.Status400BadRequest;
                }

                var problemDetailsFactory = context.HttpContext.RequestServices.GetRequiredService<ProblemDetailsFactory>();

                var result = ProblemDetailsInvalidModelStateResponse(problemDetailsFactory, context, status);

                context.HttpContext.Items["MvcErrorHandled"] = true;
                return result;
            };
        };

        private static IActionResult ProblemDetailsInvalidModelStateResponse(ProblemDetailsFactory problemDetailsFactory, ActionContext context, int status)
        {
            var problemDetails = problemDetailsFactory.CreateValidationProblemDetails(context.HttpContext, context.ModelState, status);
            ObjectResult result;
            if (problemDetails.Status == 400)
            {
                // For compatibility with 2.x, continue producing BadRequestObjectResult instances if the status code is 400.
                result = new BadRequestObjectResult(problemDetails);
            }
            else
            {
                result = new ObjectResult(problemDetails)
                {
                    StatusCode = problemDetails.Status,
                };
            }
            result.ContentTypes.Add("application/problem+json");
            result.ContentTypes.Add("application/problem+xml");

            return result;
        }
#else

        public bool EnableAngularErrors { get; set; } = false;

        public Func<bool, Func<ActionContext, IActionResult>> InvalidModelStateResponseAbstractFactory { get; set; } = (enableAngularErrors) =>
        {
            return (actionContext) =>
            {
                var actionExecutingContext =
                    actionContext as Microsoft.AspNetCore.Mvc.Filters.ActionExecutingContext;

                int status;

                // if there are modelstate errors & all keys were correctly
                // found/parsed we're dealing with validation errors
                var bodyParams = actionContext.ActionDescriptor.Parameters.Where(p => p.BindingInfo.BindingSource == null || p.BindingInfo.BindingSource.CanAcceptDataFrom(BindingSource.Body)).ToList();
                var bodyParamKeys = bodyParams.Select(p => p.Name).ToList();

                if (actionContext.ModelState.ErrorCount > 0
                   & bodyParams.Count > 0 && actionExecutingContext?.ActionArguments.Keys.Where(key => bodyParamKeys.Contains(key)).Count() == bodyParams.Count)
                {
                    status = StatusCodes.Status422UnprocessableEntity;
                }
                else
                {
                    // if one of the keys wasn't correctly found / couldn't be parsed
                    // we're dealing with null/unparsable input
                    status = StatusCodes.Status400BadRequest;
                }

                var problemDetails = StaticProblemDetailsFactory.CreateValidationProblemDetails(actionContext.HttpContext, actionContext.ModelState, status, null, null, null, null, enableAngularErrors);

                ObjectResult result;
                if (problemDetails.Status == 400)
                {
                    // For compatibility with 2.x, continue producing BadRequestObjectResult instances if the status code is 400.
                    result = new BadRequestObjectResult(problemDetails);
                }
                else
                {
                    result = new ObjectResult(problemDetails)
                    {
                        StatusCode = problemDetails.Status,
                    };
                }
                result.ContentTypes.Add("application/problem+json");
                result.ContentTypes.Add("application/problem+xml");

                actionContext.HttpContext.Items["MvcErrorHandled"] = true;
                return result;
            };
        };
#endif
    }
}

