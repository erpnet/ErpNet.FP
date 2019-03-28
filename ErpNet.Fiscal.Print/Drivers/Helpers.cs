using System;
using System.Collections.Generic;

namespace ErpNet.Fiscal.Print.Drivers
{
    public static class Helpers
    {
        public static T[] Slice<T>(this T[] arr, uint indexFrom, uint indexTo)
        {
            if (indexFrom > indexTo)
            {
                throw new ArgumentOutOfRangeException("indexFrom is bigger than indexTo!");
            }

            uint length = indexTo - indexFrom;
            T[] result = new T[length];
            Array.Copy(arr, indexFrom, result, 0, length);

            return result;
        }

        public static string WithMaxLength(this string text, int maxLength)
        {
            if (text.Length <= maxLength)
            {
                return text;
            }

            return text.Substring(0, maxLength);
        }

        public static IEnumerable<string> WrapAtLength(this string text, int maxLength)
        {
            if (text == null)
                throw new ArgumentNullException("text");
            if (maxLength < 1)
                throw new ArgumentException("'maxLength' must be greater than 0.");

            var rows = Math.Ceiling((double)text.Length / maxLength);
            if (rows < 2) return new string[] { text };

            var listOfStrings = new List<string>();

            for (int i = 0; i < rows; i++)
            {
                var index = i * maxLength;
                var length = Math.Min(maxLength, text.Length - index);
                listOfStrings.Add(text.Substring(index, length));
            }

            return listOfStrings;
        }

        public static IDictionary<string, string> MergeWith(this IDictionary<string, string> options, IDictionary<string, string> newOptions)
        {
            if (newOptions != null)
            {
                foreach (KeyValuePair<string, string> newOption in newOptions)
                {
                    options[newOption.Key] = newOption.Value;
                }
            }
            return options;
        }
    }
}
