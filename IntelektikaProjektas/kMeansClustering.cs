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
    class kMeansClustering
    {
        private Matrix<double> data;
        private int numClusters;
        private int[] clustering;
        private double[,] means;

        public kMeansClustering(Matrix<double> _data, int _numClusters)
        {
            data = _data;
            numClusters = _numClusters;
            clustering = new int[data.RowCount];
            means = new double[numClusters, data.ColumnCount];
        }

        public void Cluster()
        {
            bool changed = true;
            bool success = true;
            InitClustering();
            int maxCount = data.RowCount * 10;
            int count = 0;
            while (changed == true && success == true && count < maxCount)
            {
                count++;
                success = UpdateMeans();
                changed = UpdateClustering();
            }
            int homeCount = 0;
            int awayCount = 0;
            int drawCount = 0;
            for (int i = 0; i < clustering.Length; i++)
            {
                if (clustering[i] == 0) homeCount++;
                else if (clustering[i] == 1) drawCount++;
                else awayCount++;
                
            }
            Console.WriteLine(homeCount);
            Console.WriteLine(drawCount);
            Console.WriteLine(awayCount);

        }

        private void InitClustering()
        {
            Random rand = new Random(1);
            for (int i = 0; i < numClusters; i++)
            {
                clustering[i] = i;
            }
            for (int i = numClusters; i < clustering.Length; i++)
            {
                clustering[i] = rand.Next(0, numClusters);
            }
        }

        private bool UpdateMeans()
        {
            int[] clusterCounts = new int[numClusters];
            for (int i = 0; i < data.RowCount; i++)
            {
                clusterCounts[clustering[i]]++;
            }

            for (int i = 0; i < numClusters; i++)
            {
                if (clusterCounts[i] == 0)
                    return false;
            }

            for (int i = 0; i < means.GetLength(0); i++)
                for (int j = 0; j < means.GetLength(1); j++)
                    means[i, j] = 0.0;
            
            for (int i = 0; i < data.RowCount; i++)
            {
                int cluster = clustering[i];
                for (int j = 0; j < data.ColumnCount; j++)
                {
                    means[cluster, j] += data[i, j];
                }
            }

            for (int i = 0; i < means.GetLength(0); i++)
            {
                for (int j = 0; j < means.GetLength(1); j++)
                {
                    means[i, j] /= clusterCounts[i];
                }
            }

            return true;
        }

        private bool UpdateClustering()
        {
            bool changed = false;
            int[] newClustering = new int[clustering.Length];
            Array.Copy(clustering, newClustering, clustering.Length);

            double[] distances = new double[numClusters];

            for (int i = 0; i < data.RowCount; i++)
            {
                for (int j = 0; j < numClusters; j++)
                {
                    distances[j] = Distance(data.Row(i), j);
                }
                int newCluster = MinIndex(distances);
                if (newCluster != newClustering[i])
                {
                    changed = true;
                    newClustering[i] = newCluster;
                }
            }

            if (changed == false) return false;

            int[] clusterCounts = new int[numClusters];
            for (int i = 0; i < data.RowCount; i++)
            {
                clusterCounts[newClustering[i]]++;
            }

            for (int i = 0; i < numClusters; i++)
            {
                if (clusterCounts[i] == 0)
                    return false;
            }

            Array.Copy(newClustering, clustering, newClustering.Length);
            return true;
        }

        private double Distance(Vector<double> tuple, int index)
        {
            double sum = 0.0;
            for (int i = 0; i < tuple.Count; i++)
            {
                sum += Math.Pow(tuple[i] - means[index, i], 2);
            }
            return Math.Sqrt(sum);
        }

        private int MinIndex(double[] distances)
        {
            int index = 0;
            double dist = distances[0];
            for (int i = 0; i < distances.Length; i++)
            {
                if (distances[i] < dist)
                {
                    dist = distances[i];
                    index = i;
                }
            }
            return index;
        }
    }
}
