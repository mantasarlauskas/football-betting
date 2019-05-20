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

        public static Matrix<double> ConvertToMatrix(List<List<string>> lines)
        {
            int count = 0;
            Matrix<double> data = Matrix<double>.Build.Dense(lines.Count, lines[0].Count);
            foreach (List<string> line in lines)
            {
                if (line[6] == "H") line[6] = "0";
                if (line[6] == "D") line[6] = "1";
                if (line[6] == "A") line[6] = "2";
                if (line[11] == "H") line[11] = "0";
                if (line[11] == "D") line[11] = "1";
                if (line[11] == "A") line[11] = "2";
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
            Dictionary<string, string> competitions = new Dictionary<string, string>()
            {
                { "B1", "0" },
                { "D1", "1" },
                { "D2", "2" },
                { "E0", "3" },
                { "E1", "4" },
                { "E2", "5" },
                { "F1", "6" },
                { "F2", "7" },
                { "I1", "8" },
                { "I2", "9" },
                { "N1", "10" },
                { "P1", "11" },
                { "SC0", "12" },
                { "SC1", "13" },
                { "SP1", "14" },
                { "T1", "15" },
            };

            foreach (string line in lines)
            {
                List<string> columns = line.Split(',').ToList();
                bool existsEmptyColumns = false;
                columns.RemoveAt(0);
                foreach (string column in columns)
                {
                    if(column == @"\N")
                    {
                        existsEmptyColumns = true;
                        break;
                    }
                }
                if (!existsEmptyColumns && double.Parse(columns[16]) == 0) existsEmptyColumns = true;
                if (!existsEmptyColumns)
                {
                    parsedLines.Add(columns);
                }
            }

            foreach (List<string> line in parsedLines)
            {
                line[0] = competitions[line[0]];
            }

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
                fullTimeResultCovariance.Add(new KeyValuePair<double, double>(i, Math.Abs(covarienceMatrix[i, 6])));
            }
            return fullTimeResultCovariance.OrderByDescending(x => x.Value).ToList();
        }

        static Matrix<double> GetSmallerMatrix(List<KeyValuePair<double, double>> ftr, Matrix<double> data, int dimCount)
        {
            Matrix<double> newMatrix = Matrix<double>.Build.Dense(data.RowCount, dimCount);
            newMatrix.SetColumn(0, data.Column(6));
            for (int i = 0; i < dimCount - 1; i++)
            {
                newMatrix.SetColumn(i + 1, data.Column((int)ftr[i].Key));
            }
            return newMatrix;
        }

        static double[] CalculateBoundaries(Vector<double> arr)
        {
            double max = 0;
            double min = arr[0];
            for (int i = 0; i < arr.Count; i++)
            {
                if (arr[i] > max) max = arr[i];
                if (arr[i] < min) min = arr[i];
            }
            double intervalSize = (max - min) / 3;
            return new double[2] { min + intervalSize, max - intervalSize};
        }

        static string[,] PrepareDataForBayes(Matrix <double> data)
        {
            string[,] bayesData = new string[data.RowCount, data.ColumnCount];
            for (int i = 0; i < data.ColumnCount; i++)
            {
                double[] boundaries = CalculateBoundaries(data.Column(i));
                for (int j = 0; j < data.RowCount; j++)
                {
                    if (data[j, i] >= boundaries[1]) bayesData[j, i] = "High";
                    else if (data[j, i] <= boundaries[0]) bayesData[j, i] = "Low";
                    else bayesData[j, i] = "Medium";
                }
            }
            return bayesData;
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
            string[,] bayesData = PrepareDataForBayes(newData);
            for (int i = 0; i < newData.RowCount; i++)
            {
                for (int j = 0; j < newData.ColumnCount; j++)
                    Console.Write(bayesData[i, j] + " ");
                Console.WriteLine("");
            }

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
