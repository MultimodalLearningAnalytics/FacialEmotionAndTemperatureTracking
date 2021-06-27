namespace CreatePredictionsStore
{
    using System;
    using System.Globalization;
    using System.Linq;
    using System.Collections.Generic;
    using System.IO;
    using Microsoft.Psi;
    using Microsoft.Psi.Data;
    using Microsoft.Psi.Components;

    class Program
    {
        private const string PARTICIPANT = "participant2-01";
        private const string ARFF_FILE_NAME = "FullStoreFeatures-EmotionsAndTemp-1s-Relative-CombinedFeatures-EmotionsAndTemp-1s-Relative-Unbalanced-labeled";

        // Don't touch
        private const string ARFF_FILE_PATH = @"B:\experiment_stores\experiment3\20210601-01\" + PARTICIPANT + @"\" + ARFF_FILE_NAME + ".arff";
        private const string PSI_STORES_PATH = @"B:\experiment_stores\experiment3\20210601-01\" + PARTICIPANT;
        private const string PREDICTIONS_STORE_NAME = "EmotionsTempPredictions";

        static void Main(string[] args)
        {
            var lines = File.ReadLines(ARFF_FILE_PATH).SkipWhile(s => s != "@data").Skip(1);
            IEnumerator<(DateTime, int)> predictions = lines.Select(line => {
                var csvItems = line.Split(',');
                DateTime timestamp = DateTime.Parse(csvItems.First());
                string predictedClass = csvItems.Last();
                int prediction = predictedClass switch
                {
                    "attentive" => 0,
                    "inattentive" => 1,
                    string u => throw new Exception($"Unknown class {u}"),
                };
                return (timestamp, prediction);
            }).GetEnumerator();

            using (var p = Pipeline.Create())
            {
                var store = PsiStore.Create(p, PREDICTIONS_STORE_NAME, PSI_STORES_PATH);
                var generator = new GeneratorGazeBlinkPrediction(p, predictions);
                generator.OutGazeBlinkPrediction.Write("EmotionsTempPredictions_Inattentive", store);

                p.Run();
            }
        }
    }

    public class GeneratorGazeBlinkPrediction : Generator
    {
        private IEnumerator<(DateTime timestamp, int prediction)> predictions;
        public Emitter<int> OutGazeBlinkPrediction { get; }

        public GeneratorGazeBlinkPrediction(Pipeline p, IEnumerator<(DateTime timestamp, int prediction)> predictions)
            : base(p)
        {
            this.OutGazeBlinkPrediction = p.CreateEmitter<int>(this, nameof(this.OutGazeBlinkPrediction));

            this.predictions = predictions;
        }

        protected override DateTime GenerateNext(DateTime currentTime)
        {
            if (!this.predictions.MoveNext())
            {
                Console.WriteLine("No more data.");
                return currentTime; // no more data
            }

            (DateTime timestamp, int prediction) = this.predictions.Current;

            this.OutGazeBlinkPrediction.Post(prediction, timestamp);
            Console.WriteLine(timestamp.ToString("o", CultureInfo.InvariantCulture) + ": " + prediction);

            return timestamp;
        }
    }
}
