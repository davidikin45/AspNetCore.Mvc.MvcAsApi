using AspNetCore.Mvc.MvcAsApi.BindingSources;
using AspNetCore.Mvc.MvcAsApi.ModelBinding;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using System;

namespace AspNetCore.Mvc.MvcAsApi.Attributes
{
    public class FromBodyOrFormAttribute : FromBodyOrAttribute
    {
        public FromBodyOrFormAttribute()
               : base(BodyOrBindingSource.BodyOrForm)
        {

        }
    }

    public class FromBodyOrRouteAttribute : FromBodyOrAttribute
    {
        public FromBodyOrRouteAttribute()
               : base(BodyOrBindingSource.BodyOrPath)
        {

        }
    }

    public class FromBodyOrQueryAttribute : FromBodyOrAttribute
    {
        public FromBodyOrQueryAttribute()
               : base(BodyOrBindingSource.BodyOrQuery)
        {

        }
    }

    public class FromBodyOrFormRouteQueryAttribute : FromBodyOrAttribute
    {
     public FromBodyOrFormRouteQueryAttribute()
            :base(BodyOrBindingSource.BodyOrModelBinding)
        {

        }
    }

    public class FromBodyOrModelBindingAttribute : FromBodyOrAttribute
    {
        public FromBodyOrModelBindingAttribute()
               : base(BodyOrBindingSource.BodyOrModelBinding)
        {

        }
    }

    [AttributeUsage(AttributeTargets.Parameter | AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
    public class FromBodyOrAttribute : ModelBinderAttribute, IBindingSourceMetadata
    {
        public FromBodyOrAttribute(BindingSource bindingSource = null) :base(typeof(BodyOrOtherSourcesModelBinder))
        {
            //ModelBinderAttribute will return ModelBinder.Custom if BindingSource is null.
            BindingSource = bindingSource ?? BodyOrBindingSource.BodyOrModelBinding;
        }

    }
}