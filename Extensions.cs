using System.Collections.Generic;
using System.IO;

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
    }
}
