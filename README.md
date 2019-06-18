# ASP.NET Core MVC to Api

* By default ASP.NET Core doesn't allow a single controller action to handle request/response for both Mvc and Api requests or allow an Api request to bind to Body + Route/Query. This library allows you to do so. 
* The [ApiErrorFilterAttribute] is gives similar functionality to the [ClientErrorResultFilter](https://github.com/aspnet/AspNetCore/blob/c565386a3ed135560bc2e9017aa54a950b4e35dd/src/Mvc/Mvc.Core/src/Infrastructure/ClientErrorResultFilter.cs) that is applied when a controller is decorated with [ApiController](https://docs.microsoft.com/en-us/aspnet/core/web-api/?view=aspnetcore-2.2#multipartform-data-request-inference) but it only handles request which don't have Accept header = text/html. 
* The [ApiExceptionFilterAttribute] is allows api exceptions to be handled. It only handles request which don't have Accept header = text/html.

I think it could be most useful for the following scenarios:
1. Allowing Developers to Test/Develop/Debug Mvc Forms without worrying about UI. Used by applying conventions.
2. Integration Tests for Mvc without the need of [WebApplicationFactory](https://docs.microsoft.com/en-us/aspnet/core/test/integration-tests?view=aspnetcore-2.2). Used by applying convention.
3. Used in Production to allow specific Mvc controller actions to return model as json/xml data.
4. Used in Production to allow specific Api controller actions to bind to Body + Query/Route.
5. Used in Production to allow Mvc controller actions to return error responses/exceptions as Problem Details.

Currently to create a controller which handles Api and Mvc requests you would need to write something along the lines of below.

```
[Route("contact")]
[HttpGet]
public IActionResult ContactMvc()
{
    return View(new ContactViewModel());
}

[ValidateAntiForgeryToken]
[Route("contact")]
[HttpPost]
public IActionResult ContactMvc(ContactViewModel viewModel)
{
    if(ModelState.IsValid)
    {
        //Submit Contact Form

        return RedirectToAction("Home");
    }

    return View(viewModel);
}

[ApiExceptionFilter]
[Route("api/contact")]
[HttpGet]
public ActionResult<ContactViewModel> ContactApi()
{
    return new ContactViewModel();
}

[ApiExceptionFilter]
[Route("api/contact")]
[HttpPost]
public IActionResult ContactApi(ContactViewModel viewModel)
{
    if (ModelState.IsValid)
    {
        //Submit Contact Form
        return Ok();
    }
            
    return ValidationProblem(ModelState);
}
```

* This library give thes ability to add attributes/conventions which allow an Mvc controller action to return and accept data as if it were an Api action method. An example of the attributes required can be seen below.

```
[ApiExceptionFilter]
[ApiErrorFilter]
[ViewResultToObjectResult]
[Route("contact")]
[HttpGet]
public IActionResult ContactMvc()
{
    return View(new ContactViewModel());
}

[ApiExceptionFilter]
[ApiErrorFilter]
[AutoValidateFormAntiForgeryToken]
[Route("contact")]
[HttpPost]
public IActionResult ContactMvc([FromBodyAndModelBinding] ContactViewModel viewModel)
{
    if(ModelState.IsValid)
    {
        //Submit Contact Form

        return RedirectToAction("Home");
    }

    return View(viewModel);
}
```
* There are four conventions which add required binding attributes, handle Api Error Responses/Exceptions and switch [ValidateAntiForgeryToken] > [AutoValidateFormAntiForgeryToken]. This ensures AntiForgeryToken still occurs for Mvc but is bypassed for Api requests.
* The MvcAsApiConvention adds all four conventions in one line of code.

```
 services.AddMvc(options =>
{
	if(HostingEnvironment.IsDevelopment())
	{
		options.Conventions.Add(new MvcAsApiConvention());
	
		//Return problem details in json/xml if an error response is returned via Api.
		//options.Conventions.Add(new ApiErrorFilterConvention());
		//Return problem details in json/xml if an exception is thrown via Api
		//options.Conventions.Add(new ApiExceptionFilterConvention());
	    //Post data to MVC Controller from API
		//options.Conventions.Add(new FromBodyAndOtherSourcesConvention(true, true, true));
		//Return data uisng output formatter when acccept header is application/json or application/xml
		//options.Conventions.Add(new ConvertViewResultToObjectResultConvention());
	}
});

//Optional
if(HostingEnvironment.IsDevelopment())
{
	//Overrides the default IClientErrorFactory implementation which adds traceId, timeGenerated and exception details to the ProblemDetails response.
	services.AddEnhancedProblemDetailsClientErrorFactory(true);

	services.Configure<ApiBehaviorOptions>(options =>
	{
		//Overrides the default InvalidModelStateResponseFactory, adds traceId and timeGenerated to the ProblemDetails response. 
		options.EnableEnhancedValidationProblemDetails();
	});
}

[Route("contact")]
[HttpGet]
public IActionResult ContactMvc()
{
    return View(new ContactViewModel());
}

[ValidateAntiForgeryToken]
[Route("contact")]
[HttpPost]
public IActionResult ContactMvc(ContactViewModel viewModel)
{
    if(ModelState.IsValid)
    {
        //Submit Contact Form

        return RedirectToAction("Home");
    }

    return View(viewModel);
}
```

* By default only [JsonInputFormatter](https://github.com/aspnet/Mvc/blob/master/src/Microsoft.AspNetCore.Mvc.Formatters.Json/JsonInputFormatter.cs) binds dynamic as JObject. [ComplexTypeModelBinderProvider](https://github.com/aspnet/AspNetCore/blob/c565386a3ed135560bc2e9017aa54a950b4e35dd/src/Mvc/Mvc.Core/src/ModelBinding/Binders/ComplexTypeModelBinder.cs) doesn't bind to dynamic so I have created an additional optional ModelBinder which allows the same functionality for Mvc.
* https://github.com/aspnet/AspNetCore/issues/1748
* https://stackoverflow.com/questions/9450619/using-dynamic-objects-with-asp-net-mvc-model-binding

```
 services.AddMvc(options =>
{
	if(HostingEnvironment.IsDevelopment())
	{
		options.Conventions.Add(new MvcAsApiConvention());
	
		//Return problem details in json/xml if an error response is returned via Api.
		//options.Conventions.Add(new ApiErrorFilterConvention());
		//Return problem details in json/xml if an exception is thrown via Api
		//options.Conventions.Add(new ApiExceptionFilterConvention());
		//Post data to MVC Controller from API
		//options.Conventions.Add(new FromBodyAndOtherSourcesConvention(true, true, true));
		//Return data uisng output formatter when acccept header is application/json or application/xml
		//options.Conventions.Add(new ConvertViewResultToObjectResultConvention());
	}
})
//Optional
.AddDynamicModelBinder();

//Optional
if(HostingEnvironment.IsDevelopment())
{
	//Overrides the default IClientErrorFactory implementation which adds traceId, timeGenerated and exception details to the ProblemDetails response.
	services.AddEnhancedProblemDetailsClientErrorFactory(true);

	services.Configure<ApiBehaviorOptions>(options =>
	{
		//Overrides the default InvalidModelStateResponseFactory, adds traceId and timeGenerated to the ProblemDetails response. 
		options.EnableEnhancedValidationProblemDetails();
	});
}

[Route("contact")]
[HttpGet]
public IActionResult ContactMvc()
{
    return View(new ContactViewModel());
}

[ValidateAntiForgeryToken]
[Route("contact")]
[HttpPost]
public IActionResult ContactMvc(dynamic viewModel)
{
    //Submit Contact Form

    return RedirectToAction("Home");
}
```

## Error Responses (Status Code >= 400) and Exceptions - MVC Filters
* Api Controller Action error responses (Status Code >= 400) and Exceptions will be handled with these filters. 
* For handling 404 and exceptions from other middleware you will need to implement Global Exception Handling. See below.
* IClientErrorFactory will handle generating the problem details when an Error Response occurs. See default [ProblemDetailsClientErrorFactory](https://github.com/aspnet/AspNetCore/blob/c565386a3ed135560bc2e9017aa54a950b4e35dd/src/Mvc/Mvc.Core/src/Infrastructure/ProblemDetailsClientErrorFactory.cs).
* An enhanced IClientErrorFactory can be used as this adds traceId, timeGenerated and also handles generating the problem details when an exception is thrown. 
* Use [ConfigureApiBehaviorOptions to configure problem detail type and title mapping](https://docs.microsoft.com/en-us/aspnet/core/web-api/?view=aspnetcore-2.2).

```
services.AddEnhancedProblemDetailsClientErrorFactory(true);
```

| Attribute                     | Description                                                                                 |
|-:-----------------------------|-:-------------------------------------------------------------------------------------------|
| [ApiErrorFilterAttribute]     | Return problem details in json/xml if an error response is returned from Controller Action. |
| [ApiExceptionFilterAttribute] | Return problem details in json/xml if an exception is thrown from Controller Action.        |

* Example Error Response
```
{
    "type": "about:blank",
    "title": "",
    "status": 450,
    "instance": "/new/contact",
    "traceId": "0HLNK5EJKEF4K:00000001",
    "timeGenerated": "2019-06-18T20:33:46.6609813Z"
}
```

* Example Exception Response
```
{
    "type": "about:blank",
    "title": "An error has occured.",
    "status": 500,
    "detail": "System.Exception: Test\r\n   at DynamicForms.Web.Controllers.ApiFormController.Contact() in C:\\Development\\DynamicForms\\src\\DynamicForms.Web\\Controllers\\ApiFormController.cs:line 85\r\n   at lambda_method(Closure , Object , Object[] )\r\n   at Microsoft.Extensions.Internal.ObjectMethodExecutor.Execute(Object target, Object[] parameters)\r\n   at Microsoft.AspNetCore.Mvc.Internal.ActionMethodExecutor.SyncActionResultExecutor.Execute(IActionResultTypeMapper mapper, ObjectMethodExecutor executor, Object controller, Object[] arguments)\r\n   at Microsoft.AspNetCore.Mvc.Internal.ControllerActionInvoker.InvokeActionMethodAsync()\r\n   at Microsoft.AspNetCore.Mvc.Internal.ControllerActionInvoker.InvokeNextActionFilterAsync()\r\n   at Microsoft.AspNetCore.Mvc.Internal.ControllerActionInvoker.Rethrow(ActionExecutedContext context)\r\n   at Microsoft.AspNetCore.Mvc.Internal.ControllerActionInvoker.Next(State& next, Scope& scope, Object& state, Boolean& isCompleted)\r\n   at Microsoft.AspNetCore.Mvc.Internal.ControllerActionInvoker.InvokeInnerFilterAsync()\r\n   at Microsoft.AspNetCore.Mvc.Internal.ResourceInvoker.InvokeNextExceptionFilterAsync()",
    "instance": "/new/contact",
    "traceId": "0HLNK5EJKEF4H:00000002",
    "timeGenerated": "2019-06-18T20:31:36.9343306Z"
}
```

## Global Error Responses (Status Code >= 400) and Exceptions - Middleware
* Catches [404 error responses when route is not found and exceptions from MVC and other middleware](https://github.com/aspnet/AspNetCore/issues/4953).
* Usually would use MVC Filters OR Global Error/Exception handling but not both. It will work with both though.
* Using [WebAPIContrib.Core](https://github.com/WebApiContrib/WebAPIContrib.Core) to allow the use of action results outside of MVC.
* Use [ConfigureApiBehaviorOptions to configure problem detail type and title mapping](https://docs.microsoft.com/en-us/aspnet/core/web-api/?view=aspnetcore-2.2).

```
if (!env.IsProduction())
{
	// Non Api
	app.UseWhen(context => context.Request.IsMvc(),
		appBranch =>
		{
			appBranch.UseDeveloperExceptionPage();
		}
   );

	// Web Api
	 app.UseWhen(context => context.Request.IsApi(),
		appBranch =>
		{
			appBranch.UseWebApiExceptionHandlerProblemDetails(true);
			//The global error handler has logic inbuilt so if an error has been handled by MVC Filters it won't try and reprocess. 
			appBranch.UseWebApiErrorHandlerProblemDetails();
		}
   );

	app.UseDatabaseErrorPage();
}
else
{
	// Non Api
	app.UseWhen(context => context.Request.IsMvc(),
		 appBranch =>
		 {
			 appBranch.UseExceptionHandler("/Error");
		 }
	);

	// Web Api
	 app.UseWhen(context => context.Request.IsApi(),
			appBranch =>
			{
				appBranch.UseWebApiExceptionHandlerProblemDetails(false);
				//The global error handler has logic inbuilt so if an error has been handled by MVC Filters it won't try and reprocess. 
				appBranch.UseWebApiErrorHandlerProblemDetails();
			}
	   );
   
   app.UseHsts();
}
```

* Example 404 Response
```
{
    "type": "https://tools.ietf.org/html/rfc7231#section-6.5.4",
    "title": "Not Found",
    "status": 404,
    "instance": "/new/test",
    "traceId": "0HLNK4OD71ENJ:00000004",
    "timeGenerated": "2019-06-18T20:04:50.3110301Z"
}
```

* Example Exception Response
```
{
    "type": "about:blank",
    "title": "An error has occured.",
    "status": 500,
    "detail": "System.Exception: Test\r\n   at DynamicForms.Web.Controllers.ApiFormController.Contact() in C:\\Development\\DynamicForms\\src\\DynamicForms.Web\\Controllers\\ApiFormController.cs:line 85\r\n   at lambda_method(Closure , Object , Object[] )\r\n   at Microsoft.Extensions.Internal.ObjectMethodExecutor.Execute(Object target, Object[] parameters)\r\n   at Microsoft.AspNetCore.Mvc.Internal.ActionMethodExecutor.SyncActionResultExecutor.Execute(IActionResultTypeMapper mapper, ObjectMethodExecutor executor, Object controller, Object[] arguments)\r\n   at Microsoft.AspNetCore.Mvc.Internal.ControllerActionInvoker.InvokeActionMethodAsync()\r\n   at Microsoft.AspNetCore.Mvc.Internal.ControllerActionInvoker.InvokeNextActionFilterAsync()\r\n   at Microsoft.AspNetCore.Mvc.Internal.ControllerActionInvoker.Rethrow(ActionExecutedContext context)\r\n   at Microsoft.AspNetCore.Mvc.Internal.ControllerActionInvoker.Next(State& next, Scope& scope, Object& state, Boolean& isCompleted)\r\n   at Microsoft.AspNetCore.Mvc.Internal.ControllerActionInvoker.InvokeInnerFilterAsync()\r\n   at Microsoft.AspNetCore.Mvc.Internal.ResourceInvoker.InvokeNextResourceFilter()\r\n   at Microsoft.AspNetCore.Mvc.Internal.ResourceInvoker.Rethrow(ResourceExecutedContext context)\r\n   at Microsoft.AspNetCore.Mvc.Internal.ResourceInvoker.Next(State& next, Scope& scope, Object& state, Boolean& isCompleted)\r\n   at Microsoft.AspNetCore.Mvc.Internal.ResourceInvoker.InvokeFilterPipelineAsync()\r\n   at Microsoft.AspNetCore.Mvc.Internal.ResourceInvoker.InvokeAsync()\r\n   at Microsoft.AspNetCore.Routing.EndpointMiddleware.Invoke(HttpContext httpContext)\r\n   at Microsoft.AspNetCore.Routing.EndpointRoutingMiddleware.Invoke(HttpContext httpContext)\r\n   at Microsoft.AspNetCore.StaticFiles.StaticFileMiddleware.Invoke(HttpContext context)\r\n   at Microsoft.AspNetCore.Diagnostics.EntityFrameworkCore.MigrationsEndPointMiddleware.Invoke(HttpContext context)\r\n   at Microsoft.AspNetCore.Diagnostics.EntityFrameworkCore.DatabaseErrorPageMiddleware.Invoke(HttpContext httpContext)\r\n   at Microsoft.AspNetCore.Diagnostics.EntityFrameworkCore.DatabaseErrorPageMiddleware.Invoke(HttpContext httpContext)\r\n   at AspNetCore.Mvc.HybridModelBindingAndViewToObjectResult.Middleware.ApiGlobalErrorResponseProblemDetailsMiddleware.InvokeAsync(HttpContext context) in C:\\Development\\HybridModelBindingAndViewToObjectResult\\src\\AspNetCore.Mvc.HybridModelBindingAndViewToObjectResult\\Middleware\\ApiGlobalErrorResponseProblemDetailsMiddleware.cs:line 34\r\n   at Microsoft.AspNetCore.Diagnostics.ExceptionHandlerMiddleware.Invoke(HttpContext context)",
    "instance": "/new/contact",
    "traceId": "0HLNK55N49JII:00000002",
    "timeGenerated": "2019-06-18T20:13:39.6308515Z"
}
```

## Authorization 

| Attribute                                   | Description                                                                        |
|:--------------------------------------------|:-----------------------------------------------------------------------------------|
| [AutoValidateFormAntiforgeryTokenAttribute] | Ensures only Post requests with Form content-type is checked for AntiForgeryToken. |


## Model Binding Attributes
* https://docs.microsoft.com/en-us/aspnet/core/mvc/models/model-binding?view=aspnetcore-2.2
* https://andrewlock.net/model-binding-json-posts-in-asp-net-core/
* https://stackoverflow.com/questions/45495432/asp-net-core-mvc-mixed-route-frombody-model-binding-validation
* https://github.com/billbogaiv/hybrid-model-binding

| Attribute                                                                  | Description                                                                                   |
|:---------------------------------------------------------------------------|:----------------------------------------------------------------------------------------------|
| [FromBodyOrFormAttribute]                                                  | Binds Model to Body or Form                                                                   |
| [FromBodyOrQueryAttribute]                                                 | Binds Model to Body or Query                                                                  |
| [FromBodyOrRouteAttribute]                                                 | Binds Model to Body or Route                                                                  |
| [FromBodyOrFormRouteQueryAttribute] or [FromBodyOrModelBindingAttribute]   | Binds Model to Body or Form/Route/Query                                                       |
| [FromBodyAndQueryAttribute]                                                | Binds Model to Body and Query                                                                 |
| [FromBodyAndRouteAttribute]                                                | Binds Model to Body and Route                                                                 |
| [FromBodyFormAndRouteQueryAttribute] or [FromBodyAndModelBindingAttribute] | Binds Model to Body/Form and Route/Query                                                      |
| [FromBodyExplicitAttribute]                                                | If conventions are used to change [FromBody] attributes this can be used to prevent doing so. |

## Model State Errors
* I recommend using options.EnableEnhancedValidationProblemDetails() as this adds traceId and timeGenerated to the Invalid Model State Problem Details.
* Pass true to add angular formatted errors to the Invalid Model State Problem Details also.

```
services.Configure<ApiBehaviorOptions>(options =>
{
	options.EnableEnhancedValidationProblemDetails(true);
});
```

*Example ModelState error response
```
{
    "errors": {
        "Name": [
            "The Name field is required."
        ]
    },
    "type": "https://tools.ietf.org/html/rfc7231#section-6.5.1",
    "title": "One or more validation errors occurred.",
    "status": 400,
    "detail": "Please refer to the errors property for additional details.",
    "instance": "/home/contact",
    "traceId": "8000003d-0006-fd00-b63f-84710c7967bb",
    "timeGenerated": "2019-06-18T21:18:03.7548395Z"
}
```

## Output Formatting Attributes

| Attribute                                  | Description                                                                                          |
|:-------------------------------------------|:-----------------------------------------------------------------------------------------------------|
| [ConvertViewResultToObjectResultAttribute] | Converts ViewResult to ObjectResult when Accept header matches output formatter SupportedMediaTypes. |


## Conventions

| Convention                                | Description                                                                                                                                                                                                                      |
|:------------------------------------------|:---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|
| ApiErrorFilterConvention                  | Adds ApiErrorFilterAttribute to all Controller Actions.                                                                                                                                                                          |
| ApiErrorExceptionFilterConvention         | Adds ApiExceptionFilterAttribute to all Controller Actions.                                                                                                                                                                      |
| FromBodyAndOtherSourcesConvention         | Adds required attributes to all Controllers, Actions and Parameters. Good for Development environment. In production only recommending passing true for first argument which applys convention to params with no binding source. |
| FromBodyOrOtherSourcesConvention          | Adds required attributes to all Controllers, Actions and Parameters. Good for Development environment. In production only recommending passing true for first argument which applys convention to params with no binding source. |
| ConvertViewResultToObjectResultConvention | Adds ConvertViewResultToObjectResultAttribute to all Controller Actions.                                                                                                                                                         |
| MvcAsApiConvention                        | Adds ApiErrorFilterConvention, ApiErrorExceptionFilterConvention, FromBodyOrOtherSourcesConvention and ConvertViewResultToObjectResultConvention to all Controller Actions.                                                      |
                                                                          

## Api Response
* If Accept Header matches OutputFormatter Supported Media Type and the ModelState is Valid, ViewResult is Converted to ObjectResult.
* If Accept Header matches OutputFormatter Supported Media Type and the ModelState is Valid, ApiBehaviorOptions InvalidModelStateResponseFactory delegate is called which by default returns [ValidationProblemDetails](https://github.com/aspnet/AspNetCore/blob/c565386a3ed135560bc2e9017aa54a950b4e35dd/src/Mvc/Mvc.Core/src/DependencyInjection/ApiBehaviorOptionsSetup.cs). [See web API Documentation](https://docs.microsoft.com/en-us/aspnet/core/web-api/?view=aspnetcore-2.2)
* If an error response or exception is thrown ProblemDetails are returned.


## Authors

* **Dave Ikin** - [davidikin45](https://github.com/davidikin45)


## License

This project is licensed under the MIT License