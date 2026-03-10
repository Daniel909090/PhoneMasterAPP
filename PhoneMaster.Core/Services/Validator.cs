using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Text.RegularExpressions;

namespace PhoneMaster.Core.Services
{
    public static class Validator
    {
        public static bool IsMenuOptionValid(int value, int min, int max)
        {
            return value >= min && value <= max;
        }

        public static bool IsStringValid(string input)
        {
            return !string.IsNullOrWhiteSpace(input);
        }

        public static bool TryParseInt(string input, out int value)
        {
            return int.TryParse(input, out value);
        }

        public static bool TryParseDouble(string input, out double value)
        {
            return double.TryParse(input, out value);
        }

        public static bool TryParseYesNo(string input, out bool result)
        {
            result = false;

            if (string.IsNullOrWhiteSpace(input))
                return false;

            string cleaned = input.Trim().ToLower();

            if (cleaned == "y" || cleaned == "yes")
            {
                result = true;
                return true;
            }

            if (cleaned == "n" || cleaned == "no")
            {
                result = false;
                return true;
            }

            return false;
        }

        public static bool MatchesPattern(string input, string regex)
        {
            if (string.IsNullOrWhiteSpace(input))
                return false;

            return Regex.IsMatch(input.Trim(), regex);
        }
    }
}