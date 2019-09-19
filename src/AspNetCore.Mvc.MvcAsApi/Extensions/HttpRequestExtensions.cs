using AspNetCore.Mvc.MvcAsApi.Internal;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;
using Microsoft.Net.Http.Headers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using MediaTypeSegmentWithQuality = AspNetCore.Mvc.MvcAsApi.Internal.MediaTypeSegmentWithQuality;

namespace AspNetCore.Mvc.MvcAsApi.Extensions
{
    public static class HttpRequestExtensions
    {
        //Api Error Handling
        public static bool IsApi(this HttpRequest request)
        {
            return !IsMvc(request);
        }

        //Mvc Error Handling
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

        //We could do this check at an Action Level but for error handling generally you want all actions for a controller to behave the same by default and differ when an explicit accept header is sent in.
        public static Func<HttpContext, bool> IsApiControllerDelegate { get; set; } = (context) =>
        {
            //requires app.UseEndpointRouting(); or app.UseRouting();
            var endpointFeature = context.Features.Get<IEndpointFeature>();

            if (endpointFeature != null)
            {
                var endpoint = endpointFeature.Endpoint;

                if (endpoint != null)
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

        public static bool IsBrowser(this HttpRequest request)
        {
            return IsBrowserDelegate(request.HttpContext);
        }

        public static Func<HttpContext, bool> IsBrowserDelegate { get; set; } = (context) =>
         {
             var isMvcController = !IsApiControllerDelegate(context);

             var result = Internal.AcceptHeaderParser.ParseAcceptHeader(context.Request.Headers[HeaderNames.Accept]);

             if (isMvcController)
             {
                 if (result.Count == 1)
                 {
                     var mediaType = new Internal.MediaType(result[0].MediaType);
                     if (!(mediaType.MatchesAllSubTypes && mediaType.MatchesAllTypes) && result[0].MediaType != "text/html")
                     {
                         var outputFormatter = context.Request.SelectFormatterUsingSortedAcceptHeaders(typeof(Object), new object(), new List<MediaTypeSegmentWithQuality>() { result[0] });
                         if (outputFormatter != null)
                         {
                             return false;
                         }
                     }
                 }

                 return true;
             }
             else
             {
                 if (result.Count == 1)
                 {
                     if (result[0].MediaType == "text/html")
                     {
                         return true;
                     }
                 }

                 return false;
             }
         };

        private static readonly Comparison<MediaTypeSegmentWithQuality> _sortFunction = (left, right) =>
        {
            return left.Quality > right.Quality ? -1 : (left.Quality == right.Quality ? 0 : 1);
        };

        internal static IOutputFormatter SelectFormatterUsingSortedAcceptHeaders(this HttpRequest request, Type objectType, object @object, List<MediaTypeSegmentWithQuality> sortedAcceptHeaders)
        {
            var options = request.HttpContext.RequestServices.GetService<IOptions<MvcOptions>>()?.Value;

            if (options == null)
                return null;

            Func<Stream, Encoding, TextWriter> writerFactory = (stream, encoding) => null;
            var formatterContext = new OutputFormatterWriteContext(
                request.HttpContext,
                writerFactory,
                objectType,
                @object);

            IOutputFormatter selectedFormatter = null;
            if (sortedAcceptHeaders.Count > 0)
            {
                selectedFormatter = request.SelectFormatterUsingSortedAcceptHeaders(formatterContext, options.OutputFormatters, sortedAcceptHeaders);
            }

            return selectedFormatter;
        }

        internal static IOutputFormatter SelectFormatterUsingSortedAcceptHeaders(
          this HttpRequest request,
          OutputFormatterCanWriteContext formatterContext,
          IList<IOutputFormatter> formatters,
          IList<MediaTypeSegmentWithQuality> sortedAcceptHeaders)
        {
            if (formatterContext == null)
            {
                throw new ArgumentNullException(nameof(formatterContext));
            }

            if (formatters == null)
            {
                throw new ArgumentNullException(nameof(formatters));
            }

            if (sortedAcceptHeaders == null)
            {
                throw new ArgumentNullException(nameof(sortedAcceptHeaders));
            }

            for (var i = 0; i < sortedAcceptHeaders.Count; i++)
            {

                var mediaType = sortedAcceptHeaders[i];

                formatterContext.ContentType = mediaType.MediaType;
                formatterContext.ContentTypeIsServerDefined = false;

                for (var j = 0; j < formatters.Count; j++)
                {
                    var formatter = formatters[j];

                    if (formatter is OutputFormatter)
                    {
                        var outputForamtter = formatter as OutputFormatter;
                        if (outputForamtter.CanWriteResult(formatterContext) && OutputFormatterSupportsMediaType(outputForamtter, formatterContext))
                        {
                            return formatter;
                        }
                    }
                }
            }

            return null;
        }

        public static IOutputFormatter SelectFormatterNotUsingContentType(
           this HttpRequest request,
            Type objectType, 
            object @object)
        {
            var options = request.HttpContext.RequestServices.GetService<IOptions<MvcOptions>>()?.Value;

            if (options == null)
                return null;

            Func<Stream, Encoding, TextWriter> writerFactory = (stream, encoding) => null;
            var formatterContext = new OutputFormatterWriteContext(
                request.HttpContext,
                writerFactory,
                objectType,
                @object);

            foreach (var formatter in options.OutputFormatters)
            {
                formatterContext.ContentType = new StringSegment();
                formatterContext.ContentTypeIsServerDefined = false;

                if (formatter.CanWriteResult(formatterContext))
                {
                    return formatter;
                }
            }

            return null;
        }

        private static bool OutputFormatterSupportsMediaType(OutputFormatter outputForamtter, OutputFormatterCanWriteContext context)
        {
            var parsedContentType = new Internal.MediaType(context.ContentType);
            for (var i = 0; i < outputForamtter.SupportedMediaTypes.Count; i++)
            {
                var supportedMediaType = new Internal.MediaType(outputForamtter.SupportedMediaTypes[i]);
                if (supportedMediaType.HasWildcard)
                {
                    // For supported media types that are wildcard patterns, confirm that the requested
                    // media type satisfies the wildcard pattern (e.g., if "text/entity+json;v=2" requested
                    // and formatter supports "text/*+json").
                    // We only do this when comparing against server-defined content types (e.g., those
                    // from [Produces] or Response.ContentType), otherwise we'd potentially be reflecting
                    // back arbitrary Accept header values.
                    if (context.ContentTypeIsServerDefined
                        && parsedContentType.IsSubsetOf(supportedMediaType))
                    {
                        return true;
                    }
                }
                else
                {
                    // For supported media types that are not wildcard patterns, confirm that this formatter
                    // supports a more specific media type than requested e.g. OK if "text/*" requested and
                    // formatter supports "text/plain".
                    // contentType is typically what we got in an Accept header.
                    if (supportedMediaType.IsSubsetOf(parsedContentType))
                    {
                        context.ContentType = new StringSegment(outputForamtter.SupportedMediaTypes[i]);
                        return true;
                    }
                }
            }

            return false;
        }
        internal static List<MediaTypeSegmentWithQuality> GetAcceptableMediaTypes(this HttpRequest request)
        {
            var mvcOptions = request.HttpContext.RequestServices.GetRequiredService<IOptions<MvcOptions>>()?.Value;

            var result = new List<MediaTypeSegmentWithQuality>();

            //If the accept header contains '*/*' or 'text/html' ignore all accept headers
            Internal.AcceptHeaderParser.ParseAcceptHeader(request.Headers[HeaderNames.Accept], result);
            for (var i = 0; i < result.Count; i++)
            {
                var mediaType = new Internal.MediaType(result[i].MediaType);
                if ((!mvcOptions.RespectBrowserAcceptHeader && mediaType.MatchesAllSubTypes && mediaType.MatchesAllTypes))
                {
                    result.Clear();
                    return result;
                }
            }

            result.Sort(_sortFunction);

            return result;
        }

        public static bool HasAcceptHeaders(this HttpRequest request)
        {
            var result = Internal.AcceptHeaderParser.ParseAcceptHeader(request.Headers[HeaderNames.Accept]);

            return result.Count > 0;
        }
    }
}
