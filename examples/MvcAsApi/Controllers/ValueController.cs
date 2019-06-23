using Microsoft.AspNetCore.Mvc;
using MvcAsApi.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MvcAsApi.Controllers
{
    [ApiController]
    [Route("api/values")]
    public class ValueController : ControllerBase
    {

        [Route("")]
        [HttpGet]
        public IActionResult ErrorResponse()
        {
            throw new Exception();
        }
    }
}
