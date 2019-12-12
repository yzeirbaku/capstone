using System;
using DomesticViolenceWebApp.Models;
using LiteDB;
using Microsoft.AspNetCore.Mvc;

namespace DomesticViolenceWebApp.Controllers
{
    [Route("admin")]
    public class LoginController : Controller
    {
        [Route("login")]
        public IActionResult Index()
        {
            return View();
        }

        [Route("login")]
        [HttpPost]
        public ActionResult Authorize(DomesticViolenceWebApp.Models.Admin admin)
        {
            using (var database = new LiteDatabase(@"Admins.db"))
            {
                var admins = database.GetCollection<Admin>("Admin");
                var searchedAdmin = admins.FindOne(x => x.Mail == admin.Mail && x.Password == admin.Password);
                if (searchedAdmin == null)
                {
                    admin.loginError = "Wrong username or password ";
                    return View("Index", admin);
                }

                if (searchedAdmin != null)
                {
                    if (ModelState.IsValid)
                    {
                        return Redirect("http://domesticviolenceapi.azurewebsites.net/swagger");
                    }
                    else
                    {
                        return View("Index");
                    }
                }
                return null;
            }


        }


        public ActionResult LogOut()
        {
            return RedirectToAction("Index", "Home");
        }
    }
}