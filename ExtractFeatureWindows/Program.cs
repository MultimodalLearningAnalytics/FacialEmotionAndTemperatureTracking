namespace ExtractFeatureWindows
{
    using System;
    using System.Linq;
    using System.Collections.Generic;
    using System.IO;
    using Microsoft.Psi;
    using System.Globalization;

    public class EmotionsOrTemperatureMode
    {
        private EmotionsOrTemperatureMode(string value) { Value = value; }

        public string Value { get; private set; }

        public static EmotionsOrTemperatureMode EmotionsAndTemp { get { return new EmotionsOrTemperatureMode("EmotionsAndTemp"); } }
        public static EmotionsOrTemperatureMode EmotionsOnly { get { return new EmotionsOrTemperatureMode("EmotionsOnly"); } }
        public static EmotionsOrTemperatureMode TempOnly { get { return new EmotionsOrTemperatureMode("TempOnly"); } }

        public override bool Equals(object obj)
        {
            if ((obj == null) || !this.GetType().Equals(obj.GetType()))
            {
                return false;
            }
            else
            {
                EmotionsOrTemperatureMode m = (EmotionsOrTemperatureMode)obj;
                return m.Value.Equals(this.Value);
            }
        }
    }

    class Program
    {
        public static readonly int[] WINDOW_SIZE_SECONDS_OPTIONS = new int[] { 1 };
        public static readonly int[] PARTICIPANT_ID_OPTIONS = new int[] { 1, 2, 3 };
        private static int WINDOW_SIZE_SECONDS;
        private static string PARTICIPANT;

        private const string EXPERIMENT = "fullFeatures";
        private static string PSI_STORES_PATH;
        private const string OUTPUT_SUB_FOLDER = "_RelativeResultCSVs";
        private static readonly EmotionsOrTemperatureMode MODE = EmotionsOrTemperatureMode.EmotionsAndTemp;

        private const string DETECTED_FACES_STORE_NAME = "DetectedFacesData";
        private const string TEMPERATURE_STORE_NAME = "TemperatureFilteredData";
        private const int EXP2_NUMBER_OF_STORES = 12;
        private const string EXP3_DISTRACTION_STORE_NAME = "Distraction";

        static void Main(string[] args)
        {
            if (EXPERIMENT == "fullFeatures")
            {
                foreach (int wSize in WINDOW_SIZE_SECONDS_OPTIONS)
                {
                    WINDOW_SIZE_SECONDS = wSize;
                    
                    foreach (int pId in PARTICIPANT_ID_OPTIONS)
                    {
                        PSI_STORES_PATH = @"B:\experiment_stores\experiment3\20210601-01\" + $"participant{pId}-01";
                        FullFeaturesExtraction();
                    }
                }
            } else if (EXPERIMENT == "allEx3")
            {
                foreach (int wSize in WINDOW_SIZE_SECONDS_OPTIONS)
                {
                    WINDOW_SIZE_SECONDS = wSize;

                    foreach (int pId in PARTICIPANT_ID_OPTIONS)
                    {
                        PSI_STORES_PATH = @"B:\experiment_stores\experiment3\20210601-01\" + $"participant{pId}-01";
                        ExtractAttentiveInattentiveExperiment3();
                    }
                }
            } else
            {
                foreach (int wSize in WINDOW_SIZE_SECONDS_OPTIONS)
                {
                    WINDOW_SIZE_SECONDS = wSize;

                    foreach (int pId in PARTICIPANT_ID_OPTIONS)
                    {
                        PARTICIPANT = $"participant{pId}-01";
                        PSI_STORES_PATH = @"B:\experiment_stores\" + EXPERIMENT + @"\20210601-01\" + PARTICIPANT;

                        Console.WriteLine("\n===================================================================");
                        Console.WriteLine($"Running {EXPERIMENT} on {PARTICIPANT} with window size {WINDOW_SIZE_SECONDS}");

                        if (EXPERIMENT == "experiment2")
                            Experiment2Extraction();
                        else if (EXPERIMENT == "experiment3")
                            Experiment3Extraction();
                    }
                }
            }
        }

        static private void Experiment2Extraction()
        {
            string attentiveCsvPath = Path.Combine(PSI_STORES_PATH, OUTPUT_SUB_FOLDER, $"AttentiveFeatures-{MODE.Value}-{WINDOW_SIZE_SECONDS}s.csv");
            
            if(File.Exists(attentiveCsvPath))
            {
                Console.WriteLine("WARNING: csv will be overwritten");
                Console.WriteLine(attentiveCsvPath);
                
                ConsoleKey response;
                do
                {
                    Console.Write("\nDo you want to override or skip? [yn] ");
                    response = Console.ReadKey(false).Key;
                    if (response != ConsoleKey.Enter) Console.WriteLine();

                } while (response != ConsoleKey.Y && response != ConsoleKey.N);

                if (response != ConsoleKey.Y) return;
            }

            List<string> attentiveCsv = new();

            for (int i = 0; i < EXP2_NUMBER_OF_STORES; i++)
            {
                if (PARTICIPANT == "participant1-01" && i == 0) continue;
                using (var p = Pipeline.Create())
                {
                    var detectedFacesStore = PsiStore.Open(p, DETECTED_FACES_STORE_NAME, Path.Combine(PSI_STORES_PATH, $"{DETECTED_FACES_STORE_NAME}.{i.ToString("D4")}"));
                    var temperatureStore = PsiStore.Open(p, TEMPERATURE_STORE_NAME, Path.Combine(PSI_STORES_PATH, $"{TEMPERATURE_STORE_NAME}.{i.ToString("D4")}"));

                    IProducer<double> angryStream = detectedFacesStore.OpenStream<double>("angry");
                    IProducer<double> disgustStream = detectedFacesStore.OpenStream<double>("disgust");
                    IProducer<double> fearStream = detectedFacesStore.OpenStream<double>("fear");
                    IProducer<double> happyStream = detectedFacesStore.OpenStream<double>("happy");
                    IProducer<double> neutralStream = detectedFacesStore.OpenStream<double>("neutral");
                    IProducer<double> sadStream = detectedFacesStore.OpenStream<double>("sad");
                    IProducer<double> surpriseStream = detectedFacesStore.OpenStream<double>("surprise");
                    IProducer<double> temperatureStream = temperatureStore.OpenStream<double>("Participant temperature (filtered)");

                    /*IProducer<(
                        double angry,
                        double disgust,
                        double fear,
                        double happy,
                        double neutral,
                        double sad,
                        double surprise
                    )> emotionsStream =
                        angryStream
                        .Join(disgustStream)
                        .Join(fearStream)
                        .Join(happyStream)
                        .Join(neutralStream)
                        .Join(sadStream)
                        .Join(surpriseStream);*/

                    IProducer<((
                        double angry,
                        double disgust,
                        double fear,
                        double happy,
                        double neutral,
                        double sad,
                        double surprise
                    ) emotions, double participantTemperature)> emotionsAndTemperatureStream =
                        angryStream
                        .Join(disgustStream)
                        .Join(fearStream)
                        .Join(happyStream)
                        .Join(neutralStream)
                        .Join(sadStream)
                        .Join(surpriseStream)
                        .Join(temperatureStream, Reproducible.Nearest<double>(RelativeTimeInterval.Infinite));

                    List<double> angryValues = new();
                    List<double> disgustValues = new();
                    List<double> fearValues = new();
                    List<double> happyValues = new();
                    List<double> neutralValues = new();
                    List<double> sadValues = new();
                    List<double> surpriseValues = new();
                    List<double> participantTempValues = new();

                    DateTime? windowStart = null;

                    emotionsAndTemperatureStream.Do((values, e) => {
                        if (!windowStart.HasValue) // Initialize windowStart
                            windowStart = e.OriginatingTime;

                        if (e.OriginatingTime > windowStart.Value.AddSeconds(WINDOW_SIZE_SECONDS))
                        {
                            Console.WriteLine($"Calculating features for {windowStart} to {e.OriginatingTime}");

                            string statisticalFeatures = ExtractFeatures.GetStatisticalFeatures(
                                angryValues,
                                disgustValues,
                                fearValues,
                                happyValues,
                                neutralValues,
                                sadValues,
                                surpriseValues,
                                participantTempValues,
                                MODE
                            );
                            
                            attentiveCsv.Add(statisticalFeatures);

                            angryValues = new();
                            disgustValues = new();
                            fearValues = new();
                            happyValues = new();
                            neutralValues = new();
                            sadValues = new();
                            surpriseValues = new();
                            participantTempValues = new();

                            windowStart = e.OriginatingTime;
                        }
                        else
                        {
                            angryValues.Add(values.emotions.angry);
                            disgustValues.Add(values.emotions.disgust);
                            fearValues.Add(values.emotions.fear);
                            happyValues.Add(values.emotions.happy);
                            neutralValues.Add(values.emotions.neutral);
                            sadValues.Add(values.emotions.sad);
                            surpriseValues.Add(values.emotions.surprise);
                            participantTempValues.Add(values.participantTemperature);
                        }
                    });

                    p.Run(ReplayDescriptor.ReplayAll);
                }
            }

            string csvHeader = ExtractFeatures.CsvFeaturesHeaders() + ",class";

            File.WriteAllLines(attentiveCsvPath, attentiveCsv
                .Select(s => s + ",attentive")
                .Prepend(csvHeader)
                .ToList());
        }

        static private void Experiment3Extraction()
        {
            string distractedCsvPath = Path.Combine(PSI_STORES_PATH, OUTPUT_SUB_FOLDER, $"DistractionFeatures-{MODE.Value}-{WINDOW_SIZE_SECONDS}s.csv");
            string inattentiveCsvPath = Path.Combine(PSI_STORES_PATH, OUTPUT_SUB_FOLDER, $"InattentiveFeatures-{MODE.Value}-{WINDOW_SIZE_SECONDS}s.csv");
            string combinedCsvPath = Path.Combine(PSI_STORES_PATH, OUTPUT_SUB_FOLDER, $"CombinedFeatures-{MODE.Value}-{WINDOW_SIZE_SECONDS}s.csv");

            if (File.Exists(distractedCsvPath) || File.Exists(inattentiveCsvPath) || File.Exists(combinedCsvPath))
            {
                Console.WriteLine("WARNING: One or more CSV's  will be overridden");
                Console.WriteLine(distractedCsvPath);
                Console.WriteLine(inattentiveCsvPath);
                Console.WriteLine(combinedCsvPath);

                ConsoleKey response;
                do
                {
                    Console.Write("\nDo you want to overwrite? [yn] ");
                    response = Console.ReadKey(false).Key;
                    if (response != ConsoleKey.Enter) Console.WriteLine();

                } while (response != ConsoleKey.Y && response != ConsoleKey.N);

                if (response != ConsoleKey.Y) return;
            }

            List<DateTime> distractionTimes = new();
            List<DateTime> inattentiveTimes = new();

            using (var p = Pipeline.Create())
            {
                var distractionStore = PsiStore.Open(p, EXP3_DISTRACTION_STORE_NAME, PSI_STORES_PATH);

                var distractionStream = distractionStore.OpenStream<string>("Distraction");
                var inattentiveStream = distractionStore.OpenStream<string>("Inattentive");

                distractionStream.Do((d, e) => distractionTimes.Add(e.OriginatingTime));
                inattentiveStream.Do((i, e) => inattentiveTimes.Add(e.OriginatingTime));

                p.Run(ReplayDescriptor.ReplayAll);
            }

            var distractionCsv = distractionTimes.Select(time => ExtractFeaturesWindow(time, WINDOW_SIZE_SECONDS));
            var inattentiveCsv = inattentiveTimes.Select(time => ExtractFeaturesWindow(time, WINDOW_SIZE_SECONDS));
            var combinedCsv = distractionCsv.Concat(inattentiveCsv);

            string csvHeader = ExtractFeatures.CsvFeaturesHeaders() + ",class";
            File.WriteAllLines(distractedCsvPath, distractionCsv
                .Select(s => s + ",distracted")
                .Prepend(csvHeader)
                .ToList());
            File.WriteAllLines(inattentiveCsvPath, inattentiveCsv
                .Select(s => s + ",inattentive")
                .Prepend(csvHeader)
                .ToList());
            File.WriteAllLines(combinedCsvPath, combinedCsv
                .Select(s => s + ",inattentive")
                .Prepend(csvHeader)
                .ToList());
        }

        static private string ExtractFeaturesWindow(DateTime time, int windowSize)
        {
            using var p = Pipeline.Create();
            var detectedFacesStore = PsiStore.Open(p, DETECTED_FACES_STORE_NAME, Path.Combine(PSI_STORES_PATH, $"{DETECTED_FACES_STORE_NAME}.0000"));
            var temperatureStore = PsiStore.Open(p, TEMPERATURE_STORE_NAME, Path.Combine(PSI_STORES_PATH, $"{TEMPERATURE_STORE_NAME}.0000"));

            IProducer<double> angryStream = detectedFacesStore.OpenStream<double>("angry");
            IProducer<double> disgustStream = detectedFacesStore.OpenStream<double>("disgust");
            IProducer<double> fearStream = detectedFacesStore.OpenStream<double>("fear");
            IProducer<double> happyStream = detectedFacesStore.OpenStream<double>("happy");
            IProducer<double> neutralStream = detectedFacesStore.OpenStream<double>("neutral");
            IProducer<double> sadStream = detectedFacesStore.OpenStream<double>("sad");
            IProducer<double> surpriseStream = detectedFacesStore.OpenStream<double>("surprise");
            IProducer<double> temperatureStream = temperatureStore.OpenStream<double>("Participant temperature (filtered)");

            /*IProducer<(
                double angry,
                double disgust,
                double fear,
                double happy,
                double neutral,
                double sad,
                double surprise
            )> emotionsStream =
                angryStream
                .Join(disgustStream)
                .Join(fearStream)
                .Join(happyStream)
                .Join(neutralStream)
                .Join(sadStream)
                .Join(surpriseStream);*/

            IProducer<((
                double angry,
                double disgust,
                double fear,
                double happy,
                double neutral,
                double sad,
                double surprise
            ) emotions, double participantTemperature)> emotionsAndTemperatureStream =
                angryStream
                .Join(disgustStream)
                .Join(fearStream)
                .Join(happyStream)
                .Join(neutralStream)
                .Join(sadStream)
                .Join(surpriseStream)
                .Join(temperatureStream, Reproducible.Nearest<double>(RelativeTimeInterval.Infinite));

            List<double> angryValues = new();
            List<double> disgustValues = new();
            List<double> fearValues = new();
            List<double> happyValues = new();
            List<double> neutralValues = new();
            List<double> sadValues = new();
            List<double> surpriseValues = new();
            List<double> participantTempValues = new();

            emotionsAndTemperatureStream.Do((values, e) =>
            {
                angryValues.Add(values.emotions.angry);
                disgustValues.Add(values.emotions.disgust);
                fearValues.Add(values.emotions.fear);
                happyValues.Add(values.emotions.happy);
                neutralValues.Add(values.emotions.neutral);
                sadValues.Add(values.emotions.sad);
                surpriseValues.Add(values.emotions.surprise);
                participantTempValues.Add(values.participantTemperature);
            });

            DateTime startTime = time.Subtract(TimeSpan.FromSeconds(windowSize));
            DateTime stopTime = time;
            Console.WriteLine($"Calculating features for {startTime} to {stopTime}");
            p.Run(startTime, stopTime, false);

            return ExtractFeatures.GetStatisticalFeatures(
                angryValues,
                disgustValues,
                fearValues,
                happyValues,
                neutralValues,
                sadValues,
                surpriseValues,
                participantTempValues,
                MODE
            );
        }

        static private void FullFeaturesExtraction()
        {
            string fullFeaturesArffPath = Path.Combine(PSI_STORES_PATH, $"FullStoreFeatures-{MODE.Value}-{WINDOW_SIZE_SECONDS}s-Actual.arff");

            if (File.Exists(fullFeaturesArffPath))
            {
                Console.WriteLine("WARNING: arffFile will be overwritten");
                Console.WriteLine(fullFeaturesArffPath);

                ConsoleKey response;
                do
                {
                    Console.Write("\nDo you want to override or skip? [yn] ");
                    response = Console.ReadKey(false).Key;
                    if (response != ConsoleKey.Enter) Console.WriteLine();

                } while (response != ConsoleKey.Y && response != ConsoleKey.N);

                if (response != ConsoleKey.Y) return;
            }

            List<string> fullArff = new List<string>();

            fullArff.Add($"@RELATION {WINDOW_SIZE_SECONDS}s-FullFeatures-Ex3-Actual\n");

            string csvHeader = ExtractFeatures.CsvFeaturesHeaders();
            foreach (string feature in csvHeader.Split(',').Prepend("timestamp").Append("class"))
            {
                string fType = "numeric";
                if (feature == "class")
                {
                    fType = "{attentive,inattentive}";
                }
                if (feature == "timestamp")
                {
                    fType = "string";
                }
                fullArff.Add($"@ATTRIBUTE {feature} {fType}");
            }
            fullArff.Add("\n\n@DATA");

            using (var p = Pipeline.Create())
            {
                var detectedFacesStore = PsiStore.Open(p, DETECTED_FACES_STORE_NAME, PSI_STORES_PATH);
                var temperatureStore = PsiStore.Open(p, TEMPERATURE_STORE_NAME, PSI_STORES_PATH);

                IProducer<double> angryStream = detectedFacesStore.OpenStream<double>("angry");
                IProducer<double> disgustStream = detectedFacesStore.OpenStream<double>("disgust");
                IProducer<double> fearStream = detectedFacesStore.OpenStream<double>("fear");
                IProducer<double> happyStream = detectedFacesStore.OpenStream<double>("happy");
                IProducer<double> neutralStream = detectedFacesStore.OpenStream<double>("neutral");
                IProducer<double> sadStream = detectedFacesStore.OpenStream<double>("sad");
                IProducer<double> surpriseStream = detectedFacesStore.OpenStream<double>("surprise");
                IProducer<double> temperatureStream = temperatureStore.OpenStream<double>("Participant temperature (filtered)");

                /*IProducer<(
                    double angry,
                    double disgust,
                    double fear,
                    double happy,
                    double neutral,
                    double sad,
                    double surprise
                )> emotionsStream =
                    angryStream
                    .Join(disgustStream)
                    .Join(fearStream)
                    .Join(happyStream)
                    .Join(neutralStream)
                    .Join(sadStream)
                    .Join(surpriseStream);*/

                IProducer<((
                        double angry,
                        double disgust,
                        double fear,
                        double happy,
                        double neutral,
                        double sad,
                        double surprise
                    ) emotions, double participantTemperature)> emotionsAndTemperatureStream =
                        angryStream
                        .Join(disgustStream)
                        .Join(fearStream)
                        .Join(happyStream)
                        .Join(neutralStream)
                        .Join(sadStream)
                        .Join(surpriseStream)
                        .Join(temperatureStream, Reproducible.Nearest<double>(RelativeTimeInterval.Infinite));

                List<(double val, DateTime origTime)> angryValues = new();
                List<(double val, DateTime origTime)> disgustValues = new();
                List<(double val, DateTime origTime)> fearValues = new();
                List<(double val, DateTime origTime)> happyValues = new();
                List<(double val, DateTime origTime)> neutralValues = new();
                List<(double val, DateTime origTime)> sadValues = new();
                List<(double val, DateTime origTime)> surpriseValues = new();
                List<(double val, DateTime origTime)> participantTempValues = new();

                DateTime? windowStart = null;

                emotionsAndTemperatureStream.Do((tuple, e) => {
                    if (!windowStart.HasValue)
                    {
                        // Initialize windowStart
                        windowStart = e.OriginatingTime;
                        return;
                    }

                    angryValues.Add((tuple.emotions.angry, e.OriginatingTime));
                    disgustValues.Add((tuple.emotions.disgust, e.OriginatingTime));
                    fearValues.Add((tuple.emotions.fear, e.OriginatingTime));
                    happyValues.Add((tuple.emotions.happy, e.OriginatingTime));
                    neutralValues.Add((tuple.emotions.neutral, e.OriginatingTime));
                    sadValues.Add((tuple.emotions.sad, e.OriginatingTime));
                    surpriseValues.Add((tuple.emotions.surprise, e.OriginatingTime));
                    participantTempValues.Add((tuple.participantTemperature, e.OriginatingTime));

                    angryValues = angryValues.EvictOlderThan(e.OriginatingTime, WINDOW_SIZE_SECONDS);
                    disgustValues = disgustValues.EvictOlderThan(e.OriginatingTime, WINDOW_SIZE_SECONDS);
                    fearValues = fearValues.EvictOlderThan(e.OriginatingTime, WINDOW_SIZE_SECONDS);
                    happyValues = happyValues.EvictOlderThan(e.OriginatingTime, WINDOW_SIZE_SECONDS);
                    neutralValues = neutralValues.EvictOlderThan(e.OriginatingTime, WINDOW_SIZE_SECONDS);
                    sadValues = sadValues.EvictOlderThan(e.OriginatingTime, WINDOW_SIZE_SECONDS);
                    surpriseValues = surpriseValues.EvictOlderThan(e.OriginatingTime, WINDOW_SIZE_SECONDS);
                    participantTempValues = participantTempValues.EvictOlderThan(e.OriginatingTime, WINDOW_SIZE_SECONDS);

                    if (e.OriginatingTime < windowStart.Value.AddSeconds(WINDOW_SIZE_SECONDS))
                    {
                        // Not yet collected enough data
                        return;
                    }

                    Console.WriteLine($"Calculating features for {e.OriginatingTime.AddSeconds(-WINDOW_SIZE_SECONDS)} to {e.OriginatingTime}");
                    string statisticalFeatures = ExtractFeatures.GetStatisticalFeatures(
                        angryValues.Select(t => t.val).ToList(),
                        disgustValues.Select(t => t.val).ToList(),
                        fearValues.Select(t => t.val).ToList(),
                        happyValues.Select(t => t.val).ToList(),
                        neutralValues.Select(t => t.val).ToList(),
                        sadValues.Select(t => t.val).ToList(),
                        surpriseValues.Select(t => t.val).ToList(),
                        participantTempValues.Select(t => t.val).ToList(),
                        MODE
                    );
                    fullArff.Add(e.OriginatingTime.ToString("o", CultureInfo.InvariantCulture) + "," + statisticalFeatures + ",?");
                    //fullCsv.Add(statisticalFeatures); // Without timestamp
                });

                p.Run(ReplayDescriptor.ReplayAll);
            }

            File.WriteAllLines(fullFeaturesArffPath, fullArff);
        }

        static private void ExtractAttentiveInattentiveExperiment3()
        {
            var distractionInattentiveModes = new List<string>() { "Distraction", "Inattentive", "Combined" };
            foreach (string diMode in distractionInattentiveModes)
            {
                string csvPath = Path.Combine(PSI_STORES_PATH, "_AllEx3Rel", $"AllEx3-{diMode}-{MODE.Value}-{WINDOW_SIZE_SECONDS}s-Relative.csv");

                if (File.Exists(csvPath))
                {
                    Console.WriteLine("WARNING: csv will be overwritten");
                    Console.WriteLine(csvPath);

                    ConsoleKey response;
                    do
                    {
                        Console.Write("\nDo you want to override or skip? [yn] ");
                        response = Console.ReadKey(false).Key;
                        if (response != ConsoleKey.Enter) Console.WriteLine();

                    } while (response != ConsoleKey.Y && response != ConsoleKey.N);

                    if (response != ConsoleKey.Y) return;
                }

                List<DateTime> distractionTimes = new();
                List<DateTime> inattentiveTimes = new();

                using (var p = Pipeline.Create())
                {
                    var distractionStore = PsiStore.Open(p, EXP3_DISTRACTION_STORE_NAME, PSI_STORES_PATH);

                    var distractionStream = distractionStore.OpenStream<string>("Distraction");
                    var inattentiveStream = distractionStore.OpenStream<string>("Inattentive");

                    distractionStream.Do((d, e) => distractionTimes.Add(e.OriginatingTime));
                    inattentiveStream.Do((i, e) => inattentiveTimes.Add(e.OriginatingTime));

                    p.Run(ReplayDescriptor.ReplayAll);
                }

                List<(DateTime from, DateTime to)> forbiddenAttentiveWindows = new();
                if (diMode.Equals("Inattentive") || diMode.Equals("Combined"))
                {
                    inattentiveTimes.ForEach(d => forbiddenAttentiveWindows.Add((d.AddSeconds(-WINDOW_SIZE_SECONDS), d)));
                }
                if (diMode.Equals("Distraction") || diMode.Equals("Combined"))
                {
                    distractionTimes.ForEach(d => forbiddenAttentiveWindows.Add((d.AddSeconds(-WINDOW_SIZE_SECONDS), d)));
                }

                List<string> attentiveCsvValues = new();

                using (var p = Pipeline.Create())
                {
                    var detectedFacesStore = PsiStore.Open(p, DETECTED_FACES_STORE_NAME, PSI_STORES_PATH);
                    var temperatureStore = PsiStore.Open(p, TEMPERATURE_STORE_NAME, PSI_STORES_PATH);

                    IProducer<double> angryStream = detectedFacesStore.OpenStream<double>("angry");
                    IProducer<double> disgustStream = detectedFacesStore.OpenStream<double>("disgust");
                    IProducer<double> fearStream = detectedFacesStore.OpenStream<double>("fear");
                    IProducer<double> happyStream = detectedFacesStore.OpenStream<double>("happy");
                    IProducer<double> neutralStream = detectedFacesStore.OpenStream<double>("neutral");
                    IProducer<double> sadStream = detectedFacesStore.OpenStream<double>("sad");
                    IProducer<double> surpriseStream = detectedFacesStore.OpenStream<double>("surprise");
                    IProducer<double> temperatureStream = temperatureStore.OpenStream<double>("Participant temperature (filtered)");

                    /*IProducer<(
                        double angry,
                        double disgust,
                        double fear,
                        double happy,
                        double neutral,
                        double sad,
                        double surprise
                    )> emotionsStream =
                        angryStream
                        .Join(disgustStream)
                        .Join(fearStream)
                        .Join(happyStream)
                        .Join(neutralStream)
                        .Join(sadStream)
                        .Join(surpriseStream);*/

                    /*IProducer<((
                        double angry,
                        double disgust,
                        double fear,
                        double happy,
                        double neutral,
                        double sad,
                        double surprise
                    ) emotions, double participantTemperature)> emotionsAndTemperatureStream =
                        angryStream
                        .Join(disgustStream)
                        .Join(fearStream)
                        .Join(happyStream)
                        .Join(neutralStream)
                        .Join(sadStream)
                        .Join(surpriseStream)
                        .Join(temperatureStream, Reproducible.Nearest<double>(RelativeTimeInterval.Infinite));*/

                    //List<double> angryValues = new();
                    //List<double> disgustValues = new();
                    //List<double> fearValues = new();
                    //List<double> happyValues = new();
                    //List<double> neutralValues = new();
                    //List<double> sadValues = new();
                    //List<double> surpriseValues = new();
                    List<double> participantTempValues = new();

                    DateTime? windowStart = null;

                    temperatureStream.Do((values, e) => {
                        if (!windowStart.HasValue) // Initialize windowStart
                        {
                            windowStart = e.OriginatingTime;
                        }

                        if (e.OriginatingTime > windowStart.Value.AddSeconds(WINDOW_SIZE_SECONDS))
                        {
                            if (forbiddenAttentiveWindows.All(f => !e.OriginatingTime.IsBetween(f.from, f.to) && !windowStart.Value.IsBetween(f.from, f.to)))
                            {
                                Console.WriteLine($"Calculating features for {windowStart} to {e.OriginatingTime}");

                                string statisticalFeatures = ExtractFeatures.GetStatisticalFeatures(
                                    //angryValues,
                                    //disgustValues,
                                    //fearValues,
                                    //happyValues,
                                    //neutralValues,
                                    //sadValues,
                                    //surpriseValues,
                                    participantTempValues,
                                    MODE
                                );

                                attentiveCsvValues.Add(statisticalFeatures);
                            }
                            else
                            {
                                Console.WriteLine($"Forbidden window for attentive features from {windowStart} to {e.OriginatingTime}");
                            }

                            //angryValues = new();
                            //disgustValues = new();
                            //fearValues = new();
                            //happyValues = new();
                            //neutralValues = new();
                            //sadValues = new();
                            //surpriseValues = new();
                            participantTempValues = new();

                            windowStart = e.OriginatingTime;
                        }
                        else
                        {
                            //angryValues.Add(values.angry);
                            //disgustValues.Add(values.disgust);
                            //fearValues.Add(values.fear);
                            //happyValues.Add(values.happy);
                            //neutralValues.Add(values.neutral);
                            //sadValues.Add(values.sad);
                            //surpriseValues.Add(values.surprise);
                            participantTempValues.Add(values);
                        }
                    });

                    p.Run(ReplayDescriptor.ReplayAll);
                }

                attentiveCsvValues = attentiveCsvValues.Select(s => s + ",attentive").ToList();
                List<string> inattentiveDistractionCsv;
                if (diMode.Equals("Distraction"))
                {
                    inattentiveDistractionCsv = distractionTimes.Select(time => ExtractFeaturesWindow(time, WINDOW_SIZE_SECONDS)).Select(s => s + ",distraction").ToList();
                } else if (diMode.Equals("Inattentive"))
                {
                    inattentiveDistractionCsv = inattentiveTimes.Select(time => ExtractFeaturesWindow(time, WINDOW_SIZE_SECONDS)).Select(s => s + ",inattentive").ToList();
                } else if (diMode.Equals("Combined"))
                {
                    inattentiveDistractionCsv = 
                        distractionTimes
                            .Select(time => ExtractFeaturesWindow(time, WINDOW_SIZE_SECONDS))
                            .Concat(inattentiveTimes.Select(time => ExtractFeaturesWindow(time, WINDOW_SIZE_SECONDS)))
                            .Select(s => s + ",inattentive")
                            .ToList();
                } else
                {
                    throw new Exception();
                }


                var combinedCsv = attentiveCsvValues.Concat(inattentiveDistractionCsv).ToList();

                string csvHeader = ExtractFeatures.CsvFeaturesHeaders() + ",class";
                File.WriteAllLines(csvPath, combinedCsv
                    .Prepend(csvHeader)
                    .ToList());
            }
        }
    }
}
