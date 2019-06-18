using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace AspNetCore.Mvc.MvcAsApi.BindingSources
{
    public class BodyOrBindingSource : BindingSource
    {
        public static readonly BindingSource BodyOrForm = new BodyOrBindingSource(
            "BodyOrForm",
            "BodyOrForm",
            false,
            true
            );

        public static readonly BindingSource BodyOrPath = new BodyOrBindingSource(
            "BodyOrPath",
            "BodyOrPath",
            false,
            true
            );

        public static readonly BindingSource BodyOrQuery = new BodyOrBindingSource(
            "BodyOrQuery",
            "BodyOrQuery",
            false,
            true
            );

        public static readonly BindingSource BodyOrModelBinding = new BodyOrBindingSource(
           "BodyOrModelBinding",
           "BodyOrModelBinding",
           false,
           true
           );

        public BodyOrBindingSource(string id, string displayName, bool isGreedy, bool isFromRequest) : base(id, displayName, isGreedy, isFromRequest)
        {
        }

        public override bool CanAcceptDataFrom(BindingSource bindingSource)
        {
            if (this == bindingSource)
            {
                return true;
            }

            if (this == BodyOrForm)
            {
                return bindingSource == BindingSource.Form;
            }

            if (this == BodyOrPath)
            {
                return bindingSource == BindingSource.Path;
            }

            if (this == BodyOrQuery)
            {
                return bindingSource == BindingSource.Query;
            }

            if (this == BodyOrModelBinding)
            {
                return bindingSource == BindingSource.Form || bindingSource == BindingSource.Path || bindingSource == BindingSource.Query || bindingSource == BindingSource.ModelBinding;
            }

            return false;
        }
    }
}