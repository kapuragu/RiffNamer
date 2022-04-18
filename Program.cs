using System;
using System.IO;

namespace RiffNamer
{
    internal static class Program
    {
        private static void Main(string[] args)
        {
            //Extensions to check for when renaming companion files
            string[] extraFileExtensons = new string[]
            {
                "ls2",
                "bin",
            };

            //Check if argument is folder and add folder contents to arguments
            foreach (string arg in args)
            {
                if (Directory.Exists(arg))
                {
                    foreach (string file in Directory.GetFiles(arg))
                    {
                        Array.Resize(ref args, args.Length + 1);
                        args[args.Length - 1] = file;
                    };
                    continue;
                };
            };

            //Main read loop
            foreach (string arg in args)
            {
                //Argument just isn't a file
                if (!File.Exists(arg))
                {
                    Console.WriteLine($"{arg} Not an existing file!!!");
                    continue;
                };

                //Filename for renaming later and for logs
                string fileName = Path.GetFileNameWithoutExtension(arg);

                /*if (!Path.HasExtension(arg))
                {
                    Console.WriteLine($"{fileName} has no extension!!!");
                    continue;
                };

                if (Path.GetExtension(arg).Substring(1) != "wem")
                {
                    Console.WriteLine($"{fileName}.{Path.GetExtension(arg).Substring(1)} Not a .WEM file!!!");
                    continue;
                };*/

                //Filename marker, will be filled in if found
                string embeddedFilenameMarker = "";

                //Actual binary reading time
                using (BinaryReader reader = new BinaryReader(new FileStream(arg, FileMode.Open)))
                {
                    //Based on 010 Editor template by gocha:
                    //https://www.sweetscape.com/010editor/repository/templates/file_info.php?file=RIFF.bt

                    //Header:
                    string signature = new string(reader.ReadChars(4));
                    if (signature != "RIFF")
                    {
                        Console.WriteLine($"{fileName}.wem Not a RIFF file!!! {signature}");
                        continue;
                    };
                    uint fileSize = reader.ReadUInt32();
                    string riffType = new string(reader.ReadChars(4));

                    //No idea what this would mean but sure let's check for that
                    if (riffType != "WAVE")
                    {
                        Console.WriteLine($"{fileName}.wem RIFF not a WAVE!!! {riffType}");
                        continue;
                    };

                    //Chunk reading: trying to find the chunk that has the filename marker
                    while (reader.BaseStream.Position<fileSize)
                    {
                        switch(new string(reader.ReadChars(4)))
                        {
                            //Actual chunk with the filename marker
                            case "LIST":
                                //No idea what most of these mean
                                uint listChunkSize = reader.ReadUInt32();
                                string subChunkType = new string(reader.ReadChars(4));
                                string tag = new string(reader.ReadChars(4)); //adtl
                                uint tagSize = reader.ReadUInt32(); //labl
                                uint unknown = reader.ReadUInt32(); // 1 usually
                                embeddedFilenameMarker = reader.ReadCString();
                                reader.AlignStream(2);
                                break;
                            //Default generic chunk to skip
                            default:
                                uint genericChunkSize = reader.ReadUInt32();
                                reader.BaseStream.Position += genericChunkSize;
                                break;
                        };
                    };
                };

                if (embeddedFilenameMarker == "")
                {
                    Console.WriteLine($"{fileName}.wem Does not contain an embedded filename marker!!!");
                    continue;
                };

                Rename(arg, embeddedFilenameMarker);

                foreach (string ext in extraFileExtensons)
                {
                    string dir = Path.GetDirectoryName(arg);
                    string potentialExtraFileNamePath = dir + "\\" + Path.GetFileNameWithoutExtension(arg) + "." + ext;
                    if (File.Exists(potentialExtraFileNamePath))
                        Rename(potentialExtraFileNamePath, embeddedFilenameMarker);
                };
            };
        }
        public static void Rename(string path, string name)
        {
            string dir = Path.GetDirectoryName(path);
            name = name.Normalize();
            string ext = Path.GetExtension(path).Substring(1);
            string newPath = dir + "\\" + name + "." + ext;
            if (!File.Exists(newPath))
            {
                File.Move(path, newPath);
                Console.WriteLine($"Renamed {Path.GetFileName(path)} to {name}");
            }
            else
            {
                if (path != newPath)
                {
                    File.Move(path, dir + "\\" + name + "_" + Path.GetFileNameWithoutExtension(path) + "." + ext);
                    Console.WriteLine($"Renamed {Path.GetFileName(path)} to {name + "_" + Path.GetFileNameWithoutExtension(path)}");
                }
                else
                    Console.WriteLine($"{Path.GetFileName(path)} already named!!!");
            }
        }
    }
}
