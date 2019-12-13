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
            using (var database = new LiteDatabase(@"Admins.db"))
            {
                var admins = database.GetCollection<Admin>("Admin");
                var admin = admins.FindOne(x => x.Mail == "admin" && x.Password == "1234");
                if (admin == null)
                {
                    var newAdmin = new Admin
                    {
                        Id = Guid.NewGuid().ToString(),
                        Mail = "admin",
                        Password = "1234"
                    };
                    admins.Insert(newAdmin);
                }
            }
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
                        return RedirectToAction("Index", "Users");
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
            HttpContext.Session.Clear();
            return RedirectToAction("Index", "Home");
        }
    }
}