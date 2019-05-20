using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Net;
using Newtonsoft.Json;
using MathNet.Numerics.LinearAlgebra;

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

        /*static string GetTypeOfOdds(double odds)
        {
            return odds <= 2 ? "small" : odds > 3.5 ? "big" : "medium";
        }

        static string GetTypeOfRanking(int ranking)
        {
            return ranking <= 1600 ? "small" : ranking > 1800 ? "big" : "medium";
        }*/

        /*static double[] calculateOddsBoundaries(List<string[]> lines)
        {
            double max = 0;
            double min = double.Parse(lines[0][17]);
            foreach (string[] columns in lines)
            {
                foreach (string column in columns)
                {
                    double homeTeamWinOdds = double.Parse(columns[17]);
                    double awayTeamWinOdds = double.Parse(columns[18]);
                    double drawOdds = double.Parse(columns[19]);
                    if (max < homeTeamWinOdds) max = homeTeamWinOdds;
                    if (max < awayTeamWinOdds) max = awayTeamWinOdds;
                    if (max < drawOdds) max = drawOdds;
                    if (min > homeTeamWinOdds) min = homeTeamWinOdds;
                    if (min > awayTeamWinOdds) min = awayTeamWinOdds;
                    if (min > drawOdds) min = drawOdds;
                }
            }
            return new double[2] { min, max };
        }*/

        public static Matrix<double> ConvertToMatrix(List<List<string>> lines)
        {
            int count = 0;
            Matrix<double> data = Matrix<double>.Build.Dense(lines.Count, lines[0].Count);
            foreach (List<string> line in lines)
            {
                if (line[4] == "H") line[4] = "0";
                if (line[4] == "D") line[4] = "1";
                if (line[4] == "A") line[4] = "2";
                if (line[9] == "H") line[9] = "0";
                if (line[9] == "D") line[9] = "1";
                if (line[9] == "A") line[9] = "2";
                string[] arr = line.ToArray();
                data.SetRow(count, arr.Select(double.Parse).ToArray());
                count++;
            }
            return data;
        }

        static Matrix<double> GetDataFromFile()
        {
            string[] lines = File.ReadAllLines("matches.csv");
            lines = lines.Skip(1).ToArray();
            List<List<string>> parsedLines = new List<List<string>>();
            foreach (string line in lines)
            {
                List<string> columns = line.Split(',').ToList();
                bool existsEmptyColumns = false;
                columns.RemoveAt(0);
                columns.RemoveAt(0);
                columns.RemoveAt(0);
                foreach (string column in columns)
                {
                    if(column == @"\N")
                    {
                        existsEmptyColumns = true;
                        break;
                    }
                }
                if (!existsEmptyColumns && double.Parse(columns[17]) == 0) existsEmptyColumns = true;
                if (!existsEmptyColumns)
                {
                    parsedLines.Add(columns);
                }
            }

           // Matrix<double>
            //Console.WriteLine(parsedLines[0][9]); //4 ir 9

            /*
            double[] boundaries = calculateOddsBoundaries(parsedLines);
            Console.WriteLine((boundaries[1] - boundaries[0]) / 3);

            foreach (string[] columns in parsedLines)
            {
                foreach(string column in columns)
                {
                    string matchResult = columns[7];
                    string homeTeamRanking = GetTypeOfRanking(int.Parse(columns[8]));
                    string awayTeamRanking = GetTypeOfRanking(int.Parse(columns[9]));
                    string homeTeamWinOdds = GetTypeOfOdds(double.Parse(columns[17]));
                    string awayTeamWinOdds = GetTypeOfOdds(double.Parse(columns[18]));
                    string drawOdds = GetTypeOfOdds(double.Parse(columns[19]));
                    FootballData matchData = new FootballData(matchResult, homeTeamRanking, awayTeamRanking, homeTeamWinOdds, awayTeamWinOdds, drawOdds);
                    data.Add(matchData);
                }
            }*/
            return ConvertToMatrix(parsedLines);
        }

        static Matrix<double> ReadData()
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

        public static Matrix<double> NormalizeData(Matrix<double> data)
        {
            Matrix<double> newData = Matrix<double>.Build.Dense(data.RowCount, data.ColumnCount);
            for (int i = 0; i < data.ColumnCount; i++)
            {
                Vector<double> column = data.Column(i);
                Vector<double> newColumn = Vector<double>.Build.Dense(column.Count);
                double min = column.Min();
                double max = column.Max();
                for (int j = 0; j < column.Count; j++)
                {
                    newColumn[j] = (column[j] - min) / (max - min);
                }
                newData.SetColumn(i, newColumn);
            }
            return newData;
        }

        static double Covariance(Vector<double> arr1, Vector<double> arr2)
        {
            int n = arr1.Count;
            double mean1 = arr1.Average();
            double mean2 = arr2.Average();
            double sum = 0;
            for (int i = 0; i < n; i++)
                sum = sum + (arr1[i] - mean1) * (arr2[i] - mean2);
            return sum / (n - 1);
        }

        static Matrix<double> GetCovarianceMatrix(Matrix<double> data)
        {
            Matrix<double> covarianceMatrix = Matrix<double>.Build.Dense(data.ColumnCount, data.ColumnCount);
            for(int i = 0; i < data.ColumnCount; i++)
            {
                for(int j = 0; j < data.ColumnCount; j++)
                {
                    covarianceMatrix[i, j] = Covariance(data.Column(i), data.Column(j));
                }
            }
            return covarianceMatrix;
        }


        static List<KeyValuePair<double, double>> GetSortedFullTimeResultCovariance(Matrix<double> covarienceMatrix)
        {
            List<KeyValuePair<double, double>> fullTimeResultCovariance = new List<KeyValuePair<double, double>>();
            for (int i = 0; i < covarienceMatrix.ColumnCount; i++)
            {
                fullTimeResultCovariance.Add(new KeyValuePair<double, double>(i, Math.Abs(covarienceMatrix[i, 4])));
            }
            return fullTimeResultCovariance.OrderByDescending(x => x.Value).ToList();
        }

        static Matrix<double> GetSmallerMatrix(List<KeyValuePair<double, double>> ftr, Matrix<double> data, int dimCount)
        {
            Matrix<double> newMatrix = Matrix<double>.Build.Dense(data.RowCount, dimCount);
            newMatrix.SetColumn(0, data.Column(4));
            for (int i = 0; i < dimCount - 1; i++)
            {
                newMatrix.SetColumn(i + 1, data.Column((int)ftr[i].Key));
                Console.WriteLine(ftr[i].Key);
            }
            return newMatrix;
        }

        /*
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
        }*/

        static void Main(string[] args)
        {
            int dimensions = 6;
            Matrix<double> data = ReadData();
            Console.WriteLine("Duomenys iš failo: \n" + data.ToMatrixString());
            Matrix<double> normalizedData = NormalizeData(data);
            Console.WriteLine("Normalizuoti duomenys: \n" + normalizedData.ToMatrixString());
            Matrix<double> covarienceMatrix = GetCovarianceMatrix(data);
            Console.WriteLine("Kovariacijos matrica: \n" + covarienceMatrix.ToMatrixString());
            List<KeyValuePair<double, double>> ftrSortedCovarianceList = GetSortedFullTimeResultCovariance(covarienceMatrix);
            Console.WriteLine("Full Time Result stulpelio kovariacijos reikšmės su kitais stulpeliais surikiuoti mažėjimo tvarka");
            for(int i = 0; i < ftrSortedCovarianceList.Count; i++)
            {
                Console.WriteLine("[{0}, {1}]", ftrSortedCovarianceList[i].Key, ftrSortedCovarianceList[i].Value);
            }
            Matrix<double> newData = GetSmallerMatrix(ftrSortedCovarianceList, normalizedData, dimensions);
            Console.WriteLine("Duomenys po dimencijų sumažinimo: \n" + newData.ToMatrixString());


            // Console.WriteLine("Covarience matrix: \n" + list.ToString());
            /*  for (int i = 0; i < data.Count; i++)
              {
                  Console.WriteLine(data[i].HomeTeamWinOdds);
              }
              int segmentSize = data.Count / n;*/
            /*for (int i = 0; i < 1; i++)
            {
                List<FootballData> trainingData = GetTrainingData(i, segmentSize, data);
                List<FootballData> testingData = GetTestingData(i, segmentSize, data);
                AnalyzeTrainingData(trainingData);
            }*/
        }
    }
}
