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
using TextToxicityAPI.Models;
using System.Globalization;
using DomesticViolenceAPI;

namespace TextToxicityAPI.Controllers
{
    [Produces("application/json")]
    [Route("api/textanalysis")]
    public class TextAnalysisController : Controller
    {
        private HelperMethods helperMethods = new HelperMethods();
        public static string _dataPath = Path.Combine(Environment.CurrentDirectory, "Data", "domestic_violence_dataset.txt");

        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public IActionResult GetTextAnalysis([FromBody] string text)
        {
            var result = helperMethods.processedText(text);
            var textAnalysisResult = textAnalysis(result);
            var textAnalysisResultJson = JsonConvert.DeserializeObject(textAnalysisResult);
            return Ok(textAnalysisResultJson);
        }

        [HttpPost]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public IActionResult SaveTextAnalysisForUser([FromHeader] string userId, [FromHeader] string timestamp, [FromHeader] string date, [FromHeader] string lastKnownLocation, [FromBody] string text)
        {
            TextAnalysis userTextAnalysis = null;
            var result = helperMethods.processedText(text);
            var textAnalysisResult = textAnalysis(result);
            using (var database = new LiteDatabase(@"TextAnalysis1.db"))
            {
                var usersTextAnalysis = database.GetCollection<TextAnalysis>("UserTextAnalysis");
                var allUsers = database.GetCollection<User>("User");
                var user = allUsers.FindOne(x => x.userId == userId);
                if (user == null)
                {
                    return NotFound("The user with id " + userId + " could not be found! Create the user first.");
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
                        date = date,
                        lastKnownLocation = lastKnownLocation
                    };
                    userTextAnalysis = newUserTextAnalysis;
                    usersTextAnalysis.Insert(newUserTextAnalysis);
                }
                return Ok("Text analysis information for user with id " + userId + " has been successfully saved in the database.");
            }
        }

        [HttpDelete]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public IActionResult DeleteAllTextAnalyses()
        {
            using (var database = new LiteDatabase(@"TextAnalysis1.db"))
            {
                var usersTextAnalyses = database.GetCollection<TextAnalysis>("UserTextAnalysis");
                var allTextAnalyses = usersTextAnalyses.FindAll();
                foreach (var item in allTextAnalyses)
                {
                    usersTextAnalyses.Delete(item.Id);
                }
                if (usersTextAnalyses.Count() == 0)
                {
                    return Ok("All text analyses for all users have been deleted from the database.");
                }
                else
                {
                    return BadRequest("Could not delete all text analyses for all users!");
                }
            }
        }

        [HttpGet("user")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public IActionResult GetTextAnalysisForUser([FromHeader] string userId)
        {
            List<TextAnalysis> infoTextAnalyses = new List<TextAnalysis>();
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
                        return NotFound("The user with id " + userId + " has no text analysis saved.");
                    }
                    else
                    {
                        infoTextAnalyses = textAnalysisToBeFound.ToList();
                        userTextAnalysisJson = helperMethods.getUserTextAnalysis(infoTextAnalyses, userId);
                    }
                }
            }
            return Ok(userTextAnalysisJson);
        }

        [HttpDelete("user")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public IActionResult DeleteAllTextAnalysesForUser([FromQuery] string userId)
        {
            using (var database = new LiteDatabase(@"TextAnalysis1.db"))
            {
                var userTextAnalyses = database.GetCollection<TextAnalysis>("UserTextAnalysis");
                var allUsers = database.GetCollection<User>("User");
                var allUserTextAnalyses = userTextAnalyses.Find(x => x.userId == userId);
                var user = allUsers.FindOne(x => x.userId == userId);
                if (user == null)
                {
                    return NotFound("The user with id " + userId + " could not be found! Create the user first.");
                }
                else
                {
                    if (allUserTextAnalyses.Count() == 0)
                    {
                        return NotFound("The user with id " + userId + " has no text analysis saved.");
                    }
                    else
                    {
                        foreach (var item in allUserTextAnalyses)
                        {
                            userTextAnalyses.Delete(item.Id);
                        }

                        return Ok("All text analyses for the user with id " + userId + " have been deleted from the database.");

                    }
                }
            }
        }

        [HttpGet("user/date")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public IActionResult GetTextAnalysisForUserWithDate([FromHeader] string userId, [FromHeader] string date, [FromHeader] string startingTime, [FromHeader] string endingTime)
        {
            List<TextAnalysis> infoTextAnalyses = new List<TextAnalysis>();
            UserTextAnalysis userTextAnalysisJson = new UserTextAnalysis();
            List<string> textAnalysesTimestampsWithinRange = new List<string>();

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
                    if (startingTime != null && endingTime != null)
                    {
                        var dateAndStartTime = date + " " + startingTime;
                        var startTime = DateTime.ParseExact(dateAndStartTime, "MM-dd-yyyy HH:mm:ss", CultureInfo.InvariantCulture);
                        var dateAndEndTime = date + " " + endingTime;
                        var endTime = DateTime.ParseExact(dateAndEndTime, "MM-dd-yyyy HH:mm:ss", CultureInfo.InvariantCulture);

                        var startTimeToParse = startTime.Hour.ToString() + ":" + startTime.Minute + ":" + startTime.Second;
                        var endTimeToParse = endTime.Hour.ToString() + ":" + endTime.Minute + ":" + endTime.Second;
                        TimeSpan startingTimeSpan = TimeSpan.Parse(startTimeToParse);
                        TimeSpan endingTimeSpan = TimeSpan.Parse(endTimeToParse);

                        var allTextAnalysisForUser = usersTextAnalysis.Find(x => x.userId == userId);
                        var allTextAnalysisForUserWithDate = allTextAnalysisForUser.Where(x => x.date == date);
                        if (allTextAnalysisForUser.Count() == 0)
                        {
                            return NotFound("The user with id " + userId + " has no text analysis saved.");
                        }
                        else if (allTextAnalysisForUserWithDate.Count() == 0)
                        {
                            return NotFound("The user with id " + userId + " has no text analysis saved for this date.");
                        }
                        else
                        {
                            foreach (var item in allTextAnalysisForUserWithDate)
                            {
                                var userTextAnalysisTimestamp = item.timestamp;
                                DateTime userTextAnalysisDateTime = DateTime.Parse(userTextAnalysisTimestamp);
                                var userTextAnalysisDateTimeToParse = userTextAnalysisDateTime.Hour.ToString() + ":" + userTextAnalysisDateTime.Minute + ":" + userTextAnalysisDateTime.Second;
                                TimeSpan userTextAnalysisTimeSpan = TimeSpan.Parse(userTextAnalysisDateTimeToParse);

                                if (startingTimeSpan <= endingTimeSpan)
                                {
                                    // start and stop times are in the same day
                                    if (userTextAnalysisTimeSpan >= startingTimeSpan && userTextAnalysisTimeSpan <= endingTimeSpan)
                                    {
                                        textAnalysesTimestampsWithinRange.Add(userTextAnalysisTimeSpan.ToString());
                                    }
                                }
                                else
                                {
                                    // start and stop times are in different days
                                    if (userTextAnalysisTimeSpan >= startingTimeSpan || userTextAnalysisTimeSpan <= endingTimeSpan)
                                    {
                                        textAnalysesTimestampsWithinRange.Add(userTextAnalysisTimeSpan.ToString());
                                    }
                                }
                            }

                            foreach (var item in textAnalysesTimestampsWithinRange)
                            {
                                var userTextAnalysesWithinTimeRange = allTextAnalysisForUserWithDate.Where(x => x.timestamp == item).ToList();

                                foreach (var timeRange in userTextAnalysesWithinTimeRange)
                                {
                                    infoTextAnalyses.Add(timeRange);
                                }
                                break;
                            }

                            userTextAnalysisJson = helperMethods.getUserTextAnalysis(infoTextAnalyses, userId);

                            return Ok(userTextAnalysisJson);
                        }
                    }

                    else
                    {
                        var allTextAnalysisForUser = usersTextAnalysis.Find(x => x.userId == userId);
                        var allTextAnalysisForUserWithDate = allTextAnalysisForUser.Where(x => x.date == date);
                        if (allTextAnalysisForUser.Count() == 0)
                        {
                            return NotFound("The user with id " + userId + " has no text analysis saved.");
                        }
                        else if (allTextAnalysisForUserWithDate.Count() == 0)
                        {
                            return NotFound("The user with id " + userId + " has no text analysis saved for this date.");
                        }
                        else
                        {
                            infoTextAnalyses = allTextAnalysisForUserWithDate.ToList();
                            userTextAnalysisJson = helperMethods.getUserTextAnalysis(infoTextAnalyses, userId);
                        }
                    }
                }
                return Ok(userTextAnalysisJson);
            }
        }

        [HttpGet("user/dates")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public IActionResult GetTextAnalysisForUserBetweenDates([FromHeader] string userId, [FromHeader] string startingDate, [FromHeader] string endingDate, [FromHeader] string negative)
        {
            List<TextAnalysis> infoTextAnalyses = new List<TextAnalysis>();
            UserTextAnalysis userTextAnalysisJson = new UserTextAnalysis();
            List<TextAnalysisResult> textAnalysisResults = new List<TextAnalysisResult>();
            List<string> textAnalysesTimestampsWithinRange = new List<string>();
            List<string> parsedDatesBetweenRange = new List<string>();
            List<string> datesBetweenRange = new List<string>();
            List<TextAnalysis> allTextAnalysesForDateRange = new List<TextAnalysis>();

            if (negative != null)
            {
                if (startingDate != null && endingDate != null)
                {
                    var changedStartingDate = startingDate.Replace("-", "/");
                    var changedEndingDate = endingDate.Replace("-", "/");
                    var parsedStartingDate = DateTime.ParseExact(changedStartingDate, "MM/dd/yyyy", null);
                    var parsedEndingDate = DateTime.ParseExact(changedEndingDate, "MM/dd/yyyy", null);

                    if ((helperMethods.GetDateRange(parsedStartingDate, parsedEndingDate)).Count() == 0)
                    {
                        return BadRequest("Start date is bigger than end date!");
                    }

                    foreach (DateTime date in helperMethods.GetDateRange(parsedStartingDate, parsedEndingDate))
                    {
                        parsedDatesBetweenRange.Add(date.ToShortDateString());
                    }

                    foreach (var date in parsedDatesBetweenRange)
                    {
                        var changedDate = date.Replace("/", "-");
                        datesBetweenRange.Add(changedDate);
                    }
                }
                else if (startingDate == null || endingDate == null)
                {
                    return BadRequest("Start or end date not specified!");
                }

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
                        var allTextAnalysisForUser = usersTextAnalysis.Find(x => x.userId == userId);
                        foreach (var date in datesBetweenRange)
                        {
                            var allTextAnalysisForUserWithDate = allTextAnalysisForUser.Where(x => x.date == date);
                            foreach (var item in allTextAnalysisForUserWithDate)
                            {
                                allTextAnalysesForDateRange.Add(item);
                            }

                        }

                        if (allTextAnalysisForUser.Count() == 0)
                        {
                            return NotFound("The user with id " + userId + " has no text analysis saved.");
                        }
                        else if (allTextAnalysesForDateRange.Count() == 0)
                        {
                            return NotFound("The user with id " + userId + " has no text analysis saved for this date range.");
                        }
                        else
                        {
                            foreach(var textAnalysis in allTextAnalysesForDateRange)
                            {
                                var textAnalysisResultJson = JsonConvert.DeserializeObject<TextAnalysisResult>(textAnalysis.textAnalysisResult);
                                textAnalysisResults.Add(textAnalysisResultJson);
                            }

                            var negativeTextAnalysisResults = textAnalysisResults.Where(x => x.Context == "Negative").ToList();

                            foreach (var item in negativeTextAnalysisResults)
                            {
                                var negativeList = allTextAnalysesForDateRange.Where(x => x.textAnalysisResult == JsonConvert.SerializeObject(item)).ToList();

                                foreach (var negativeItem in negativeList)
                                {
                                    infoTextAnalyses.Add(negativeItem);
                                }
                                break;
                            }
                            
                            userTextAnalysisJson = helperMethods.getUserTextAnalysis(infoTextAnalyses, userId);
                            return Ok(userTextAnalysisJson);
                        }
                    }
                }
            }
            else
            {
                if (startingDate != null && endingDate != null)
                {
                    var changedStartingDate = startingDate.Replace("-", "/");
                    var changedEndingDate = endingDate.Replace("-", "/");
                    var parsedStartingDate = DateTime.ParseExact(changedStartingDate, "MM/dd/yyyy", null);
                    var parsedEndingDate = DateTime.ParseExact(changedEndingDate, "MM/dd/yyyy", null);

                    if ((helperMethods.GetDateRange(parsedStartingDate, parsedEndingDate)).Count() == 0)
                    {
                        return BadRequest("Start date is bigger than end date!");
                    }

                    foreach (DateTime date in helperMethods.GetDateRange(parsedStartingDate, parsedEndingDate))
                    {
                        parsedDatesBetweenRange.Add(date.ToShortDateString());
                    }

                    foreach (var date in parsedDatesBetweenRange)
                    {
                        var changedDate = date.Replace("/", "-");
                        datesBetweenRange.Add(changedDate);
                    }
                }
                else if (startingDate == null || endingDate == null)
                {
                    return BadRequest("Start or end date not specified!");
                }

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
                        var allTextAnalysisForUser = usersTextAnalysis.Find(x => x.userId == userId);
                        foreach (var date in datesBetweenRange)
                        {
                            var allTextAnalysisForUserWithDate = allTextAnalysisForUser.Where(x => x.date == date);
                            foreach (var item in allTextAnalysisForUserWithDate)
                            {
                                allTextAnalysesForDateRange.Add(item);
                            }

                        }

                        if (allTextAnalysisForUser.Count() == 0)
                        {
                            return NotFound("The user with id " + userId + " has no text analysis saved.");
                        }
                        else if (allTextAnalysesForDateRange.Count() == 0)
                        {
                            return NotFound("The user with id " + userId + " has no text analysis saved for this date range.");
                        }
                        else
                        {
                            userTextAnalysisJson = helperMethods.getUserTextAnalysis(allTextAnalysesForDateRange, userId);
                            return Ok(userTextAnalysisJson);
                        }
                    }
                }
            }
        }

        [HttpGet("user/negative")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public IActionResult GetNegativeTextAnalysisForUser([FromHeader] string userId, [FromHeader] string date)
        {
            List<TextAnalysis> infoTextAnalyses = new List<TextAnalysis>();
            List<TextAnalysisResult> textAnalysisResults = new List<TextAnalysisResult>();
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
                    if (date != null)
                    {
                        var allTextAnalysisForUser = usersTextAnalysis.Find(x => x.userId == userId);
                        var allTextAnalysisForUserWithDate = allTextAnalysisForUser.Where(x => x.date == date).ToList();

                        foreach (var item in allTextAnalysisForUserWithDate)
                        {
                            var textAnalysisResultJson = JsonConvert.DeserializeObject<TextAnalysisResult>(item.textAnalysisResult);
                            textAnalysisResults.Add(textAnalysisResultJson);
                        }

                        var negativeTextAnalysisResults = textAnalysisResults.Where(x => x.Context == "Negative").ToList();

                        foreach (var item in negativeTextAnalysisResults)
                        {
                            var negativeList = allTextAnalysisForUserWithDate.Where(x => x.textAnalysisResult == JsonConvert.SerializeObject(item)).ToList();

                            foreach (var negative in negativeList)
                            {
                                infoTextAnalyses.Add(negative);
                            }
                            break;
                        }

                        if (allTextAnalysisForUser.Count() == 0)
                        {
                            return NotFound("The user with id " + userId + " has no text analysis saved.");
                        }
                        else if (allTextAnalysisForUserWithDate.Count() == 0)
                        {
                            return NotFound("The user with id " + userId + " has no text analysis saved for this date.");
                        }
                        else
                        {
                            userTextAnalysisJson = helperMethods.getUserTextAnalysis(infoTextAnalyses, userId);
                        }
                    }

                    else
                    {
                        var allTextAnalysisForUser = usersTextAnalysis.Find(x => x.userId == userId);

                        foreach (var item in allTextAnalysisForUser)
                        {
                            var textAnalysisResultJson = JsonConvert.DeserializeObject<TextAnalysisResult>(item.textAnalysisResult);
                            textAnalysisResults.Add(textAnalysisResultJson);
                        }

                        var negativeTextAnalysisResults = textAnalysisResults.Where(x => x.Context == "Negative").ToList();

                        foreach (var item in negativeTextAnalysisResults)
                        {
                            var negativeList = allTextAnalysisForUser.Where(x => x.textAnalysisResult == JsonConvert.SerializeObject(item)).ToList();

                            foreach (var negative in negativeList)
                            {
                                infoTextAnalyses.Add(negative);
                            }
                            break;
                        }

                        if (allTextAnalysisForUser.Count() == 0)
                        {
                            return NotFound("The user with id " + userId + " has no text analysis saved.");
                        }
                        else
                        {
                            userTextAnalysisJson = helperMethods.getUserTextAnalysis(infoTextAnalyses, userId);
                        }
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
            var curseCount = helperMethods.profanityCheck(text);

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
                CurseRatio = curseRatio
            };

            var returnJson = JsonConvert.SerializeObject(result);

            return returnJson;
        }
        #endregion
    }
}