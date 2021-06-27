using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ExtractFeatureWindows
{
    class ExtractFeatures
    {
        private static readonly List<string> dataInputNames = new List<string>()
        {
            "angry",
            "disgust",
            "fear",
            "happy",
            "neutral",
            "sad",
            "surprise",
            "participantTemp",
        };

        private static readonly List<string> featureNames = new List<string>()
        {
            //"mean",
            //"median",
            "min",
            "max",
            "sd",
            "range",
            "skewness",
            "kurtosis",
        };

        private static string extractFeaturesFromList(List<double> list)
        {
            double average = list.AverageOrDefault();
            double relMin = Math.Abs(average - list.MinOrDefault());
            double relMax = Math.Abs(average - list.MaxOrDefault());

            return
                //list.Average().ToString() + ',' +
                //list.Median().ToString() + ',' +
                relMin.ToString() + ',' +
                relMax.ToString() + ',' +
                list.StdDev().ToString() + ',' +
                (Math.Abs(relMax - relMin)).ToString() + ',' +
                list.Skewness().ToString() + ',' +
                list.Kurtosis().ToString();
        }

        public static string CsvFeaturesHeaders() {
            string res = "";
            for (int i = 0; i < dataInputNames.Count; i++)
            {
                for (int j = 0; j < featureNames.Count; j++)
                {
                    res += dataInputNames[i] + '-' + featureNames[j] + ',';
                }
            }
            return res[0..^1];
        }

        public static string GetStatisticalFeatures(
            List<double> angryValues,
            List<double> disgustValues,
            List<double> fearValues,
            List<double> happyValues,
            List<double> neutralValues,
            List<double> sadValues,
            List<double> surpriseValues,
            EmotionsOrTemperatureMode mode
        ){
            if (!mode.Equals(EmotionsOrTemperatureMode.EmotionsOnly))
            {
                throw new Exception("ERROR: Incorrect mode supplied to GetStatisticalFeatures");
            }

            string angryCsvValues = extractFeaturesFromList(angryValues);
            string disgustCsvValues =  extractFeaturesFromList(disgustValues);
            string fearCsvValues =  extractFeaturesFromList(fearValues);
            string happyCsvValues =  extractFeaturesFromList(happyValues);
            string neutralCsvValues =  extractFeaturesFromList(neutralValues);
            string sadCsvValues =  extractFeaturesFromList(sadValues);
            string surpriseCsvValues =  extractFeaturesFromList(surpriseValues);

            return $"{angryCsvValues},{disgustCsvValues},{fearCsvValues},{happyCsvValues},{neutralCsvValues},{sadCsvValues},{surpriseCsvValues}";
        }

        public static string GetStatisticalFeatures(List<double> participantTempValues, EmotionsOrTemperatureMode mode)
        {
            if (!mode.Equals(EmotionsOrTemperatureMode.TempOnly))
            {
                throw new Exception("ERROR: Incorrect mode supplied to GetStatisticalFeatures");
            }

            return extractFeaturesFromList(participantTempValues);
        }

        public static string GetStatisticalFeatures(
            List<double> angryValues,
            List<double> disgustValues,
            List<double> fearValues,
            List<double> happyValues,
            List<double> neutralValues,
            List<double> sadValues,
            List<double> surpriseValues,
            List<double> participantTempValues,
            EmotionsOrTemperatureMode mode
        )
        {
            if (!mode.Equals(EmotionsOrTemperatureMode.EmotionsAndTemp) || dataInputNames.Count != 8)
            {
                throw new Exception("ERROR: Incorrect mode supplied to GetStatisticalFeatures");
            }
            
            string emotionsValues = GetStatisticalFeatures(angryValues, disgustValues, fearValues, happyValues, neutralValues, sadValues, surpriseValues, EmotionsOrTemperatureMode.EmotionsOnly);
            string participantValues = GetStatisticalFeatures(participantTempValues, EmotionsOrTemperatureMode.TempOnly);

            return $"{emotionsValues},{participantValues}";
        }
    }
}
