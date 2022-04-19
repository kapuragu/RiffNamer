using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace RiffNamer
{
    public static class Extensions
    {
        public static string ReadCString(this BinaryReader reader)
        {
            var chars = new List<char>();
            var @char = reader.ReadChar();
            while (@char != '\0')
            {
                chars.Add(@char);
                @char = reader.ReadChar();
            }

            return new string(chars.ToArray());
        }
        public static void AlignStream(this BinaryReader reader, byte div)
        {
            long pos = reader.BaseStream.Position;
            if (pos % div != 0)
                reader.BaseStream.Position += div - pos % div;
        }
        public static string ToHex(this string input)
        {
            StringBuilder sb = new StringBuilder();
            char[] charArray = input.ToCharArray();
            Array.Reverse(charArray); //endian
            foreach (char c in charArray)
                sb.AppendFormat("{0:X2}", (int)c);
            Console.WriteLine($"{input} => {sb.ToString().Trim()}");
            return sb.ToString().Trim();
        }
    }
}
