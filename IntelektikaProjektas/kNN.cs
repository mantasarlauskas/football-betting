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
    public class KNN
    {
        static string DASHES = new string('-', 50);
        private int k = 0;
        private Matrix<double> data;
        private const double HOME = 0;
        private const double DRAW = 0.5;
        private const double AWAY = 1;

        public KNN(Matrix<double> _data, int _neighbourCount)
        {
            k = _neighbourCount;
            data = _data;
        }

        public void ClassifyList(Matrix<double> instances, string type, int? index = null)
        {
            Console.WriteLine(DASHES);
            if (index != null)
            {
                Console.WriteLine("kNN");
                Console.WriteLine("{0} Iteracija.", index);
                Console.WriteLine(DASHES);
            }

            Console.WriteLine(type);
            int correctCount = 0;
            int wrongCount = 0;
            for (int j = 0; j < instances.RowCount; j++)
            {
                if (Classify(instances.Row(j)) == true)
                {
                    correctCount++;
                }
                else
                {
                    wrongCount++;
                }
            }

            Console.WriteLine("Teisingi: {0}", correctCount);
            Console.WriteLine("Teisingi procentais: {0}%", Math.Round((double)correctCount / instances.RowCount * 100, 2));
            Console.WriteLine("Neteisingi: {0}", wrongCount);
            Console.WriteLine("Neteisingi procentais: {0}%", Math.Round((double)wrongCount / instances.RowCount * 100, 2));
            Console.WriteLine(DASHES);
        }

        public bool Classify(Vector<double> instance)
        {
            double expectedResult = instance[0];
            double[] distances = new double[data.RowCount];
            double[] classes = new double[data.RowCount];
            List<double> kDistances = new List<double>();
            List<double> kClasses = new List<double>();

            for (int i = 0; i < data.RowCount; i++)
            {
                double sum = 0;
                for (int j = 1; j < data.ColumnCount; j++)
                {
                    sum += Math.Pow(data[i, j] - instance[j], 2);
                }
                distances[i] = Math.Sqrt(sum);
                classes[i] = data[i, 0];
            }

            Array.Sort(distances, classes);
            kDistances = distances.Take(k).ToList();
            kClasses = classes.Take(k).ToList();
            return GetResult(kClasses) == expectedResult;
        }

        private double GetResult(List<double> kClasses)
        {
            int homeCount = 0;
            int drawCount = 0;
            int awayCount = 0;

            for (int i = 0; i < kClasses.Count; i++)
            {
                if (kClasses[i] == HOME)
                {
                    homeCount++;
                }
                else if (kClasses[i] == DRAW)
                {
                    drawCount++;
                }
                else
                {
                    awayCount++;
                }
            }

            if (awayCount > drawCount && awayCount > homeCount)
            {
                return AWAY;
            }
            else if (drawCount > homeCount)
            {
                return DRAW;
            }
            else
            {
                return HOME;
            }
        }
    }
}