using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Text;

namespace AspNetCore.Mvc.MvcAsApi.Extensions
{
    public static class ProblemDetailsExtensions
    {
        public static IActionResult ToActionResult(this ProblemDetails problemDetails)
        {
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
        }
    }
}
