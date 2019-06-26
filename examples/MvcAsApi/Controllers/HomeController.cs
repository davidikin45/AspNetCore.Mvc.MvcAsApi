using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using MvcAsApi.Models;
using Newtonsoft.Json.Linq;

namespace MvcAsApi.Controllers
{
    public class HomeController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }

        public IActionResult Privacy(string name = "hello")
        {
            return View();
        }

        public IActionResult Contact()
        {
            return View(new ContactViewModel());
        }

        public IActionResult ErrorResponse()
        {
            return BadRequest();
        }

        [ValidateAntiForgeryToken]
        [HttpPost]
        public IActionResult Contact(ContactViewModel contactViewModel)
        {
            if(ModelState.IsValid)
            {
                return RedirectToAction(nameof(Index));
            }

            return View(contactViewModel);
        }

        public IActionResult Dynamic()
        {
            return View(new ContactViewModel());
        }

        [ValidateAntiForgeryToken]
        [HttpPost]
        public IActionResult Dynamic(dynamic contactViewModel)
        {
            if (ModelState.IsValid)
            {
                return RedirectToAction(nameof(Index));
            }

            var viewModel = contactViewModel.ToObject<ContactViewModel>();

            return View(viewModel);
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
