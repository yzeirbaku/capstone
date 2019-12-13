using DomesticViolenceWebApp.Models;
using Microsoft.AspNetCore.Mvc;
using RestSharp;
using RestSharp.Serialization.Json;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Net;
using System.Web;

namespace DomesticViolenceWebApp.Controllers
{
    public class UsersController : Controller
    {
        private static string apiUrl = "http://domesticviolenceapi.azurewebsites.net";

        public IActionResult Index()
        {
            List<User> emptyList = new List<User>();
            var deserializer = new JsonDeserializer();
            var client = new RestClient(apiUrl);
            var request = new RestRequest("api/user/all", Method.GET);
            IRestResponse response = client.Execute(request);
            if (deserializer.Deserialize<String>(response) == "There are no users in the database.")
            {
                return View(emptyList);
            }
            else
            {
                var usersList = deserializer.Deserialize<List<User>>(response);
                return View(usersList.ToList());
            }
        }

        public ActionResult Create()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create([Bind("Id,userId,name,age,gender,location,emergencyContactOne,emergencyContactTwo,emergencyContactThree")] User user)
        {
            var deserializer = new JsonDeserializer();
            if (user == null)
            {
                throw new ArgumentNullException(nameof(user));
            }

            if (ModelState.IsValid)
            {
                var client = new RestClient(apiUrl);
                var request = new RestRequest("api/user", Method.POST);
                var newUser = new User()
                {
                    Id = Guid.NewGuid().ToString(),
                    userId = user.userId,
                    name = user.name,
                    age = user.age,
                    gender = user.gender,
                    location = user.location,
                    emergencyContactOne = user.emergencyContactOne,
                    emergencyContactTwo = user.emergencyContactTwo,
                    emergencyContactThree = user.emergencyContactThree
                };

                request.AddJsonBody(newUser);
                IRestResponse response = client.Execute(request);
                if (deserializer.Deserialize<String>(response).Contains("Assign a new user id."))
                {
                    TempData["msg"] = "<script>alert('An user with this id already exists!');</script>";
                    return RedirectToAction("Create");
                }
                return RedirectToAction("Index");
            }

            return View(user);
        }

        public ActionResult Details(string userId)
        {
            var deserializer = new JsonDeserializer();
            var client = new RestClient(apiUrl);
            var request = new RestRequest("api/user", Method.GET);
            request.AddHeader("userId", userId);
            IRestResponse response = client.Execute(request);
            var user = deserializer.Deserialize<User>(response);
            return View(user);
        }

        // GET: Users/Delete/5
        public ActionResult Delete(string userId)
        {
            var deserializer = new JsonDeserializer();
            var client = new RestClient(apiUrl);
            var request = new RestRequest("api/user", Method.GET);
            request.AddHeader("userId", userId);
            IRestResponse response = client.Execute(request);
            var user = deserializer.Deserialize<User>(response);
            return View(user);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(string userId)
        {
            var client = new RestClient(apiUrl);
            var request = new RestRequest("api/user", Method.DELETE);
            request.AddQueryParameter("userId", userId);
            IRestResponse response = client.Execute(request);
            return RedirectToAction("Index");
        }


        // GET: Users/Edit/5
        public ActionResult Edit(string userId)
        {
            var deserializer = new JsonDeserializer();
            var client = new RestClient(apiUrl);
            var request = new RestRequest("api/user", Method.GET);
            request.AddHeader("userId", userId);
            IRestResponse response = client.Execute(request);
            var user = deserializer.Deserialize<User>(response);
            return View(user);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit([Bind("Id,userId,name,age,gender,location,emergencyContactOne,emergencyContactTwo,emergencyContactThree")] User user)
        {
            var deserializer = new JsonDeserializer();
            if (user == null)
            {
                throw new ArgumentNullException(nameof(user));
            }

            if (ModelState.IsValid)
            {
                var client = new RestClient(apiUrl);
                var request = new RestRequest("api/user/update", Method.POST);
                var newUser = new User()
                {
                    Id = Guid.NewGuid().ToString(),
                    userId = user.userId,
                    name = user.name,
                    age = user.age,
                    gender = user.gender,
                    location = user.location,
                    emergencyContactOne = user.emergencyContactOne,
                    emergencyContactTwo = user.emergencyContactTwo,
                    emergencyContactThree = user.emergencyContactThree
                };

                request.AddHeader("userId", user.userId);
                request.AddJsonBody(newUser);
                IRestResponse response = client.Execute(request);
                return RedirectToAction("Index");
            }
            return View(user);
        }

        public IActionResult LogOut()
        {
            return RedirectToAction("Index", "Home");
        }
    }
}
