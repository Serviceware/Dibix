using System;
using System.Text;

namespace Dibix
{
    internal sealed class TextObfuscator
    {
        #region Public Methods
        public static string Obfuscate(string input)
        {
            string obfuscated = Convert(input, 14);
            string encoded = System.Convert.ToBase64String(Encoding.UTF8.GetBytes(obfuscated));
            return encoded;
        }
        #endregion

        #region Private Methods
        public static string Deobfuscate(string input)
        {
            if (input == null)
                return null;

            string decoded = Encoding.UTF8.GetString(System.Convert.FromBase64String(input));
            string deobfuscated = Convert(decoded, -14);
            return deobfuscated;
        }

        private static string Convert(string source, short shift)
        {
            int maxChar = System.Convert.ToInt32(Char.MaxValue);
            int minChar = System.Convert.ToInt32(Char.MinValue);

            char[] buffer = source.ToCharArray();

            for (int i = 0; i < buffer.Length; i++)
            {
                int shifted = System.Convert.ToInt32(buffer[i]) + shift;

                if (shifted > maxChar)
                {
                    shifted -= maxChar;
                }
                else if (shifted < minChar)
                {
                    shifted += maxChar;
                }

                buffer[i] = System.Convert.ToChar(shifted);
            }

            return new string(buffer);
        }
        #endregion
    }
}