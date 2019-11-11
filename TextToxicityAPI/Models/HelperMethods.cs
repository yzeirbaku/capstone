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

        public void updateUser(string userId, string property)
        {
            using (var database = new LiteDatabase("TextAnalysis1.db"))
            {
                var users = database.GetCollection<User>("User");
                var userToBeUpdated = users.FindOne(x => x.userId == userId);
                List<string> propertyNames = new List<string>();
                List<string> propertyValues = new List<string>();

                if (userToBeUpdated == null)
                {
                    return;
                }

                var propertyNamesAndValues = property.Split(",");
                foreach (var item in propertyNamesAndValues)
                {
                    var properties = item.Split(":");
                    propertyNames.Add(properties[0]);
                    propertyValues.Add(properties[1]);
                }
                foreach (var (item, index) in propertyNames.Select((v, i) => (v, i)))
                {
                    if (item.Contains("name"))
                    {
                        userToBeUpdated.name = propertyValues[index];
                    }
                    if (item.Contains("age"))
                    {
                        userToBeUpdated.age = propertyValues[index];
                    }
                    if (item.Contains("gender"))
                    {
                        userToBeUpdated.gender = propertyValues[index];
                    }
                    if (item.Contains("location"))
                    {
                        userToBeUpdated.location = propertyValues[index];
                    }
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

