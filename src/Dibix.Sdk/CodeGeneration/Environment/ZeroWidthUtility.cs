using System;
using System.Linq;
using System.Text;

namespace Dibix.Sdk.CodeGeneration
{
    internal static class ZeroWidthUtility
    {
        private const char ZeroWidthSpace = '\u200B'; // &#8203
        private const char ZeroWidthNonJoiner = '\u200C'; // &#8204
        private const char ZeroWidthJoiner = '\u200D'; // &#8205

        public static string MaskText(string text)
        {
            string binaryText = String.Join(" ", Encoding.Unicode.GetBytes(text).Select(c => Convert.ToString(c, 2).PadLeft(8, '0')).ToArray());
            string zeroWidthText = String.Join("\ufeff", binaryText.ToCharArray().Select(MaskChar).ToArray());
            return zeroWidthText;
        }

        public static string UnmaskText(string text)
        {
            string binaryText = String.Join("", text.ToCharArray().Select(UnmaskChar).ToArray());
            string originalText = Encoding.Unicode.GetString(binaryText.Split(' ').Select(s => Convert.ToByte(s, 2)).ToArray());
            return originalText;
        }

        private static char MaskChar(char c)
        {
            if (c == '1')
                return ZeroWidthSpace;

            if (c == '0')
                return ZeroWidthNonJoiner;

            return ZeroWidthJoiner;
        }

        private static char? UnmaskChar(char c)
        {
            if (c == ZeroWidthSpace)
                return '1';

            if (c == ZeroWidthNonJoiner)
                return '0';

            if (c == ZeroWidthJoiner)
                return ' ';

            return null;
        }
    }
}