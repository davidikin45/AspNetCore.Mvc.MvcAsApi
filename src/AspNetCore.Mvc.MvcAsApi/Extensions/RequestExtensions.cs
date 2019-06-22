using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using System.Threading.Tasks;
using WebApiContrib.Core.Results;

namespace AspNetCore.Mvc.MvcAsApi.Extensions
{
    public static class RequestExtensions
    {
        public static async Task WriteProblemDetailsResultAsync(this HttpContext context, ProblemDetails problemDetails)
        {
            var apiBehaviorOptions = context.RequestServices.GetService<IOptions<ApiBehaviorOptions>>()?.Value;

            if (apiBehaviorOptions != null)
            {
                if (problemDetails.Status is int statusCode && apiBehaviorOptions != null && apiBehaviorOptions.ClientErrorMapping.TryGetValue(statusCode, out var errorData))
                {
                    problemDetails.Title = errorData.Title;
                    problemDetails.Type = errorData.Link;
                }

                var result = new ObjectResult(problemDetails)
                {
                    StatusCode = problemDetails.Status,
                    ContentTypes =
                    {
                        "application/problem+json",
                        "application/problem+xml",
                    },
                };

                await context.WriteActionResult(result);
            }
            else
            {
                var message = JsonConvert.SerializeObject(problemDetails);
                context.Response.StatusCode = problemDetails.Status.HasValue ? problemDetails.Status.Value : 400;
                context.Response.ContentType = "application/problem+json";
                await context.Response.WriteAsync(message).ConfigureAwait(false);
            }
        }
    }
}
