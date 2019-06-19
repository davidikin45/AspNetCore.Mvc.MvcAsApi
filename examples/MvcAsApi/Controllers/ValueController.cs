using Microsoft.AspNetCore.Mvc;
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

        [Route("ErrorResponse")]
        [HttpGet]
        public IActionResult ErrorResponse()
        {
            return BadRequest();
        }
    }
}
