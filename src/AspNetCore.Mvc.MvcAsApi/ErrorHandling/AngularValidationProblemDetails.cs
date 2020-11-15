using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;

namespace AspNetCore.Mvc.MvcAsApi.ErrorHandling
{
    public class AngularValidationProblemDetails : ValidationProblemDetails
    {
        public AngularValidationProblemDetails()
            : base()
        {

        }

        public AngularValidationProblemDetails(ModelStateDictionary modelState)
            : base(modelState)
        {

        }

        public AngularValidationProblemDetails(IDictionary<string, string[]> errors)
            : base(errors)
        {

        }


#if NETCOREAPP3_0
        [System.Text.Json.Serialization.JsonPropertyName("angularErrors")]
#endif
        [JsonProperty(PropertyName = "angularErrors")]
        public SerializableDictionary<string, AngularFormattedValidationError[]> AngularErrors
        {
            get
            {
                var dict = new SerializableDictionary<string, AngularFormattedValidationError[]>(StringComparer.Ordinal);

                foreach (var kvp in Errors)
                {
                    var angularErrorMessages = new List<AngularFormattedValidationError>();
                    foreach (var errorMessage in kvp.Value)
                    {
                        var keyAndMessage = errorMessage.Split('|');
                        if (keyAndMessage.Count() > 1)
                        {
                            //Formatted for Angular Binding
                            //e.g required|Error Message
                            angularErrorMessages.Add(new AngularFormattedValidationError(
                                keyAndMessage[1],
                                keyAndMessage[0]));
                        }
                        else
                        {
                            angularErrorMessages.Add(new AngularFormattedValidationError(
                                keyAndMessage[0]));
                        }
                    }

                    dict.Add(kvp.Key, angularErrorMessages.ToArray());
                }

                return dict;
            }
        }
    }
}
