using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Xml.Linq;
using static System.Net.Mime.MediaTypeNames;

namespace RiffNamer
{
    internal static class Program
    {
        private const string EmbeddedFilenameStringsFileName = "embedded_filename_strings.txt";
        private static void Main(string[] args)
        {
            //Extensions to check for when renaming companion files
            string[] extraFileExtensons = new string[]
            {
                "ls", //SAB
                "ls2", //StpTool
                "bin", //SBP_Tool
            };

            bool noSuffix = false;

            foreach (string arg in args)
            {
                if (arg.ToLower().Equals("-nosuf"))
                {
                    noSuffix = true;
                }
            }

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
                Console.WriteLine($"{arg}:");
                //Argument just isn't a file
                if (!File.Exists(arg))
                {
                    Console.WriteLine($"{arg} Not an existing file!!!");
                    continue;
                };

                //Filename for logs
                string fileName = Path.GetFileName(arg);
                string fileNameNoExt = Path.GetFileNameWithoutExtension(arg);
                string dir = Path.GetDirectoryName(arg);
                string wemExt = Path.GetExtension(arg);

                //Extension check
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

                    if (reader.BaseStream.Length<=0)
                    {
                        Console.WriteLine($"{fileName} is empty!!!"); //TODO?
                        continue;
                    };
                    //Based on 010 Editor template by gocha:
                    //https://www.sweetscape.com/010editor/repository/templates/file_info.php?file=RIFF.bt

                    //Header:
                    uint signature = reader.ReadUInt32();
                    if (Convert.ToString(signature, 16) != "RIFF".ToHex())
                    {
                        if (Convert.ToString(signature, 16) == "RIFX".ToHex())
                            Console.WriteLine($"{fileName} is a Big-Endian RIFF, unsupported!!!"); //TODO?
                        else
                            Console.WriteLine($"{fileName} Not a RIFF file!!! {signature}");
                        continue;
                    };

                    uint fileSize = reader.ReadUInt32();

                    //No idea what this would mean but sure let's check for that
                    uint riffType = reader.ReadUInt32();
                    if (Convert.ToString(riffType, 16) != "WAVE".ToHex())
                    {
                        Console.WriteLine($"{fileName} RIFF not a WAVE!!! {riffType}");
                        continue;
                    };

                    //Chunk reading: trying to find the chunk that has the filename marker
                    while (reader.BaseStream.Position<fileSize)
                    {
                        //switched to using Convert.ToUInt32 elsewhere,
                        //but since we're sure this is a riff file already,
                        //its probably okay for now
                        switch (new string(reader.ReadChars(4)))
                        {
                            //Actual chunk with the filename marker
                            case "LIST":
                                //No idea what most of these mean
                                uint listChunkSize = reader.ReadUInt32();
                                string subChunkType = new string(reader.ReadChars(4));
                                string tag = new string(reader.ReadChars(4)); //adtl
                                uint tagSize = reader.ReadUInt32(); //labl
                                uint unknown = reader.ReadUInt32(); // 1 usually
                                embeddedFilenameMarker = reader.ReadCString().Normalize();
                                reader.AlignStream(2);
                                break;
                            //Default generic chunk to skip
                            case "fmt ":
                            case "cue ":
                            case "smpl":
                            case "vorb":
                            case "data":
                            case "JUNK":
                                uint genericChunkSize = reader.ReadUInt32();
                                reader.BaseStream.Position += genericChunkSize;
                                break;
                            default:
                                reader.BaseStream.Position -= 4;
                                Console.WriteLine($"{fileName} Unknown entry type!! {reader.ReadUInt32()}");
                                uint unknownChunkSize = reader.ReadUInt32();
                                reader.BaseStream.Position += unknownChunkSize;
                                break;
                        };
                    };
                };

                if (embeddedFilenameMarker == "")
                {
                    Console.WriteLine($"{fileName} Does not contain an embedded filename marker!!!");
                    continue;
                };

                DumpEmbeddedFilenameMarkers(embeddedFilenameMarker);

                string newPath = "";

                string newPath_embeddedFileName = dir + "\\" + embeddedFilenameMarker;
                string newName_embeddedFileNamePrefixed = embeddedFilenameMarker + "_" + fileNameNoExt;
                string newPath_embeddedFileNamePrefixed = dir + "\\" + newName_embeddedFileNamePrefixed;

                if (!arg.Contains(embeddedFilenameMarker) && !fileNameNoExt.Equals(embeddedFilenameMarker))
                {
                    if (noSuffix)
                    {
                        if (!File.Exists(newPath_embeddedFileName + wemExt))
                        {
                            newPath = newPath_embeddedFileName;
                        }
                        else
                        {
                            if (!File.Exists(newPath_embeddedFileNamePrefixed + wemExt))
                            {
                                newPath = newPath_embeddedFileNamePrefixed;
                            }
                        }
                    }
                    else
                    {
                        if (!File.Exists(newPath_embeddedFileNamePrefixed + wemExt))
                        {
                            newPath = newPath_embeddedFileNamePrefixed;
                        }
                    }
                }
                else if (arg.Contains(embeddedFilenameMarker + "_"))
                {
                    string revertedFileName = Path.GetFileNameWithoutExtension(arg).Replace(embeddedFilenameMarker + "_", string.Empty).Normalize();
                    string newPath_revertedFileName = dir + "\\" + revertedFileName;
                    if (!File.Exists(newPath_revertedFileName + wemExt))
                    {
                        newPath = newPath_revertedFileName;
                    }
                }
                else if (arg.Equals(newPath_embeddedFileName + wemExt))
                {
                    Console.WriteLine($"{embeddedFilenameMarker} contains no sound file id to revert to!");
                    continue;
                }

                if (newPath=="")
                {
                    continue;
                }

                if (!File.Exists(newPath + wemExt))
                {
                    File.Move(arg, newPath + wemExt);
                    Console.WriteLine($"{arg} renamed to {newPath + wemExt}");
                }

                foreach (string ext in extraFileExtensons)
                {
                    string dotExt = "." + ext;
                    string extPath = dir + "\\" + fileNameNoExt + dotExt;
                    if (File.Exists(extPath))
                    {
                        if (!File.Exists(newPath + dotExt))
                        {
                            File.Move(extPath, newPath + dotExt);
                            Console.WriteLine($"{extPath} renamed to {newPath + dotExt}");
                        }
                    }
                };
            };
            Console.Read();
        }
        public static void PrefixName(string path, string name)
        {
            string dir = Path.GetDirectoryName(path);
            name = name.Normalize();
            string ext = Path.GetExtension(path).Substring(1);
            string newPath = dir + "\\" + name + "_" + Path.GetFileNameWithoutExtension(path) + "." + ext;
            if (!Path.GetFileNameWithoutExtension(path).StartsWith(name + "_"))
            {
                //Rename to originalstringname_numbername
                File.Move(path, newPath);
                Console.WriteLine($"Prefixed1 {Path.GetFileName(path)} to {name + "_" + Path.GetFileNameWithoutExtension(path)}");
            }
            else
            {
                //Rename back to numbername
                name = Path.GetFileNameWithoutExtension(path).Replace(name + "_", string.Empty);
                newPath = dir + "\\" + name + "." + ext;
                File.Move(path, newPath);
                Console.WriteLine($"Prefixed2 {Path.GetFileName(path)} to {name}");
            }
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
                Console.WriteLine($"Renamed {Path.GetFileName(path)} to {Path.GetFileNameWithoutExtension(newPath)}");
            }
            else
            {
                Console.WriteLine($"{Path.GetFileNameWithoutExtension(newPath)} already exists, prefixing");

                PrefixName(path, name);
            }
        }
        public static void DumpEmbeddedFilenameMarkers(string name)
        {
            string path = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) + "/" + EmbeddedFilenameStringsFileName;

            using (StreamWriter file = new StreamWriter(path, append: true))
            {
                file.WriteLine(name);
            }

            var contents = File.ReadAllLines(path).Distinct().ToArray();
            Array.Sort(contents);
            File.WriteAllLines(path, contents);
        }
    }
}
