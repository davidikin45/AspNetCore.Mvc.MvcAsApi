using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace AspNetCore.Mvc.MvcAsApi.BindingSources
{
    public class BodyExplicitBindingSource : BindingSource
    {
        public static new readonly BindingSource Body = new BodyExplicitBindingSource(
            "Body",
            "Body",
            true,
            true
            );

        public BodyExplicitBindingSource(string id, string displayName, bool isGreedy, bool isFromRequest) : base(id, displayName, isGreedy, isFromRequest)
        {
        }

        public override bool CanAcceptDataFrom(BindingSource bindingSource)
        {
            if (this == bindingSource)
            {
                return true;
            }

            if (this == Body)
            {
                return bindingSource == BindingSource.Body;
            }

            return false;
        }
    }
}