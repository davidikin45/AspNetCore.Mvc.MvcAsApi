using AspNetCore.Mvc.MvcAsApi.BindingSources;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ModelBinding.Binders;
using Microsoft.AspNetCore.Mvc.ModelBinding.Metadata;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace AspNetCore.Mvc.MvcAsApi.ModelBinding
{
    //https://github.com/aspnet/AspNetCore/blob/c565386a3ed135560bc2e9017aa54a950b4e35dd/src/Mvc/Mvc.Core/src/ModelBinding/ModelBinderFactory.cs
    //https://github.com/aspnet/AspNetCore/blob/c565386a3ed135560bc2e9017aa54a950b4e35dd/src/Mvc/Mvc.Core/src/ModelBinding/Binders/BodyModelBinder.cs
    //https://github.com/aspnet/AspNetCore/blob/c565386a3ed135560bc2e9017aa54a950b4e35dd/src/Mvc/Mvc.Core/src/Controllers/ControllerBinderDelegateProvider.cs
    //Uses Input Formatters
    public class BodyOrOtherSourcesModelBinder : IModelBinder
    {
        private readonly ILogger _logger;
        private readonly IModelBinderFactory _modelBinderFactory;
        private readonly IModelMetadataProvider _modelMetadataProvider;
        private readonly ICompositeMetadataDetailsProvider _detailsProvider;
        private readonly IModelBinderProvider[] _providers;
        private readonly DefaultModelBindingMessageProvider _modelBindingMessageProvider;

        public BodyOrOtherSourcesModelBinder(ILoggerFactory loggerFactory, IOptions<MvcOptions> options, IModelBinderFactory modelBinderFactory, IModelMetadataProvider modelMetadataProvider, ICompositeMetadataDetailsProvider detailsProvider)
        {
            _logger = loggerFactory.CreateLogger<BodyOrOtherSourcesModelBinder>();
            _modelBinderFactory = modelBinderFactory;
            _providers = options.Value.ModelBinderProviders.ToArray();
            _modelMetadataProvider = modelMetadataProvider;
            _modelBindingMessageProvider = options.Value.ModelBindingMessageProvider;
            _detailsProvider = detailsProvider;
        }

        public async Task BindModelAsync(ModelBindingContext bindingContext)
        {
            //Body
            bool tryOtherBindingSources = false;

            var bodyBinder = GetBinder(bindingContext.ModelMetadata, BindingSource.Form);

            if(bodyBinder != null)
            {
                await bodyBinder.BindModelAsync(bindingContext);

                if ((bindingContext.BindingSource == null || !bindingContext.BindingSource.CanAcceptDataFrom(BindingSource.Body)))
                {
                    if (!bindingContext.Result.IsModelSet)
                    {
                        tryOtherBindingSources = true;
                        bindingContext.ModelState.Clear();

                        _logger.LogDebug("Couldn't bind from Body, now binding from BindingSource: {0}", bindingContext.BindingSource != null ? bindingContext.BindingSource.DisplayName.Replace("BodyOr", "") : BindingSource.ModelBinding.DisplayName);
                    }
                }
            }
            else if ((bindingContext.BindingSource == null || !bindingContext.BindingSource.CanAcceptDataFrom(BindingSource.Body)))
            {
                tryOtherBindingSources = true;
            }

            //Form/Route/Query
            if (tryOtherBindingSources)
            {
                var binder = GetBinder(bindingContext.ModelMetadata, bindingContext.BindingSource ?? BodyAndBindingSource.BodyAndModelBinding);

                if (binder != null)
                {
                    await binder.BindModelAsync(bindingContext);
                }
            }
        }

        private IModelBinder GetBinder(ModelMetadata modelMetadata, BindingSource bindingSource)
        {
            dynamic identity = typeof(ModelMetadata).GetProperty("Identity", BindingFlags.Public | BindingFlags.Instance | BindingFlags.NonPublic).GetValue(modelMetadata);
            var key = ModelMetadataIdentity.ForParameter(identity.ParameterInfo, modelMetadata.ModelType);
            var newModelMetadata = new DefaultModelMetadata(_modelMetadataProvider, _detailsProvider, new DefaultMetadataDetails(key, ModelAttributes.GetAttributesForParameter(identity.ParameterInfo, modelMetadata.ModelType)), _modelBindingMessageProvider);

            newModelMetadata.BindingMetadata.BinderType = null;

            var factoryContext = new ModelBinderFactoryContext()
            {
                Metadata = newModelMetadata,
                BindingInfo = new BindingInfo()
                {
                    BinderModelName = newModelMetadata.BinderModelName,
                    BinderType = newModelMetadata.BinderType,// bypasses model binder providers if set
                    BindingSource = bindingSource,
                    PropertyFilterProvider = newModelMetadata.PropertyFilterProvider,
                },

                CacheToken = bindingSource,
            };

            var binder = _modelBinderFactory.CreateBinder(factoryContext);

            return binder;
        }
    }
}
