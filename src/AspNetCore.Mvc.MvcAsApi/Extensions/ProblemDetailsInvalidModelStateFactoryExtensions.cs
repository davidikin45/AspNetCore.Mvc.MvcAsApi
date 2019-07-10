using AspNetCore.Mvc.MvcAsApi.Factories;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using System;

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

            options.InvalidModelStateResponseFactory = problemDetailsInvalidModelStateFactoryOptions.InvalidModelStateResponseAbstractFactory(problemDetailsInvalidModelStateFactoryOptions.EnableAngularErrors);
        }

    }

    public class ProblemDetailsInvalidModelStateFactoryOptions
    {
        public Action<ApiBehaviorOptions> ConfigureApiBehaviorOptions { get; set; }

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
                if (actionContext.ModelState.ErrorCount > 0
                    && actionExecutingContext?.ActionArguments.Count == actionContext.ActionDescriptor.Parameters.Count)
                {
                    status = StatusCodes.Status422UnprocessableEntity;
                }
                else
                {
                    // if one of the keys wasn't correctly found / couldn't be parsed
                    // we're dealing with null/unparsable input
                    status = StatusCodes.Status400BadRequest;
                }

                var problemDetails = ProblemDetailsFactory.GetValidationProblemDetails(actionContext.HttpContext, actionContext.ModelState, status, enableAngularErrors);

                var result = new ObjectResult(problemDetails)
                {
                    StatusCode = problemDetails.Status,
                    ContentTypes =
                    {
                        "application/problem+json",
                        "application/problem+xml",
                    },
                };

                actionContext.HttpContext.Items["MvcErrorHandled"] = true;
                return result;
            };
        };
    }
}

