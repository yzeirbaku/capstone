using LiteDB;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace TextToxicityAPI.Models
{
    public class HelperMethods
    {
        public static string _curseWordsInEnglishPath = Path.Combine(Environment.CurrentDirectory, "Data", "curses_in_english.txt");

        public string getGender(string gender)
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

        public void updateUser(string userId, User user)
        {
            using (var database = new LiteDatabase("TextAnalysis1.db"))
            {
                var users = database.GetCollection<User>("User");
                var userToBeUpdated = users.FindOne(x => x.userId == userId);

                if (userToBeUpdated == null)
                {
                    return;
                }

                if (user.name != null)

                    if (userToBeUpdated.name != user.name)
                    {
                        userToBeUpdated.name = user.name;
                    }

                if (user.location != null)

                    if (userToBeUpdated.location != user.location)
                    {
                        userToBeUpdated.location = user.location;
                    }

                if (user.age != null)

                    if (userToBeUpdated.age != user.age)
                    {
                        userToBeUpdated.age = user.age;
                    }

                if (user.gender != null)

                    if (userToBeUpdated.gender != user.gender)
                    {
                        userToBeUpdated.gender = user.gender;
                    }

                if (user.emergencyContactOne != null)

                    if (userToBeUpdated.emergencyContactOne != user.emergencyContactOne)
                    {
                        userToBeUpdated.emergencyContactOne = user.emergencyContactOne;
                    }

                if (user.emergencyContactTwo != null)

                    if (userToBeUpdated.emergencyContactTwo != user.emergencyContactTwo)
                    {
                        userToBeUpdated.emergencyContactTwo = user.emergencyContactTwo;
                    }

                if (user.emergencyContactThree != null)

                    if (userToBeUpdated.emergencyContactThree != user.emergencyContactThree)
                    {
                        userToBeUpdated.emergencyContactThree = user.emergencyContactThree;
                    }

                users.Update(userToBeUpdated);
            }
        }

        public int profanityCheck(string text)
        {
            StreamReader streamReader = new StreamReader(_curseWordsInEnglishPath);
            string stringWithMultipleSpaces = streamReader.ReadToEnd();
            streamReader.Close();
            Regex r = new Regex(" +");
            string[] curseWords = r.Split(stringWithMultipleSpaces);

            int count = 0;
            var punctuation = text.Where(Char.IsPunctuation).Distinct().ToArray();
            var words = text.Split().Select(x => x.Trim(punctuation));

            foreach (string word in words)
            {
                if (curseWords.Contains(word))
                {
                    count++;
                }
            }
            return count;
        }
    }
}

