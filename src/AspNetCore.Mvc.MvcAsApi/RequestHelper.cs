using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.AspNetCore.Mvc.Formatters.Internal;
using Microsoft.Net.Http.Headers;
using System;
using System.Collections.Generic;
using System.Text;

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
            if (request.Path.ToString().Contains("/api"))
                return false;

            var result = new List<MediaTypeSegmentWithQuality>();

            AcceptHeaderParser.ParseAcceptHeader(request.Headers[HeaderNames.Accept], result);
            for (var i = 0; i < result.Count; i++)
            {
                var mediaType = new MediaType(result[i].MediaType);
                if (result[i].MediaType == "text/html" || (mediaType.MatchesAllSubTypes && mediaType.MatchesAllTypes)) //*.*
                {
                    return true;
                }
            }

            return false;
        }

        public static bool IsBrowser(this HttpRequest request)
        {
            var result = new List<MediaTypeSegmentWithQuality>();

            AcceptHeaderParser.ParseAcceptHeader(request.Headers[HeaderNames.Accept], result);
            for (var i = 0; i < result.Count; i++)
            {
                var mediaType = new MediaType(result[i].MediaType);

                if (mediaType.MatchesAllSubTypes && mediaType.MatchesAllTypes) //*.*
                {
                    return true;
                }
            }

            return false;
        }
    }
}