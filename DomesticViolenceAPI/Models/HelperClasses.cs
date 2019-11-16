using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace TextToxicityAPI.Models
{
    public class Gender
    {
        public string Id { get; set; }
        public string genderString { get; set; }
    }

    public class TextAnalysisResult
    {
        public string Context { get; set; }
        public int CurseCount { get; set; }
        public float CurseRatio { get; set; }
        public float GoodContextProbability { get; set; }
    }

    public class User
    {
        public string Id { get; set; }
        public string userId { get; set; }
        public string name { get; set; }
        public string age { get; set; }
        public string gender { get; set; }
        public string location { get; set; }
        public string emergencyContactOne { get; set; }
        public string emergencyContactTwo { get; set; }
        public string emergencyContactThree { get; set; }
    }

    public class UserTextAnalysis
    {
        public UserInfo userInfo { get; set; }
        public List<TextAnalysisInfo> textAnalysesInfo { get; set; }
    }

    public class UserInfo
    {
        public string userId { get; set; }
        public string name { get; set; }
        public string gender { get; set; }
        public string age { get; set; }
    }

    public class TextAnalysisInfo
    {
        public string text { get; set; }
        public string textAnalysisResult { get; set; }
        public string timestamp { get; set; }
        public string date { get; set; }
        public string lastKnownLocation { get; set; }
    }

    public class TextAnalysis
    {
        public string Id { get; set; }
        public string userId { get; set; }
        public string text { get; set; }
        public string textAnalysisResult { get; set; }
        public string timestamp { get; set; }
        public string date { get; set; }
        public string lastKnownLocation { get; set; }
    }
}
