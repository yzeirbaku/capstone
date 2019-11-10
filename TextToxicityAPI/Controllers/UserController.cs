using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using LiteDB;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace TextToxicityAPI.Controllers
{
    [Produces("application/json")]
    [Route("api/user")]
    public class UserController : Controller
    {
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
                    return NotFound("The user with id " + searchedUser.userId + " could not be found!");
                }
                else
                {
                    user.Id = searchedUser.Id;
                    user.userId = searchedUser.userId;
                    user.name = searchedUser.name;
                    user.age = searchedUser.age;
                    user.location = searchedUser.location;
                    user.gender = getGender(searchedUser.gender);
                }
            }
            return Ok(user);
        }

        [HttpPost]
        public IActionResult SaveUser([FromHeader] string userId, [FromHeader] string name, [FromHeader] string age, [FromHeader] string gender, [FromHeader] string location)
        {
            using (var database = new LiteDatabase(@"TextAnalysis1.db"))
            {
                var users = database.GetCollection<User>("User");
                var newUser = new User
                {
                    Id = Guid.NewGuid().ToString(),
                    userId = userId,
                    name = name,
                    age = age,
                    gender = gender,
                    location = location
                };
                users.Insert(newUser);
            }

            return Ok("The user " + name + " with id " + userId + " has been successfully saved in the database.");
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
                    return NotFound("The user with id " + userToBeDeleted.userId + " could not be found!");
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

        [HttpGet("all")]
        public IActionResult GetUsers([FromHeader] string userId)
        {
            IEnumerable<User> allUsers = null;
            List<User> jsonAllUsers = new List<User>();
            using (var database = new LiteDatabase(@"TextAnalysis1.db"))
            {
                var users = database.GetCollection<User>("User");
                var genders = database.GetCollection<Gender>("Gender");
                allUsers = users.Find(x => x.userId != null);

                if (allUsers.Count() == 0)
                {
                    return Ok("There are no users in the database.");
                }
                else
                {
                    foreach (var user in allUsers)
                    {
                        user.gender = getGender(user.gender);
                        jsonAllUsers.Add(user);
                    }
                }

                return Ok(jsonAllUsers);
            }
        }

        private string getGender(string gender)
        {
            using (var database = new LiteDatabase(@"TextAnalysis1.db"))
            {
                var genders = database.GetCollection<Gender>("Gender");
                var Female = new Gender { Id = Guid.NewGuid().ToString(), genderString = "Female" };
                var Male = new Gender { Id = Guid.NewGuid().ToString(), genderString = "Male" };

                if (genders.Count() == 0)
                {
                    genders.Insert(Female);
                    genders.Insert(Male);

                    if (gender == "1")
                    {
                        var maleGender = genders.FindOne(x => x.genderString == "Male");
                        return maleGender.genderString;
                    }
                    else
                    {
                        var femaleGender = genders.FindOne(x => x.genderString == "Female");
                        return femaleGender.genderString;
                    }
                }

                else
                {
                    if (gender == "1")
                    {
                        var maleGender = genders.FindOne(x => x.genderString == "Male");
                        return maleGender.genderString;
                    }
                    else
                    {
                        var femaleGender = genders.FindOne(x => x.genderString == "Female");
                        return femaleGender.genderString;
                    }
                }
            }
        }
    }
}

#region helperClasses
public class Gender
{
    public string Id { get; set; }
    public string genderString { get; set; }
}
#endregion