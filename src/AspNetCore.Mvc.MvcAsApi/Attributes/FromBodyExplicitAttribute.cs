using AspNetCore.Mvc.MvcAsApi.BindingSources;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using System;

namespace AspNetCore.Mvc.MvcAsApi.Attributes
{
    [AttributeUsage(AttributeTargets.Parameter | AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
    public class FromBodyExplicitAttribute : Attribute, IBindingSourceMetadata
    {
        public BindingSource BindingSource => BodyExplicitBindingSource.Body;
    }
}
