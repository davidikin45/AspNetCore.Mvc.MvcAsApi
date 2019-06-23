using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.AspNetCore.Mvc.Formatters.Internal;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.Net.Http.Headers;
using System;
using System.Collections.Generic;
using System.Linq;

namespace AspNetCore.Mvc.MvcAsApi
{
    public static class RequestHelper
    {
        public static bool IsApi(this HttpRequest request)
        {
            return !IsMvc(request);
        }

        public static bool IsMvc(this HttpRequest request)
        {
            return IsBrowserDelegate(request.HttpContext);
        }

        public static List<string> ApiControllerNames = new List<string>()
        {
            "api"
        };

        public static Func<HttpContext, bool> IsPossibleApiControllerDelegate { get; set; } = (context) =>
        {
            return ApiControllerNames.Any(search => context.Request.Host.ToString().Contains(search) || context.Request.Path.ToString().Contains(search));
        };

        public static Func<HttpContext, bool> IsApiControllerDelegate { get; set; } = (context) =>
        {
            //requires app.UseEndpointRouting(); or app.UseRouting();
            var endpointFeature = context.Features.Get<IEndpointFeature>();

            if (endpointFeature != null)
            {
                var endpoint = endpointFeature.Endpoint;

                if(endpoint != null)
                {
                    //endpoint found
                    var controllerACtionDescriptor = endpoint.Metadata.OfType<ApiControllerAttribute>().FirstOrDefault();

                    var isApiController = controllerACtionDescriptor != null;

                    return isApiController;
                }
            }

            //404 - Route not found or endpoint routing is disabled.
            if (IsPossibleApiControllerDelegate(context))
            {
                return true;
            }

            return false;
        };

        private static readonly Comparison<MediaTypeSegmentWithQuality> _sortFunction = (left, right) =>
        {
            return left.Quality > right.Quality ? -1 : (left.Quality == right.Quality ? 0 : 1);
        };

        public static List<MediaTypeSegmentWithQuality> GetAcceptableMediaTypes(this HttpRequest request)
        {
            var mvcOptions = request.HttpContext.RequestServices.GetRequiredService<IOptions<MvcOptions>>()?.Value;

            var result = new List<MediaTypeSegmentWithQuality>();

            //If the accept header contains '*/*' or 'text/html' ignore all accept headers
            AcceptHeaderParser.ParseAcceptHeader(request.Headers[HeaderNames.Accept], result);
            for (var i = 0; i < result.Count; i++)
            {
                var mediaType = new MediaType(result[i].MediaType);
                if ((!mvcOptions.RespectBrowserAcceptHeader && mediaType.MatchesAllSubTypes && mediaType.MatchesAllTypes) || result[i].MediaType == "text/html")
                {
                    result.Clear();
                    return result;
                }
            }

            result.Sort(_sortFunction);

            return result;
        }

        public static bool IsBrowser(this HttpRequest request)
        {
            return IsBrowserDelegate(request.HttpContext);
        }

        public static Func<HttpContext, bool> IsBrowserDelegate { get; set; } = (context) =>
         {
             var isMvcController = !IsApiControllerDelegate(context);

             //Allowing MvcController to be Mvc/Api but not allowing ApiController to be Api/Mvc.
             if (isMvcController)
             {
                 var acceptableMediaTypes = context.Request.GetAcceptableMediaTypes();
                 if(acceptableMediaTypes.Count > 0)
                 {
                     return false;
                 }
                 else
                 {
                     return true;
                 }
             }
             else
             {
                 var result = new List<MediaTypeSegmentWithQuality>();

                 AcceptHeaderParser.ParseAcceptHeader(context.Request.Headers[HeaderNames.Accept], result);

                 if (result.Count == 1 && result[0].MediaType == "text/html")
                 {
                     return true;
                 }
             }

             //otherwise we aren't from browser.
             return false;
         };
    }
}
