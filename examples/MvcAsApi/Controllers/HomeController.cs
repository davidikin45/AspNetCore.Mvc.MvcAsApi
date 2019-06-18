using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using MvcAsApi.Models;

namespace MvcAsApi.Controllers
{
    public class HomeController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }

        public IActionResult Contact()
        {
            return View(new ContactViewModel());
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

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
