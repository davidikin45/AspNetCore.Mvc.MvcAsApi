namespace AspNetCore.Mvc.MvcAsApi.ErrorHandling
{
    public class AngularFormattedValidationError
    {
        public string ValidatorKey { get; private set; }
        public string Message { get; private set; }

        public AngularFormattedValidationError(string message, string validatorKey = "")
        {
            ValidatorKey = validatorKey;
            Message = message;
        }
    }
}
