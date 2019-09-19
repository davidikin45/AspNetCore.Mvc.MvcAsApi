using AspNetCore.Mvc.MvcAsApi.Extensions;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Http;
using System;
using System.Threading.Tasks;

namespace AspNetCore.Mvc.MvcAsApi.Middleware
{
    //Cookie-based authentication
    //When a user authenticates using their username and password, they're issued a token, containing an authentication ticket that can be used for authentication and authorization. The token is stored as a cookie that accompanies every request the client makes. Generating and validating this cookie is performed by the Cookie Authentication Middleware. The middleware serializes a user principal into an encrypted cookie. On subsequent requests, the middleware validates the cookie, recreates the principal, and assigns the principal to the User property of HttpContext.

    //Token-based authentication
    //When a user is authenticated, they're issued a token (not an antiforgery token). The token contains user information in the form of claims or a reference token that points the app to user state maintained in the app. When a user attempts to access a resource requiring authentication, the token is sent to the app with an additional authorization header in form of Bearer token. This makes the app stateless. In each subsequent request, the token is passed in the request for server-side validation. This token isn't encrypted; it's encoded. On the server, the token is decoded to access its information. To send the token on subsequent requests, store the token in the browser's local storage.Don't be concerned about CSRF vulnerability if the token is stored in the browser's local storage.CSRF is a concern when the token is stored in a cookie.

    //CSRF attacks are possible against web apps that use cookies for authentication because:
    //Browsers store cookies issued by a web app.
    //Stored cookies include session cookies for authenticated users.
    //Browsers send all of the cookies associated with a domain to the web app every request regardless of how the request to app was generated within the browser.

    //In traditional HTML-based apps, antiforgery tokens are passed to the server using hidden form fields.In modern JavaScript-based apps and SPAs, many requests are made programmatically.These AJAX requests may use other techniques (such as request headers or cookies) to send the token.
    //If cookies are used to store authentication tokens and to authenticate API requests on the server, CSRF is a potential problem.If local storage is used to store the token, CSRF vulnerability might be mitigated because values from local storage aren't sent automatically to the server with every request. Thus, using local storage to store the antiforgery token on the client and sending the token as a request header is a recommended approach.

    //APIS are vulnerable to CSRF attack as long as the server uses authenticated session(cookies).
    //The solution is
    //1.Ensure that the 'safe' HTTP operations, such as GET, HEAD, OPTIONS, TRACE cannot be used to alter any server-side state.
    //2.Ensure that any 'unsafe' HTTP operations, such as POST, PUT, PATCH and DELETE, always require a valid CSRF token!
    public class ApiGenerateAntiForgeryTokenMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly IAntiforgery _antiforgery;
        private readonly ApiGenerateAntiForgeryTokenOptions _options;

        public ApiGenerateAntiForgeryTokenMiddleware(RequestDelegate next, IAntiforgery antiforgery, ApiGenerateAntiForgeryTokenOptions options)
        {
            _next = next;
            _antiforgery = antiforgery;
            _options = options;
        }

        public Task InvokeAsync(HttpContext context)
        {
            bool addAntiforgeryTokens = false;
            var method = context.Request.Method;
            if (string.Equals("GET", method, StringComparison.OrdinalIgnoreCase) ||
                string.Equals("HEAD", method, StringComparison.OrdinalIgnoreCase) ||
                string.Equals("TRACE", method, StringComparison.OrdinalIgnoreCase) ||
                string.Equals("OPTIONS", method, StringComparison.OrdinalIgnoreCase))
            {
                addAntiforgeryTokens = true;
            }

            if (addAntiforgeryTokens && context.Request.IsApi())
            {
                // The request token can be sent as a JavaScript-readable cookie, 
                // and Angular uses it by default.
                var tokens = _antiforgery.GetAndStoreTokens(context);
                context.Response.Cookies.Append(_options.TokenName, tokens.RequestToken,
                    new CookieOptions() { HttpOnly = false, IsEssential = true });
            }

            // Call the next delegate/middleware in the pipeline
            return _next(context);
        }
    }

    public class ApiGenerateAntiForgeryTokenOptions
    {
        public string TokenName { get; set; } = "XSRF-TOKEN";
    }
}
