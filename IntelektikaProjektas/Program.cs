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
        const double AWAY = 1;
        const double DRAW = 0.5;
        const double HOME = 0;
        const int FTR_COLUMN = 7;
        static string DASHES = new string('-', 50);
        static List<string> ATTRIBUTE_NAMES;

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

        public static Matrix<double> ConvertToMatrix(List<List<string>> lines)
        {
            int count = 0;
            Matrix<double> data = Matrix<double>.Build.Dense(lines.Count, lines[0].Count);
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
            foreach (List<string> line in lines)
            {
                line[0] = DateTime.Parse(line[0]).Ticks.ToString();
                line[1] = competitions[line[1]];
                // Change Home, Draw and Away letters to numbers
                if (line[7] == "H") line[7] = "0";
                if (line[7] == "D") line[7] = "1";
                if (line[7] == "A") line[7] = "2";
                if (line[12] == "H") line[12] = "0";
                if (line[12] == "D") line[12] = "1";
                if (line[12] == "A") line[12] = "2";
                string[] arr = line.ToArray();
                data.SetRow(count, arr.Select(double.Parse).ToArray());
                count++;
            }
            return data;
        }

        static Matrix<double> GetDataFromFile()
        {
            string[] lines = File.ReadAllLines("matches.csv");
            ATTRIBUTE_NAMES = lines[0].Split(",").ToList();
            lines = lines.Skip(1).ToArray();
            List<List<string>> parsedLines = new List<List<string>>();
            int count = 0;
            foreach (string line in lines)
            {
                List<string> columns = line.Split(',').ToList();
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
                    parsedLines.Add(columns);
                }
            }

            return ConvertToMatrix(parsedLines);
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
                double average = column.Average();
                for (int j = 0; j < column.Count; j++)
                {
                    newColumn[j] = max - min != 0 ? (column[j] - min) / (max - min) : 0;
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
            for (int i = 0; i < data.ColumnCount; i++)
            {
                for(int j = 0; j < data.ColumnCount; j++)
                {
                    if (i == j)
                    {
                        covarianceMatrix[i, j] = 0;
                    }
                    else
                    {
                        covarianceMatrix[i, j] = Covariance(data.Column(i), data.Column(j));
                    }
                }
            }
            return covarianceMatrix;
        }

        static List<KeyValuePair<double, double>> GetSortedFullTimeResultCovariance(Matrix<double> covarienceMatrix)
        {
            List<KeyValuePair<double, double>> fullTimeResultCovariance = new List<KeyValuePair<double, double>>();
            for (int i = 0; i < covarienceMatrix.ColumnCount; i++)
            {
                fullTimeResultCovariance.Add(new KeyValuePair<double, double>(i, Math.Abs(covarienceMatrix[i, FTR_COLUMN])));
            }
            return fullTimeResultCovariance.OrderByDescending(x => x.Value).ToList();
        }

        static Matrix<double> GetReducedMatrix(List<KeyValuePair<double, double>> ftr, Matrix<double> data, int dimCount)
        {
            Matrix<double> newMatrix = Matrix<double>.Build.Dense(data.RowCount, dimCount);
            newMatrix.SetColumn(0, data.Column(FTR_COLUMN));
            for (int i = 0; i < dimCount - 1; i++)
            {
                newMatrix.SetColumn(i + 1, data.Column((int)ftr[i].Key));
            }
            return newMatrix;
        }

        public static Counts CalculateFullTimeResultsTotals(Matrix<double> data)
        {
            Counts totals = new Counts();
            for(int i = 0; i < data.RowCount; i++)
            {
                if (data[i, FTR_COLUMN] == AWAY)
                {
                    totals.awayWinCount++;
                }
                else if (data[i, FTR_COLUMN] == DRAW)
                {
                    totals.drawCount++;
                }
                else if (data[i, FTR_COLUMN] == HOME)
                {
                    totals.homeWinCount++;
                }
            }
            return totals;
        }

        static Matrix<double> GetTrainingData(Matrix<double> data, int segmentSize, int index)
        {
            Matrix<double> trainingData = Matrix<double>.Build.Dense(data.RowCount - segmentSize, data.ColumnCount);
            int counter = 0;
            for (int i = 0; i < data.RowCount; i++)
            {
                if (i < (index * segmentSize) || i >= (index * segmentSize + segmentSize))
                {
                    for (int j = 0; j < data.ColumnCount; j++)
                    {
                        trainingData[counter, j] = data[i, j];
                    }
                    counter++;
                }
            }
            return trainingData;
        }

        static Matrix<double> GetTestingData(Matrix<double> data, int segmentSize, int index)
        {
            Matrix<double> testingData = Matrix<double>.Build.Dense(segmentSize, data.ColumnCount);
            int counter = 0;
            for (int i = 0; i < data.RowCount; i++)
            {
                if (i >= (index * segmentSize) && i < (index * segmentSize + segmentSize))
                {
                    for (int j = 0; j < data.ColumnCount; j++)
                    {
                        testingData[counter, j] = data[i, j];
                    }
                    counter++;
                }
            }
            return testingData;
        }

        static void Main(string[] args)
        {
            int segmentCount = 5;
            int dimensions = 6;
            int neighborCount = 5;
            Matrix<double> data = ReadData();
            PrintAttributeNames();
            int totalDimensions = ATTRIBUTE_NAMES.Count;
            int segmentSize = data.RowCount / segmentCount; 
            Console.WriteLine("Duomenys iš failo: \n" + data.ToMatrixString());
            Matrix<double> normalizedData = NormalizeData(data);
            Console.WriteLine("Normalizuoti duomenys: \n" + normalizedData.ToMatrixString());
            Matrix<double> covarienceMatrix = GetCovarianceMatrix(normalizedData);
            Console.WriteLine("Kovariacijos matrica: \n" + covarienceMatrix.ToMatrixString());
            List<KeyValuePair<double, double>> ftrSortedCovarianceList = GetSortedFullTimeResultCovariance(covarienceMatrix);
            PrintCovarianceList(ftrSortedCovarianceList);
            Counts totals = CalculateFullTimeResultsTotals(normalizedData);
            PrintFullTimeResultsTotals(totals);
            kMeansClustering clustering = new kMeansClustering(normalizedData, 3);
            clustering.Cluster();

           /* while (dimensions <= totalDimensions)
            {*/
                Console.WriteLine("Dimensijų kiekis: {0}", dimensions);
                Matrix<double> reducedData = GetReducedMatrix(ftrSortedCovarianceList, normalizedData, dimensions);
                Console.WriteLine("Po dimensijų sumažinimo: \n", reducedData.ToMatrixString());
                Console.WriteLine("Duomenys po dimensijų sumažinimo: \n" + reducedData.ToMatrixString());
                // Kryžminė patikra
                for (int i = 0; i < 5; i++)
                {
                    Matrix<double> trainingData = GetTrainingData(reducedData, segmentSize, i);
                    Matrix<double> testingData = GetTestingData(reducedData, segmentSize, i);
                    Bayes bayes = new Bayes(trainingData);
                    bayes.Learn(i + 1);
                    bayes.TestList(testingData);
                   /* KNN kNN = new KNN(trainingData, neighborCount);
                    kNN.ClassifyList(trainingData, "Apmokymo duomenys", i + 1);
                    kNN.ClassifyList(testingData, "Testavimo duomenys");*/
                }
               /* dimensions += 6;
            }*/
        }

        static void PrintAttributeNames()
        {
            Console.WriteLine("Atributų pavadinimai:");
            for (int i = 0; i < ATTRIBUTE_NAMES.Count; i++)
            {
                Console.Write(ATTRIBUTE_NAMES[i] + " ");
            }
            Console.WriteLine("");
        }

        static void PrintCovarianceList(List<KeyValuePair<double, double>> ftrSortedCovarianceList)
        {
            Console.WriteLine("{0} stulpelio kovariacijos reikšmės lyginant su kitais stulpeliais surikiuotos mažėjimo tvarka:",
                ATTRIBUTE_NAMES[FTR_COLUMN]);
            for (int i = 0; i < ftrSortedCovarianceList.Count; i++)
            {
                Console.WriteLine("[{0}, {1}]", ATTRIBUTE_NAMES[(int)ftrSortedCovarianceList[i].Key], ftrSortedCovarianceList[i].Value);
            }
        }

        static void PrintFullTimeResultsTotals(Counts totals)
        {
            Console.WriteLine("Namų komandos pergalės: {0}", totals.homeWinCount);
            Console.WriteLine("Lygiosios: {0}", totals.drawCount);
            Console.WriteLine("Išvykos komandos pergalės: {0}", totals.awayWinCount);
        }
    }
}
