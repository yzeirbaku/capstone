using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using LiteDB;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using TextToxicityAPI.Models;

namespace TextToxicityAPI.Controllers
{
    [Produces("application/json")]
    [Route("api/user")]
    public class UserController : Controller
    {
        HelperMethods helperMethods = new HelperMethods();

        [HttpGet]
        public IActionResult GetUser([FromHeader] string userId)
        {
            User user = new User();
            using (var database = new LiteDatabase(@"TextAnalysis1.db"))
            {
                var users = database.GetCollection<User>("User");
                var searchedUser = users.FindOne(x => x.userId == userId);

                if (searchedUser == null || searchedUser.Id.Length == 0)
                {
                    return NotFound("The user with id " + userId + " could not be found!");
                }
                else
                {
                    user.Id = searchedUser.Id;
                    user.userId = searchedUser.userId;
                    user.name = searchedUser.name;
                    user.age = searchedUser.age;
                    user.location = searchedUser.location;
                    user.gender = helperMethods.getGender(searchedUser.gender);
                    user.emergencyContactOne = searchedUser.emergencyContactOne;
                    user.emergencyContactTwo = searchedUser.emergencyContactTwo;
                    user.emergencyContactThree = searchedUser.emergencyContactThree;
                }
            }
            return Ok(user);
        }

        [HttpPost]
        public IActionResult SaveUser([FromBody] User user)
        {
            using (var database = new LiteDatabase(@"TextAnalysis1.db"))
            {
                var users = database.GetCollection<User>("User");
                var existingUser = users.FindOne(x => x.userId == user.userId);

                if (existingUser == null)
                {
                    var newUser = new User
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
                    users.Insert(newUser);
                }
                else
                {
                    return BadRequest("An user with id " + user.userId + " already exists in the database! Assign a new user id.");
                }
            }
            return Ok("The user " + user.name + " with id " + user.userId + " has been successfully saved in the database.");
        }

        [HttpDelete]
        public IActionResult DeleteUser([FromQuery] string userId)
        {
            string nameOfUserToBeDeleted = null;
            using (var database = new LiteDatabase(@"TextAnalysis1.db"))
            {
                var users = database.GetCollection<User>("User");
                var userToBeDeleted = users.FindOne(x => x.userId == userId);
                if (userToBeDeleted == null || userToBeDeleted.Id.Length == 0)
                {
                    return NotFound("The user with id " + userId + " could not be found!");
                }
                else
                {
                    var idOfUserToBeDeleted = userToBeDeleted.Id;
                    nameOfUserToBeDeleted = userToBeDeleted.name;
                    users.Delete(idOfUserToBeDeleted);
                }
                return Ok("The user " + nameOfUserToBeDeleted + " with id " + userId + " has been successfully deleted from the database.");
            }
        }

        [HttpPost("update")]
        public IActionResult UpdateUser([FromHeader] string userId, [FromBody] User user)
        {
            using (var database = new LiteDatabase("TextAnalysis1.db"))
            {
                helperMethods.updateUser(userId, user);
                var users = database.GetCollection<User>("User");
                var updatedUser = users.FindOne(x => x.userId == userId);
                if (updatedUser == null)
                {
                    return NotFound("The user with id " + userId + " could not be found!");
                }
                else
                {
                    updatedUser.gender = helperMethods.getGender(updatedUser.gender);
                    return Ok(updatedUser);
                }
            }
        }

        [HttpGet("all")]
        public IActionResult GetUsers([FromHeader] string userId)
        {
            IEnumerable<User> allUsers = null;
            List<User> jsonAllUsers = new List<User>();
            using (var database = new LiteDatabase(@"TextAnalysis1.db"))
            {
                var users = database.GetCollection<User>("User");
                allUsers = users.Find(x => x.Id != null);
                if (allUsers.Count() == 0)
                {
                    return Ok("There are no users in the database.");
                }
                else
                {
                    foreach (var user in allUsers)
                    {
                        user.gender = helperMethods.getGender(user.gender);
                        jsonAllUsers.Add(user);
                    }
                }
                return Ok(jsonAllUsers);
            }
        }

        [HttpDelete("all")]
        public IActionResult DeleteUsers()
        {
            List<User> allUsers = new List<User>();
            using (var database = new LiteDatabase(@"TextAnalysis1.db"))
            {
                var users = database.GetCollection<User>("User");
                allUsers = users.Find(x => x.Id != null).ToList();
                if (allUsers.Count() == 0)
                {
                    return Ok("There are no users in the database.");
                }
                else
                {
                    foreach (var user in allUsers)
                    {
                        users.Delete(user.Id);
                    }
                    return Ok("All the users have been deleted from the database.");
                }
            }
        }
    }
}
