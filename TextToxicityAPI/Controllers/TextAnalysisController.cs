using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.ML;
using Microsoft.ML.Data;
using static Microsoft.ML.DataOperationsCatalog;
using Microsoft.ML.Trainers;
using Microsoft.ML.Transforms.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System.Text;
using System.Text.RegularExpressions;
using LiteDB;

namespace TextToxicityAPI.Controllers
{
    [Produces("application/json")]
    [Route("api/textanalysis")]
    public class TextAnalysisController : Controller
    {
        public static string _dataPath = Path.Combine(Environment.CurrentDirectory, "Data", "domestic_violence_dataset.txt");
        public static string _curseWordsInEnglishPath = Path.Combine(Environment.CurrentDirectory, "Data", "curses_in_english.txt");

        [HttpGet]
        public IActionResult GetTextAnalysis([FromBody] string text)
        {
            var textAnalysisResult = textAnalysis(text);
            var textAnalysisResultJson = JsonConvert.DeserializeObject(textAnalysisResult);
            return Ok(textAnalysisResultJson);
        }

        [HttpPost]
        public IActionResult SaveTextAnalysisForUser([FromHeader] string userId, [FromHeader] string timestamp, [FromHeader] string location, [FromBody] string text)
        {
            TextAnalysis userTextAnalysis = null;
            var textAnalysisResult = textAnalysis(text);
            using (var database = new LiteDatabase(@"TextAnalysis1.db"))
            {
                var usersTextAnalysis = database.GetCollection<TextAnalysis>("UserTextAnalysis");
                var allUsers = database.GetCollection<User>("User");
                var user = allUsers.FindOne(x => x.userId == userId);
                if (user == null)
                {
                    return NotFound("The user with id " + userId + " could not be found! Create an user first.");
                }
                else
                {
                    var newUserTextAnalysis = new TextAnalysis
                    {
                        Id = Guid.NewGuid().ToString(),
                        userId = userId,
                        text = text,
                        textAnalysisResult = textAnalysisResult,
                        timestamp = timestamp,
                        lastKnownLocation = location
                    };
                    userTextAnalysis = newUserTextAnalysis;
                    usersTextAnalysis.Insert(newUserTextAnalysis);
                }
                return Ok("Text analysis information for user with id " + userId + " has been successfully saved in the database.");
            }
        }

        [HttpGet("user")]
        public IActionResult GetTextAnalysisForUser([FromHeader] string userId)
        {
            List<TextAnalysis> infoTextAnalyses = new List<TextAnalysis>();
            List<TextAnalysisInfo> textAnalysesInfo = new List<TextAnalysisInfo>();
            UserTextAnalysis userTextAnalysisJson = new UserTextAnalysis();

            using (var database = new LiteDatabase(@"TextAnalysis1.db"))
            {
                var usersTextAnalysis = database.GetCollection<TextAnalysis>("UserTextAnalysis");
                var allUsers = database.GetCollection<User>("User");
                var user = allUsers.FindOne(x => x.userId == userId);
                if (user == null)
                {
                    return NotFound("The user with id " + userId + " could not be found!");
                }
                else
                {
                    var textAnalysisToBeFound = usersTextAnalysis.Find(x => x.userId == userId);
                    if (textAnalysisToBeFound.Count() == 0)
                    {
                        return NotFound("User with id " + userId + " has no text analysis saved.");
                    }
                    else
                    {
                        infoTextAnalyses = textAnalysisToBeFound.ToList();
                        foreach (var item in infoTextAnalyses)
                        {
                            TextAnalysisInfo textAnalysisInfo = new TextAnalysisInfo
                            {
                                text = item.text,
                                textAnalysisResult = item.textAnalysisResult,
                                timestamp = item.timestamp,
                                lastKnownLocation = item.lastKnownLocation
                            };
                            textAnalysesInfo.Add(textAnalysisInfo);
                        }

                        UserInfo userInfo = new UserInfo
                        {
                            userId = userId,
                            name = user.name,
                            age = user.age,
                            gender = user.gender
                        };

                        UserTextAnalysis userTextAnalysis = new UserTextAnalysis
                        {
                            userInfo = userInfo,
                            textAnalysesInfo = textAnalysesInfo
                        };
                        userTextAnalysisJson = userTextAnalysis;
                    }
                }
            }
            return Ok(userTextAnalysisJson);
        }

        #region ML
        private TrainTestData LoadData(MLContext mlContext)
        {
            IDataView dataView = mlContext.Data.LoadFromTextFile<SentimentData>(_dataPath, hasHeader: false);
            TrainTestData splitDataView = mlContext.Data.TrainTestSplit(dataView, testFraction: 0.2);
            return splitDataView;
        }


        private ITransformer BuildAndTrainModel(MLContext mlContext, IDataView splitTrainSet)
        {
            var estimator = mlContext.Transforms.Text.FeaturizeText(outputColumnName: "Features", inputColumnName: nameof(SentimentData.SentimentText))
            .Append(mlContext.BinaryClassification.Trainers.SdcaLogisticRegression(labelColumnName: "Label", featureColumnName: "Features"));

            Console.WriteLine("=============== Create and Train the Model ===============");
            var model = estimator.Fit(splitTrainSet);
            Console.WriteLine("=============== End of training ===============");
            Console.WriteLine();
            return model;
        }


        private void Evaluate(MLContext mlContext, ITransformer model, IDataView splitTestSet)
        {
            Console.WriteLine("=============== Evaluating Model accuracy with Test data===============");
            IDataView predictions = model.Transform(splitTestSet);
            CalibratedBinaryClassificationMetrics metrics = mlContext.BinaryClassification.Evaluate(predictions, "Label");
            Console.WriteLine();
            Console.WriteLine("Model quality metrics evaluation");
            Console.WriteLine("--------------------------------");
            Console.WriteLine($"Accuracy: {metrics.Accuracy:P2}");
            Console.WriteLine($"Auc: {metrics.AreaUnderRocCurve:P2}");
            Console.WriteLine($"F1Score: {metrics.F1Score:P2}");
            Console.WriteLine("=============== End of model evaluation ===============");
        }

        private string textAnalysis(string text)
        {
            MLContext mlContext = new MLContext();
            TrainTestData splitDataView = LoadData(mlContext);
            ITransformer model = BuildAndTrainModel(mlContext, splitDataView.TrainSet);
            Evaluate(mlContext, model, splitDataView.TestSet);

            PredictionEngine<SentimentData, SentimentPrediction> predictionFunction = mlContext.Model.CreatePredictionEngine<SentimentData, SentimentPrediction>(model);
            SentimentData sampleStatement = new SentimentData
            {
                SentimentText = text
            };

            //Console.WriteLine();
            //Console.WriteLine($"Sentiment: {resultPrediction.SentimentText} | Prediction: {(Convert.ToBoolean(resultPrediction.Prediction) ? "Positive" : "Negative")} | Probability: {resultPrediction.Probability} ");

            var resultPrediction = predictionFunction.Predict(sampleStatement);
            var curseCount = profanityCheck(text);

            var punctuation = text.Where(Char.IsPunctuation).Distinct().ToArray();
            var words = text.Split().Select(x => x.Trim(punctuation));

            float wordslength = words.Count();
            foreach (string word in words)
            {
                if (word.Length == 0)
                {
                    wordslength = wordslength - 1;
                }
            }

            float curseRatio = curseCount / wordslength;

            var result = new TextAnalysisResult
            {
                Context = (Convert.ToBoolean(resultPrediction.Prediction) ? "Positive" : "Negative"),
                CurseCount = curseCount,
                CurseRatio = curseRatio,
                GoodContextProbability = resultPrediction.Probability
            };

            var returnJson = JsonConvert.SerializeObject(result);

            return returnJson;
        }
        #endregion

        private int profanityCheck(string text)
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

#region helperClasses
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
    public string lastKnownLocation { get; set; }
}

public class TextAnalysis
{
    public string Id { get; set; }
    public string userId { get; set; }
    public string text { get; set; }
    public string textAnalysisResult { get; set; }
    public string timestamp { get; set; }
    public string lastKnownLocation { get; set; }
}
#endregion