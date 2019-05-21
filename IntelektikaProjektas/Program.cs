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
        const string HIGH = "High";
        const string MEDIUM = "Medium";
        const string LOW = "Low";
        const string TOTAL = "TOTAL";
        const int FTR_COLUMN = 6;
        static string DASHES = new string('-', 50);

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
            for (int i = 0; i < data.ColumnCount; i++)
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

        static string[,] BinData(Matrix <double> data)
        {
            string[,] binnedData = new string[data.RowCount, data.ColumnCount];
            for (int i = 0; i < data.ColumnCount; i++)
            {
                double[] boundaries = CalculateBoundaries(data.Column(i));
                for (int j = 0; j < data.RowCount; j++)
                {
                    if (data[j, i] >= boundaries[1]) binnedData[j, i] = HIGH;
                    else if (data[j, i] <= boundaries[0]) binnedData[j, i] = LOW;
                    else binnedData[j, i] = MEDIUM;
                }
            }
            return binnedData;
        }

        static Counts CalculateFullTimeResultsTotals(string[,] binnedData)
        {
            Counts totals = new Counts();
            for(int i = 0; i < binnedData.GetLength(0); i++)
            {
                if (binnedData[i, 0] == HIGH)
                {
                    totals.highCount++;
                }
                else if (binnedData[i, 0] == MEDIUM)
                {
                    totals.mediumCount++;
                }
                else
                {
                    totals.lowCount++;
                }
            }
            return totals;
        }

        static Dictionary<int, List<Counts>> CalculateJointCounts(string[,] binnedData, Counts ftrTotals)
        {
            Dictionary<int, List<Counts>> jointCounts = new Dictionary<int, List<Counts>>();
            string[] values = new string[4] { LOW, MEDIUM, HIGH, TOTAL};
            for (int i = 1; i < binnedData.GetLength(1); i++)
            {
                List<Counts> columnCounts = new List<Counts>(); 
                for (int j = 0; j < values.Length; j++)
                {
                    Counts counts = new Counts();
                    for (int k = 0; k < binnedData.GetLength(0); k++)
                    {
                        string value = binnedData[k, i];
                        string ftrValue = binnedData[k, 0];
                        if (value == values[0] && (ftrValue == values[j] || values[j] == TOTAL))
                        {
                            counts.highCount++;
                        }
                        else if (value == values[1] && (ftrValue == values[j] || values[j] == TOTAL))
                        {
                            counts.mediumCount++;
                        }
                        else if (value == values[2] && (ftrValue == values[j] || values[j] == TOTAL))
                        {
                            counts.lowCount++;
                        }
                    }
                    double totalCount = values[j] == LOW ? ftrTotals.lowCount : 
                        values[j] == HIGH ? ftrTotals.highCount :
                        values[j] == MEDIUM ? ftrTotals.mediumCount : binnedData.GetLength(0);
                    counts.lowCountProbability = counts.lowCount / totalCount;
                    counts.mediumCountProbability = counts.mediumCount / totalCount;
                    counts.highCountProbability = counts.highCount / totalCount;
                    columnCounts.Add(counts);
                }
                jointCounts.Add(i, columnCounts);
            }
            return jointCounts;
        }

        static string ParseResult(double awayTeamWinProbability, double drawProbability, double homeTeamWinprobability)
        {
            if (awayTeamWinProbability > drawProbability && awayTeamWinProbability > homeTeamWinprobability)
            {
                return HIGH;
            }
            else if (drawProbability > homeTeamWinprobability)
            {
                return MEDIUM;
            }
            else
            {
                return LOW;
            }
        }

        static void AnalyzeData(string[,] binnedData, Dictionary<int, List<Counts>> jointCounts, Counts ftrTotals, string type)
        {
            string[] values = new string[3] { LOW, MEDIUM, HIGH };
            int correctCount = 0;
            int wrongCount = 0;
            for (int j = 0; j < binnedData.GetLength(0); j++)
            {
                double homeTeamWinprobability = 1;
                double drawProbability = 1;
                double awayTeamWinProbability = 1;
                double evidence = 1;
                for (int i = 1; i < binnedData.GetLength(1); i++)
                {
                    string value = binnedData[j, i];
                    List<Counts> columnCounts = jointCounts[i];
                    if (value == values[0])
                    {
                        homeTeamWinprobability *= columnCounts[0].lowCountProbability;
                        drawProbability *= columnCounts[1].lowCountProbability;
                        awayTeamWinProbability *= columnCounts[2].lowCountProbability;
                        evidence *= columnCounts[3].lowCountProbability;

                    }
                    else if (value == values[1])
                    {
                        homeTeamWinprobability *= columnCounts[0].mediumCountProbability;
                        drawProbability *= columnCounts[1].mediumCountProbability;
                        awayTeamWinProbability *= columnCounts[2].mediumCountProbability;
                        evidence *= columnCounts[3].mediumCountProbability;
                    }
                    else
                    {
                        homeTeamWinprobability *= columnCounts[0].highCountProbability;
                        drawProbability *= columnCounts[1].highCountProbability;
                        awayTeamWinProbability *= columnCounts[2].highCountProbability;
                        evidence *= columnCounts[3].highCountProbability;
                    }
                }
                homeTeamWinprobability *= (double)ftrTotals.lowCount / binnedData.GetLength(0);
                drawProbability *= (double)ftrTotals.mediumCount / binnedData.GetLength(0);
                awayTeamWinProbability *= (double)ftrTotals.highCount / binnedData.GetLength(0);
               // homeTeamWinprobability /= evidence;
               // drawProbability /= evidence;
               // awayTeamWinProbability /= evidence;
                string result = ParseResult(awayTeamWinProbability, drawProbability, homeTeamWinprobability);
                if (binnedData[j, 0] == result)
                {
                    correctCount++;
                }
                else
                {
                    wrongCount++;
                }
            }
            Console.WriteLine(type);
            Console.WriteLine("Teisingų: {0}", correctCount);
            Console.WriteLine("Teisingų procentais: {0}%", Math.Round((double)correctCount / binnedData.GetLength(0) * 100, 2));
            Console.WriteLine("Neteisingų: {0}", wrongCount);
            Console.WriteLine("Neteisingų procentais: {0}%", Math.Round((double)wrongCount / binnedData.GetLength(0) * 100, 2));
            Console.WriteLine("Iš viso: {0}", binnedData.GetLength(0));
        }

        static void PrintCovarianceList(List<KeyValuePair<double, double>> ftrSortedCovarianceList)
        {
            Console.WriteLine("Full Time Result stulpelio kovariacijos reikšmės lyginant su kitais stulpeliais surikiuotos mažėjimo tvarka");
            for (int i = 0; i < ftrSortedCovarianceList.Count; i++)
            {
                Console.WriteLine("[{0}, {1}]", ftrSortedCovarianceList[i].Key, ftrSortedCovarianceList[i].Value);
            }
        }

        static void PrintBinnedData(string[,] binnedData)
        {
            Console.WriteLine("Pakeistų duomenų pavyzdys:");
            for (int j = 0; j < 3; j++)
            {
                for (int i = 0; i < binnedData.GetLength(1); i++)
                    Console.Write(binnedData[j, i] + " ");
                Console.WriteLine("");
            }
        }
        
        static void PrintJointCounts(Dictionary<int, List<Counts>> jointCounts)
        {
            foreach (var item in jointCounts)
            {
                Console.WriteLine(DASHES);
                Console.WriteLine("Stulpelio numeris: " + item.Key);
                Console.WriteLine(DASHES);
                int counter = 0;
                foreach (var counts in item.Value)
                {
                    string ftr;
                    if (counter == 0)
                    {
                        ftr = "Namų komanda laimėjo";
                    }
                    else if (counter == 1)
                    {
                        ftr = "Lygiosios";
                    }
                    else
                    {
                        ftr = "Išvykos komanda laimėjo";
                    }
                    Console.WriteLine("Įvyko rezultatas: " + ftr);
                    Console.WriteLine("Stulpelių kiekiai įvykus šiam rezultatui:");
                    Console.WriteLine("Low: {0}, Medium: {1}, High: {2}", counts.lowCount, counts.mediumCount, counts.highCount);
                    Console.WriteLine("Tikimybės:");
                    Console.WriteLine("Low: {0}, Medium: {1}, High: {2}", Math.Round(counts.lowCountProbability, 4),
                        Math.Round(counts.mediumCountProbability, 4), Math.Round(counts.highCountProbability, 4));
                    counter++;
                    Console.WriteLine(DASHES);
                }
                Console.WriteLine(DASHES);
            }
        }

        static string[,] GetTrainingData(string[,] data, int segmentSize, int index)
        {
            string[,] trainingData = new string[data.GetLength(0) - segmentSize, data.GetLength(1)];
            int counter = 0;
            for (int i = 0; i < data.GetLength(0); i++)
            {
                if (i < (index * segmentSize) || i >= (index * segmentSize + segmentSize))
                {
                    for (int j = 0; j < data.GetLength(1); j++)
                    {
                        trainingData[counter, j] = data[i, j];
                    }
                    counter++;
                }
            }
            return trainingData;
        }

        static string[,] GetTestingData(string[,] data, int segmentSize, int index)
        {
            string[,] testingData = new string[segmentSize, data.GetLength(1)];
            int counter = 0;
            for (int i = 0; i < data.GetLength(0); i++)
            {
                if (i >= (index * segmentSize) && i < (index * segmentSize + segmentSize))
                {
                    for (int j = 0; j < data.GetLength(1); j++)
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
            int segmentCount = 10;
            int dimensions = 5;
            Matrix<double> data = ReadData();
            int segmentSize = data.RowCount / segmentCount; 
            Console.WriteLine("Duomenys iš failo: \n" + data.ToMatrixString());
            Matrix<double> normalizedData = NormalizeData(data);
            Console.WriteLine("Normalizuoti duomenys: \n" + normalizedData.ToMatrixString());
            Matrix<double> covarienceMatrix = GetCovarianceMatrix(data);
            Console.WriteLine("Kovariacijos matrica: \n" + covarienceMatrix.ToMatrixString());
            List<KeyValuePair<double, double>> ftrSortedCovarianceList = GetSortedFullTimeResultCovariance(covarienceMatrix);
            PrintCovarianceList(ftrSortedCovarianceList);
            Matrix<double> newData = GetReducedMatrix(ftrSortedCovarianceList, normalizedData, dimensions);
            Console.WriteLine("Duomenys po dimencijų sumažinimo: \n" + newData.ToMatrixString());
            string[,] binnedData = BinData(newData);
            PrintBinnedData(binnedData);
            for (int i = 0; i < segmentCount; i++)
            {
                string[,] trainingData = GetTrainingData(binnedData, segmentSize, i);
                string[,] testingData = GetTestingData(binnedData, segmentSize, i);
                Counts ftrTotals = CalculateFullTimeResultsTotals(trainingData);
                Console.WriteLine("Namų komandų pergalės: {0}, Lygiosios: {1}, Išvykos komandų pergalės: {2}",
                    ftrTotals.lowCount, ftrTotals.mediumCount, ftrTotals.highCount);
                Dictionary<int, List<Counts>> jointCounts = CalculateJointCounts(trainingData, ftrTotals);
                //  PrintJointCounts(jointCounts);
                Console.WriteLine("{0}\nIteracija nr. {1}\n{2}", DASHES, i + 1, DASHES);
                AnalyzeData(trainingData, jointCounts, ftrTotals, "Mokymosi duomenys");
                Console.WriteLine(DASHES);
                AnalyzeData(testingData, jointCounts, ftrTotals, "Testavimo duomenys");
                Console.WriteLine(DASHES);
            } 
        }
    }
}
