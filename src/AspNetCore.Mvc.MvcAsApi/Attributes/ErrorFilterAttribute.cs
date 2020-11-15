using AspNetCore.Mvc.MvcAsApi.Extensions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;

namespace AspNetCore.Mvc.MvcAsApi.Attributes
{
    //Replaces ClientErrorResultFilter
    //https://github.com/dotnet/aspnetcore/blob/c565386a3ed135560bc2e9017aa54a950b4e35dd/src/Mvc/Mvc.Core/src/Infrastructure/ClientErrorResultFilter.cs

    public class ApiErrorFilterAttribute : ErrorFilterAttribute
    {
        public ApiErrorFilterAttribute()
         : this(null)
        {

        }

        public ApiErrorFilterAttribute(Action<ApiErrorFilterOptions> setupAction)
        : base(false, true, ConfigureOptions(setupAction))
        {

        }

        private static ApiErrorFilterOptions ConfigureOptions(Action<ApiErrorFilterOptions> setupAction)
        {
            var options = new ApiErrorFilterOptions();
            if (setupAction != null)
                setupAction(options);

            return options;
        }
    }

    public class MvcErrorFilterAttribute : ErrorFilterAttribute
    {
        public MvcErrorFilterAttribute()
         : this(null)
        {

        }

        public MvcErrorFilterAttribute(Action<MvcErrorFilterOptions> setupAction)
        : base(true, false, ConfigureOptions(setupAction))
        {

        }

        private static MvcErrorFilterOptions ConfigureOptions(Action<MvcErrorFilterOptions> setupAction)
        {
            var options = new MvcErrorFilterOptions();
            if (setupAction != null)
                setupAction(options);

            return options;
        }
    }

    public class ErrorFilterAttribute : TypeFilterAttribute
    {
        public ErrorFilterAttribute(bool handleMvcRequests, bool handleApiRequests, ErrorFilterOptions options)
       :base(typeof(ErrorFilterImpl))
        {
            Arguments = new object[] { handleMvcRequests, handleApiRequests, options};
        }

        //https://github.com/aspnet/AspNetCore/blob/c565386a3ed135560bc2e9017aa54a950b4e35dd/src/Mvc/Mvc.Core/src/Infrastructure/ClientErrorResultFilter.cs#L44
        private class ErrorFilterImpl : IAlwaysRunResultFilter, IOrderedFilter
        {
            private readonly bool _handleMvcRequests;
            private readonly bool _handleApiRequests;
            private readonly ILogger _logger;
            internal const int FilterOrder = -2000;
            private readonly IClientErrorFactory _clientErrorFactory;

            private readonly ErrorFilterOptions _options;

            public int Order => FilterOrder;

            public ErrorFilterImpl(IClientErrorFactory clientErrorFactory, ILoggerFactory loggerFactory, bool handleMvcRequests, bool handleApiRequests, ErrorFilterOptions options)
            {
                _clientErrorFactory = clientErrorFactory ?? throw new ArgumentNullException(nameof(clientErrorFactory));
                _logger = loggerFactory.CreateLogger<ErrorFilterAttribute>();
                _handleMvcRequests = handleMvcRequests;
                _handleApiRequests = handleApiRequests;
                _options = options;
            }

            public void OnResultExecuting(ResultExecutingContext context)
            {
                if ((!_handleMvcRequests && context.HttpContext.Request.IsMvc()) || (!_handleApiRequests && context.HttpContext.Request.IsApi()) ||!(context.Result is IClientErrorActionResult clientError))
                {
                    return;
                }

                // We do not have an upper bound on the allowed status code. This allows this filter to be used
                // for 5xx and later status codes.
                if (!_options.HandleError(context, _options, clientError))
                {
                    return;
                }

                var factory = (clientError.StatusCode.HasValue && _options.ActionResultFactories.ContainsKey(clientError.StatusCode.Value)) ? _options.ActionResultFactories[clientError.StatusCode.Value] : _options.DefaultActionResultFactory ?? null;

                if (factory == null)
                {
                    return;
                }

                var result = factory(context, _logger, clientError);
                if (result == null)
                {
                    return;
                }

                _logger.TransformingClientError(context.Result.GetType(), result?.GetType(), clientError.StatusCode);

                context.HttpContext.Items["MvcErrorHandled"] = true;
                context.Result = result;
            }

            public void OnResultExecuted(ResultExecutedContext context)
            {
               
            }
        }
    }

    public abstract class ErrorFilterOptions
    {
        public Func<ResultExecutingContext, ErrorFilterOptions, IClientErrorActionResult, bool> HandleError { get; set; } = ((context, options, clientError) => ((clientError.StatusCode >= 400 && options.DefaultActionResultFactory != null) || (clientError.StatusCode.HasValue && options.ActionResultFactories.ContainsKey(clientError.StatusCode.Value))));

        public delegate IActionResult ActionResultFactoryDelegate(ResultExecutingContext context, ILogger logger, IClientErrorActionResult clientError);

        public virtual ActionResultFactoryDelegate DefaultActionResultFactory { get; set; } = null;
        public virtual Dictionary<int, ActionResultFactoryDelegate> ActionResultFactories { get; set; } = new Dictionary<int, ActionResultFactoryDelegate>()
        {

        };

        public void Clear()
        {
            DefaultActionResultFactory = null;
            ActionResultFactories.Clear();
        }
    }

    public class MvcErrorFilterOptions : ErrorFilterOptions
    {

    }

    public class ApiErrorFilterOptions : ErrorFilterOptions
    {
        public override ActionResultFactoryDelegate DefaultActionResultFactory { get; set; } = ((context, logger, clientError) =>
        {
            var clientErrorFactory = context.HttpContext.RequestServices.GetService<IClientErrorFactory>();
            return clientErrorFactory.GetClientError(context, clientError);
        });

        public override Dictionary<int, ActionResultFactoryDelegate> ActionResultFactories { get; set; } = new Dictionary<int, ActionResultFactoryDelegate>()
        {

        };
    }
}
