using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace AspNetCore.Mvc.MvcAsApi.BindingSources
{
    public class BodyAndBindingSource : BindingSource
    {
        public static readonly BindingSource BodyAndPath = new BodyAndBindingSource(
            "BodyAndPath",
            "BodyAndPath",
            false,
            true
            );


        public static readonly BindingSource BodyAndQuery = new BodyAndBindingSource(
            "BodyAndQuery",
            "BodyAndQuery",
            false,
            true
            );

        public static readonly BindingSource BodyAndModelBinding = new BodyAndBindingSource(
           "BodyAndModelBinding",
           "BodyAndModelBinding",
           false,
           true
           );

        public BodyAndBindingSource(string id, string displayName, bool isGreedy, bool isFromRequest) : base(id, displayName, isGreedy, isFromRequest)
        {
        }

        public override bool CanAcceptDataFrom(BindingSource bindingSource)
        {
            if (this == bindingSource)
            {
                return true;
            }

            if (this == BodyAndPath)
            {
                return bindingSource == BindingSource.Path;
            }

            if (this == BodyAndQuery)
            {
                return bindingSource == BindingSource.Query;
            }

            if (this == BodyAndModelBinding)
            {
                return bindingSource == BindingSource.Form || bindingSource == BindingSource.Path || bindingSource == BindingSource.Query || bindingSource == BindingSource.ModelBinding;
            }

            return false;
        }
    }
}