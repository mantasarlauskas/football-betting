using System;
using Accord.MachineLearning.Bayes;
using Accord.Statistics.Distributions.Univariate;
using MathNet.Numerics.LinearAlgebra;
using Accord.Statistics.Distributions.Fitting;

namespace IntelektikaProjektas
{
    class Bayes
    {
        private Matrix<double> data;
        private double[][] inputs;
        private int[] outputs;
        private NaiveBayes<NormalDistribution> bayes;
        static string DASHES = new string('-', 50);

        public Bayes(Matrix<double> _data)
        {
            data = _data;
            inputs = new double[data.RowCount][];
            outputs = new int[data.RowCount];
            InitData();
        }

        private void InitData()
        {
            for (int i = 0; i < data.RowCount; i++)
            {
                double output = data[i, 0];
                outputs[i] = output == 0 ? 0 : output == 0.5 ? 1 : 2;
                inputs[i] = new double[data.ColumnCount - 1];
                for (int j = 1; j < data.ColumnCount; j++)
                {
                    inputs[i][j - 1] = data[i, j];
                }
            }
        }

        public void Learn(int? index)
        {
            var learner = new NaiveBayesLearning<NormalDistribution>();
            learner.Options.InnerOption = new NormalOptions
            {
                Regularization = 1e-5 // to avoid zero variances
            };
            bayes = learner.Learn(inputs, outputs);
            int[] predicted = bayes.Decide(inputs);

            int correctCount = 0;
            int wrongCount = 0;
            for (int i = 0; i < outputs.Length; i++)
            {
                if (predicted[i] == outputs[i]) correctCount++;
                else wrongCount++;
            }

            Console.WriteLine(DASHES);
            Console.WriteLine("Bajeso teorema");
            if (index != null) Console.WriteLine("{0} iteracija", index);
            Console.WriteLine(DASHES);
            Console.WriteLine("Apmokymo duomenys");
            Console.WriteLine("Teisingi: {0}", correctCount);
            Console.WriteLine("Teisingi procentais: {0}%", Math.Round((double)correctCount / outputs.Length * 100, 2));
            Console.WriteLine("Neteisingi: {0}", wrongCount);
            Console.WriteLine("Neteisingi procentais: {0}%", Math.Round((double)wrongCount / outputs.Length * 100, 2));
            Console.WriteLine(DASHES);
        }

        public void TestList(Matrix<double> testData)
        {
            int correctCount = 0, wrongCount = 0;
            for (int i = 0; i < testData.RowCount; i++)
            {
                if (Test(testData.Row(i)) == true) correctCount++;
                else wrongCount++;
            }

            Console.WriteLine(DASHES);
            Console.WriteLine("Testavimo duomenys");
            Console.WriteLine("Teisingi: {0}", correctCount);
            Console.WriteLine("Teisingi procentais: {0}%", Math.Round((double)correctCount / testData.RowCount * 100, 2));
            Console.WriteLine("Neteisingi: {0}", wrongCount);
            Console.WriteLine("Neteisingi procentais: {0}%", Math.Round((double)wrongCount / testData.RowCount * 100, 2));
            Console.WriteLine(DASHES); ;
        }

        public bool Test(Vector<double> instance)
        {
            double[] instanceArray = new double[instance.Count - 1];
            for (int i = 1; i < instance.Count; i++)
                instanceArray[i - 1] = instance[i];
            int answer = bayes.Decide(instanceArray);
            int expectedResult = instance[0] == 0 ? 0 : instance[0] == 0.5 ? 1 : 2;
            return answer == expectedResult;
        }
    }
}
