using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Internal;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ModelBinding.Binders;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;

namespace AspNetCore.Mvc.MvcAsApi.ModelBinding
{
    public class DynamicModelBinder : IModelBinder
    {
        private readonly IReadOnlyList<IValueProviderFactory> _valueProviderFactories;
        private readonly ILogger _logger;

        public DynamicModelBinder(IOptions<MvcOptions> options, ILoggerFactory loggerFactory)
        {
            _valueProviderFactories = options.Value.ValueProviderFactories.ToArray();
            _logger = loggerFactory.CreateLogger<DynamicModelBinder>();
        }

        public async Task BindModelAsync(ModelBindingContext bindingContext)
        {
            if (bindingContext == null)
            {
                throw new ArgumentNullException(nameof(bindingContext));
            }

            var modelName = bindingContext.ModelName;

            var valueProvider = await CompositeValueProvider.CreateAsync(bindingContext.ActionContext, new CopyOnWriteList<IValueProviderFactory>(_valueProviderFactories));

            JObject model = bindingContext.Model != null ? (JObject)bindingContext.Model : new JObject();

            //Form/Route/Query
            if (bindingContext.ModelMetadata.BindingSource == null || bindingContext.ModelMetadata.BindingSource.CanAcceptDataFrom(BindingSource.Form) || bindingContext.ModelMetadata.BindingSource.CanAcceptDataFrom(BindingSource.Query) || bindingContext.ModelMetadata.BindingSource.CanAcceptDataFrom(BindingSource.Path))
            {
                model = ParseProperties(modelName, valueProvider, model);
            }

            //Route 
            if (bindingContext.IsTopLevelObject)
            {
                if (bindingContext.ModelMetadata.BindingSource == null || bindingContext.ModelMetadata.BindingSource.CanAcceptDataFrom(BindingSource.Path))
                {
                    foreach (var kvp in bindingContext.ActionContext.RouteData.Values)
                    {
                        if (kvp.Key != "area" && kvp.Key != "controller" && kvp.Key != "action" && !model.ContainsKey(kvp.Key))
                        {
                            var stringValue = kvp.Value as string ?? Convert.ToString(kvp.Value, CultureInfo.InvariantCulture) ?? string.Empty;
                            if (!model.ContainsKey(kvp.Key))
                            {
                                model.Add(kvp.Key, GetValue(stringValue));
                            }
                            else if(bindingContext.HttpContext.Request.Query.ContainsKey(kvp.Key) && !bindingContext.HttpContext.Request.Form.ContainsKey(kvp.Key))
                            {
                                model[kvp.Key] = GetValue(stringValue);
                            }
                        }
                    }
                }
            }

            //Query
            if (bindingContext.IsTopLevelObject)
            {
                if (bindingContext.ModelMetadata.BindingSource == null || bindingContext.ModelMetadata.BindingSource.CanAcceptDataFrom(BindingSource.Query))
                {
                    foreach (var kvp in bindingContext.HttpContext.Request.Query)
                    {
                        if (!model.ContainsKey(kvp.Key))
                        {
                            model.Add(kvp.Key, GetValue(kvp.Value));
                        }
                        else if (!bindingContext.HttpContext.Request.Form.ContainsKey(kvp.Key) && !bindingContext.ActionContext.RouteData.Values.ContainsKey(kvp.Key))
                        {
                            model[kvp.Key] = GetValue(kvp.Value);
                        }
                    }
                }
            }

            bindingContext.Result = ModelBindingResult.Success(model);
        }

        public JObject ParseProperties(string modelName, CompositeValueProvider valueProvider, JObject model)
        {
            IDictionary<string, string> properties = valueProvider.GetKeysFromPrefix(modelName);

            _logger.LogDebug("DynamicModelBinder property count is " + properties.Count() + " for " + modelName);

            List<string> subModelNames = new List<string>();
            List<string> arrModelNames = new List<string>();

            foreach (var property in properties)
            {
                var subProperties = valueProvider.GetKeysFromPrefix(property.Value);

                var key = property.Value;
                var propName = property.Key;
   
                if (subProperties.Count == 0)
                {   if(!propName.Contains("RequestVerification"))
                    {
                        if (!model.ContainsKey(propName))
                            model.Add(propName, GetValue(valueProvider, key));
                        else
                            model[propName] = GetValue(valueProvider, key);
                    }
                }
                else if (subProperties.Any(sp => sp.Value.Contains("[")))
                {
                    if (!arrModelNames.Contains(propName))
                        arrModelNames.Add(propName);
                }
                else
                {
                    if (!subModelNames.Contains(propName))
                        subModelNames.Add(propName);
                }
            }

            foreach (var subModelName in subModelNames)
            {
                var key = properties[subModelName];

                JObject val = ParseProperties(key, valueProvider, model.ContainsKey(subModelName) ? (JObject)model[subModelName] : new JObject());
                if (!model.ContainsKey(subModelName))
                    model.Add(subModelName, val);
                else
                    model[subModelName] = val;
            }

            foreach (var arrModelName in arrModelNames)
            {
                var key = properties[arrModelName];
                var arrKeys = valueProvider.GetKeysFromPrefix(key);
                var isComplexArray = false;
                foreach (var arrKey in arrKeys)
                {
                    var subProperties = valueProvider.GetKeysFromPrefix(arrKey.Value);
                    if (subProperties.Count > 0)
                    {
                        isComplexArray = true;
                    }
                }

                JToken arrResult = null;

                List<object> vals = new List<object>();

                vals.Cast<Object>().ToList();
                if (isComplexArray)
                {
                    foreach (var arrKey in arrKeys)
                    {
                        var arrItemKey = arrKey.Value;

                        var subProperties = valueProvider.GetKeysFromPrefix(arrItemKey);
                        if (subProperties.Count > 0)
                        {
                            object val = ParseProperties(arrItemKey, valueProvider, new JObject());
                            vals.Add(val);
                        }
                    }

                    arrResult = new JArray(vals);
                }
                else
                {
                    foreach (var arrKey in arrKeys)
                    {
                        var arrItemKey = arrKey.Value;
                        vals.Add(GetValue(valueProvider, arrItemKey));
                    }

                    arrResult = new JArray(vals);

                    bool castToType = true;
                    Type itemType = vals[0].GetType();
                    foreach (var item in vals)
                    {
                        if (item.GetType() != itemType)
                        {
                            castToType = false;
                            break;
                        }
                    }

                    if (castToType)
                    {
                        var ienumerable = typeof(Enumerable).GetMethod("Cast").MakeGenericMethod(itemType).Invoke(null, new object[] { vals });
                        arrResult = new JArray(typeof(Enumerable).GetMethod("ToList").MakeGenericMethod(itemType).Invoke(null, new object[] { ienumerable }));
                    }
                }

                if (!model.ContainsKey(arrModelName))
                    model.Add(arrModelName, arrResult);
                else
                    model[arrModelName] = arrResult;
            }

            return model;
        }

        private JToken GetValue(IValueProvider valueProvider, string key)
        {
            var valueProviderResult = valueProvider.GetValue(key);

            return GetValue(valueProviderResult.Values);
        }

        private JToken GetValue(StringValues values)
        {
            var vals = new List<object>();
            foreach (var input in values)
            {
                object val = GetValue(input).Value;

                vals.Add(val);
            }

            if (vals.Count > 1)
            {
                object result = vals;

                bool castToType = true;
                Type itemType = vals[0].GetType();
                foreach (var item in vals)
                {
                    if (item.GetType() != itemType)
                    {
                        castToType = false;
                        break;
                    }
                }

                if (castToType)
                {
                    var ienumerable = typeof(Enumerable).GetMethod("Cast").MakeGenericMethod(itemType).Invoke(null, new object[] { vals });
                    result = typeof(Enumerable).GetMethod("ToList").MakeGenericMethod(itemType).Invoke(null, new object[] { ienumerable });
                }

                return new JArray(result);
            }
            else
            {
                return new JValue(vals.First());
            }
        }

        private JValue GetValue(string input)
        {
            object val = input;

            if (!String.IsNullOrWhiteSpace(input) && DateTime.TryParse(input, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out DateTime datetime))
            {
                val = datetime;
            }
            else if (decimal.TryParse(input, out decimal decimalVal))
            {
                val = decimalVal;
            }
            else if (long.TryParse(input, out long longVal))
            {
                val = longVal;
            }
            else if (int.TryParse(input, out int intVal))
            {
                val = intVal;
            }
            else if (byte.TryParse(input, out byte byteVal))
            {
                val = byteVal;
            }
            else if (bool.TryParse(input, out bool boolVal))
            {
                val = boolVal;
            }

            return new JValue(val);
        }
    }

    public class DynamicModelBinderProvider : IModelBinderProvider
    {
        public IModelBinder GetBinder(ModelBinderProviderContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            if ((context.Metadata.ModelType == typeof(object) || context.Metadata.ModelType == typeof(JObject)) && !context.Metadata.IsCollectionType)
            {
                var loggerFactory = context.Services.GetRequiredService<ILoggerFactory>();
                var options = context.Services.GetRequiredService<IOptions<MvcOptions>>();
                return new DynamicModelBinder(options, loggerFactory);
            }

            return null;
        }
    }

    public static class DynamicModelBinderExtensions
    {
        public static IServiceCollection AddDynamicModelBinder(this IServiceCollection services)
        {
            services.AddSingleton<IConfigureOptions<MvcOptions>, DynamicModelBinderMvcOptionsSetup>();
            return services;
        }
    }
    public class DynamicModelBinderMvcOptionsSetup : IConfigureOptions<MvcOptions>
    {
        private readonly ILoggerFactory _loggerFactory;

        public DynamicModelBinderMvcOptionsSetup(ILoggerFactory loggerFactory)
        {
            _loggerFactory = loggerFactory;
        }

        public void Configure(MvcOptions options)
        {
            var binderToFind = options.ModelBinderProviders.FirstOrDefault(x => x.GetType() == typeof(ComplexTypeModelBinderProvider));

            if (binderToFind == null) return;

            var index = options.ModelBinderProviders.IndexOf(binderToFind);
            options.ModelBinderProviders.Insert(index, new DynamicModelBinderProvider());
        }
    }
}
