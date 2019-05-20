using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Net;
using Newtonsoft.Json;

namespace IntelektikaProjektas
{
    class Program
    {
        static WebClient CreateClientWithAuthorizationHeader()
        {
            WebClient client = new WebClient();
            var auth = new { Username = string.Empty, Key = string.Empty };
            using (var reader = new StreamReader("kaggle.json"))
            {
                var json = reader.ReadToEnd();
                auth = JsonConvert.DeserializeAnonymousType(json, auth);
            }
            var authToken = Convert.ToBase64String(
                ASCIIEncoding.ASCII.GetBytes(string.Format($"{auth.Username}:{auth.Key}", auth))
            );
            client.Headers.Set("Authorization", "Basic " + authToken);
            return client;
        }

        static List<FootballData> GetDataFromFile()
        {
            string[] lines = File.ReadAllLines("matches.csv");
            List<FootballData> data = new List<FootballData>();
            List<string[]> newData = new List<string[]>();
            lines = lines.Skip(1).ToArray();
            foreach (string line in lines)
            {
                string[] columns = line.Split(',');
                bool existsEmptyColumns = false;
                foreach (string column in columns)
                {
                    if(column == @"\N")
                    {
                        existsEmptyColumns = true;
                        break;
                    }
                }
                if (!existsEmptyColumns)
                {
                    newData.Add(columns);
                    /*string matchResult = columns[7];
                    int homeTeamRanking = int.Parse(columns[8]);
                    int awayTeamRanking = int.Parse(columns[9]);
                    double homeTeamWinOdds = double.Parse(columns[17]);
                    double awayTeamWinOdds = double.Parse(columns[18]);
                    double drawOdds = double.Parse(columns[19]);
                    FootballData matchData = new FootballData(matchResult, homeTeamRanking, awayTeamRanking, homeTeamWinOdds, awayTeamWinOdds, drawOdds);
                    data.Add(matchData);*/
                }
            }

            foreach(string[] row in newData)
            {
                //Console.WriteLine(row[0]);
            }

            return data;
        }

        static List<FootballData> ReadData()
        {
            if (!File.Exists("matches.csv"))
            {
                WebClient client = CreateClientWithAuthorizationHeader();
                string BaseApiUrl = "https://www.kaggle.com/api/v1/";
                string DatasetName = "paolof89/football-scientific-bets/matches.csv";
                client.DownloadFile(BaseApiUrl + "datasets/download/" + DatasetName, "matches.zip");
                System.IO.Compression.ZipFile.ExtractToDirectory("matches.zip", ".");
            }
            return GetDataFromFile();
        }

        static List<FootballData> GetTrainingData(int index, int segmentSize, List<FootballData> data)
        {
            List<FootballData> trainingData = new List<FootballData>();
            for (int i = 0; i < data.Count; i++)
            {
                if (i < (index * segmentSize) || i >= (index * segmentSize + segmentSize))
                {
                    trainingData.Add(data[i]);
                }
            }
            return trainingData;
        }

        static List<FootballData> GetTestingData(int index, int segmentSize, List<FootballData> data)
        {
            List<FootballData> testingData = new List<FootballData>();
            for (int i = 0; i < data.Count; i++)
            {
                if (i >= (index * segmentSize) && i < (index * segmentSize + segmentSize))
                {
                    testingData.Add(data[i]);
                }
            }
            return testingData;
        }

        static void AnalyzeTrainingData(List<FootballData> data)
        {
            Console.WriteLine("Total data count" + data.Count);
            Results results = new Results();
            foreach (FootballData matchData in data)
            {
                if (matchData.FullTimeResult == "H")
                {
                    results.HomeTeamWinCount++;
                }
                else if (matchData.FullTimeResult == "A")
                {
                    results.AwayTeamWinCount++;
                }
                else if (matchData.FullTimeResult == "D")
                {
                    results.DrawCount++;
                }
            }
            Console.WriteLine("home Win count " + results.HomeTeamWinCount);
            Console.WriteLine("away Win count " + results.AwayTeamWinCount);
            Console.WriteLine("draw count " + results.DrawCount);
            Console.WriteLine(results.HomeTeamWinCount + results.AwayTeamWinCount + results.DrawCount);
        }

        static void Main(string[] args)
        {
            int n = 10;
            List<FootballData> data = ReadData();
            int segmentSize = data.Count / n;
            /*for (int i = 0; i < 1; i++)
            {
                List<FootballData> trainingData = GetTrainingData(i, segmentSize, data);
                List<FootballData> testingData = GetTestingData(i, segmentSize, data);
                AnalyzeTrainingData(trainingData);
            }*/
        }
    }
}
