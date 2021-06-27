using System;
using System.Collections.Generic;
using System.Linq;

namespace ExtractFeatureWindows
{
    public static class LinqStatitisticsExtensions
    {
        public static double AverageOrDefault(this IEnumerable<double> values)
        {
            if (!values.Any()) return 0;
            return values.Average();
        }

        public static double MinOrDefault(this IEnumerable<double> values)
        {
            if (!values.Any()) return 0;
            return values.Min();
        }

        public static double MaxOrDefault(this IEnumerable<double> values)
        {
            if (!values.Any()) return 0;
            return values.Max();
        }

        // Adapted from https://stackoverflow.com/a/2253903/7387250
        public static double StdDev(this IEnumerable<double> values)
        {
            double ret = 0;
            int count = values.Count();
            if (count  > 1)
            {
                double avg = values.Average();

                // Sum of (value-avg)^2
                double sum = (double) values.Sum(d => Math.Pow(d - avg, 2));

                // Square root of sum of squared error divided by n
                ret = (double) Math.Sqrt(sum / count);
            }
            return ret;
        }

        // Adapted from https://stackoverflow.com/a/10738416/7387250
        public static double Median(this IEnumerable<double> source)
        {
            int count = source.Count();
            if(count == 0)
                return 0.0;

            source = source.OrderBy(n => n);

            int midpoint = count / 2;
            if(count % 2 == 0)
                return (source.ElementAt(midpoint - 1) + source.ElementAt(midpoint)) / 2;
            else
                return source.ElementAt(midpoint);
        }

        public static double Skewness(this IEnumerable<double> source)
        {
            if (!source.Any()) return 0;

            double mean = source.Average();
            double sd = source.StdDev();
            int n = source.Count();

            if (sd == 0) return 0;

            double a = source.Aggregate((acc, x) => Math.Pow(x - mean, 3));
            return (a / n) / Math.Pow(sd, 3);
        }

        public static double Kurtosis(this IEnumerable<double> source)
        {
            if (!source.Any()) return 0;

            double mean = source.Average();
            double sd = source.StdDev();
            int n = source.Count();

            if (sd == 0) return 0;

            double a = source.Aggregate((acc, x) => Math.Pow(x - mean, 4));
            return (a / n) / Math.Pow(sd, 4);
        }
    }
}
