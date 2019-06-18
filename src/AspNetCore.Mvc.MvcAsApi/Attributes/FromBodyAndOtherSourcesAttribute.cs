using AspNetCore.Mvc.MvcAsApi.BindingSources;
using AspNetCore.Mvc.MvcAsApi.ModelBinding;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using System;

namespace AspNetCore.Mvc.MvcAsApi.Attributes
{
    public class FromBodyAndQueryAttribute : FromBodyAndAttribute
    {
        public FromBodyAndQueryAttribute()
               : base(BodyAndBindingSource.BodyAndQuery)
        {

        }
    }

    public class FromBodyAndRouteAttribute : FromBodyAndAttribute
    {
        public FromBodyAndRouteAttribute()
               : base(BodyAndBindingSource.BodyAndPath)
        {

        }
    }

    public class FromBodyFormAndRouteQueryAttribute : FromBodyAndAttribute
    {
     public FromBodyFormAndRouteQueryAttribute()
            :base(BodyAndBindingSource.BodyAndModelBinding)
        {

        }
    }

    public class FromBodyAndModelBindingAttribute : FromBodyAndAttribute
    {
        public FromBodyAndModelBindingAttribute()
               : base(BodyAndBindingSource.BodyAndModelBinding)
        {

        }
    }

    [AttributeUsage(AttributeTargets.Parameter | AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
    public class FromBodyAndAttribute : ModelBinderAttribute, IBindingSourceMetadata
    {
        public FromBodyAndAttribute(BindingSource bindingSource = null) :base(typeof(BodyAndOtherSourcesModelBinder))
        {
            //ModelBinderAttribute will return ModelBinder.Custom if BindingSource is null.
            BindingSource = bindingSource ?? BodyAndBindingSource.BodyAndModelBinding;
        }

    }
}