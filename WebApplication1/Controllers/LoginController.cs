using System;
using DomesticViolenceWebApp.Models;
using LiteDB;
using Microsoft.AspNetCore.Mvc;

namespace DomesticViolenceWebApp.Controllers
{
    [Route("Admin")]
    public class LoginController : Controller
    {

        [Route("Login")]
        public IActionResult Index()
        {
            using (var database = new LiteDatabase(@"Admins.db"))
            {
                var admins = database.GetCollection<Admin>("Admin");
                var admin = admins.FindOne(x => x.Mail == "admin" && x.Password == "AdminApiKey");

                if (admin == null)
                {
                    var newAdmin = new Admin
                    {
                        Id = Guid.NewGuid().ToString(),
                        Mail = "admin",
                        Password = "AdminApiKey"
                    };
                    admins.Insert(newAdmin);
                }
            }
            return View();
        }

        [Route("Login")]
        [HttpPost]
        public ActionResult Authorize(DomesticViolenceWebApp.Models.Admin admin)
        {
            using (var database = new LiteDatabase(@"Admins.db"))
            {
                var admins = database.GetCollection<Admin>("Admin");
                var searchedAdmin = admins.FindOne(x => x.Mail == admin.Mail && x.Password == admin.Password);
                if (searchedAdmin == null)
                {
                    admin.loginError = "Wrong mail or password ";
                    return View("Index", admin);
                }

                if (searchedAdmin != null)
                {
                    if (ModelState.IsValid)
                    {
                        TempData["ApiKey"] = admin.Password;
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