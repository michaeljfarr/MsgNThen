using System;
using Microsoft.Extensions.Primitives;

namespace MsgNThen.Broker.Tests.HttpHelpers
{
    /// <summary>
    /// Provides utilities to parse and modify HTTP header valeus.
    /// </summary>
    public static class HeaderUtilities
    {
        public static bool TryParseNonNegativeInt64(StringSegment value, out long result)
        {
            var asString = value.ToString();
            return long.TryParse(asString, out result);
        }
        /// <summary>
        /// Converts the non-negative 64-bit numeric value to its equivalent string representation.
        /// </summary>
        /// <param name="value">
        /// The number to convert.
        /// </param>
        /// <returns>
        /// The string representation of the value of this instance, consisting of a sequence of digits ranging from 0 to 9 with no leading zeroes.
        /// </returns>
        public static string FormatNonNegativeInt64(long value)
        {
            if (value < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(value), value, "The value to be formatted must be non-negative.");
            }

            if (value == 0)
            {
                return "0";
            }

            return value.ToString();
        }
   }
 
}