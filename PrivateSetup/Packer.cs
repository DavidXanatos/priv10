using Mono.Cecil;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace PrivateSetup
{
    public class Packer
    {
        public static string ResPath = @"\Resources";
        public static string FilePath = @"\Files";

        public bool PrepareSetup(string SourcePath)
        {
            if (IsValid())
            {
                App.ShowMessage("Setup is already Prepared.");
                return true;
            }

            Console.WriteLine("Preparing Setup...");

            if (SourcePath == null || SourcePath.Length == 0)
                SourcePath = App.appPath + FilePath;
            Console.WriteLine("Source Path: {0}", SourcePath);

            FileVersionInfo fvi = FileVersionInfo.GetVersionInfo(SourcePath + @"\" + SetupData.AppBinary);

            Directory.CreateDirectory(App.appPath + ResPath);
            foreach (string fileName in Directory.GetFiles(App.appPath + ResPath))
                File.Delete(fileName);

            StreamWriter indexStram = new StreamWriter(App.appPath + ResPath + @"\FILE_IDX.TXT");

            var foundFiles = MiscFunc.EnumAllFiles(SourcePath);
            Console.WriteLine("Found Files:");
            for(int i = 0; i < foundFiles.Count; i++)
            {
                var filePath = foundFiles[i];

                var fileName = filePath.Substring(SourcePath.Length + (SourcePath[SourcePath.Length - 1].Equals('\\') ? 0 : 1));

                var resName = "FILE_" + i.ToString().PadLeft(3, '0') + ".BIN";

                string hashStr = "";
                using (var inStream = File.OpenRead(filePath))
                {
                    using (Stream outStream = File.Create(App.appPath + ResPath +  @"\" + resName))
                    {
                        using (Stream packStream = new DeflateStream(outStream, CompressionMode.Compress)) 
                        {
                            inStream.CopyTo(packStream);
                        }
                    }

                    using (var md5 = MD5.Create())
                    {
                        inStream.Seek(0, SeekOrigin.Begin);
                        var hash = md5.ComputeHash(inStream);
                        hashStr = BitConverter.ToString(hash).Replace("-", "");
                    }
                }

                indexStram.WriteLine(resName + "\t" + hashStr + "\t" + fileName);
                Console.WriteLine(resName + "\t" + hashStr + "\t" + fileName);
            }
            Console.WriteLine("+++");

            indexStram.Dispose();

            try
            {
                return CreateSetup(fvi);
            }
            catch (FileNotFoundException)
            {
                Console.WriteLine("Failed to create setup, Mono.Cecil libraries are missing: https://github.com/jbevain/cecil/");
                return false;
            }
        }

        private bool CreateSetup(FileVersionInfo fvi)
        {
            var SourcePath = App.appPath + ResPath;

            //File.Copy(App.exePath, SourcePath + @"\FILE_BAK.EXE");

            var assemblyDef = AssemblyDefinition.ReadAssembly(App.exePath);
            Console.WriteLine("Loaded assembly " + assemblyDef);

            var resources = assemblyDef.MainModule.Resources;

            foreach (string filePath in Directory.GetFiles(SourcePath))
            {
                var fileName = filePath.Substring(SourcePath.Length + (SourcePath[SourcePath.Length - 1].Equals('\\') ? 0 : 1));
                var resourceName = "PrivateSetup.Resources." + fileName.Replace(@"\", ".");

                var newResource = new EmbeddedResource(resourceName, ManifestResourceAttributes.Public, File.ReadAllBytes(filePath));
                resources.Add(newResource);

                /*var selectedResource = resources.FirstOrDefault(x => x.Name == resourceName);

                if (selectedResource != null)
                {
                    var newResource = new EmbeddedResource(resourceName, selectedResource.Attributes, File.ReadAllBytes(filePath));
                    resources.Remove(selectedResource);
                    resources.Add(newResource);
                }
                else
                {
                    Console.WriteLine("Could not find a resource with name " + resourceName);
                    //Console.WriteLine("Available resources: " + String.Join(", ", resources.Select(x => x.Name).DefaultIfEmpty("<none>")));
                }*/
            }

            assemblyDef.Name.Version = new Version(fvi.FileMajorPart, fvi.FileMinorPart, fvi.FileBuildPart, fvi.ProductPrivatePart);

            string assemblyPath = App.appPath + @"\" + SetupData.AppKey + "-Setup.exe";
            assemblyDef.Write(assemblyPath);

            Console.WriteLine("Successfully created setup {0}", assemblyPath);

            return true;
        }

        public bool IsValid(bool bStrict = true)
        {
            if (Assembly.GetExecutingAssembly().GetManifestResourceStream("PrivateSetup.Resources.FILE_IDX.TXT") != null)
                return true;
#if DEBUG
            if (!bStrict && Directory.Exists(App.appPath + FilePath))
                return true;
#endif
            return false;
        }

        public void Enum()
        {
            var assemblyDef = AssemblyDefinition.ReadAssembly(App.exePath);

            var resources = assemblyDef.MainModule.Resources;

            foreach (var resource in resources)
            {
                Console.WriteLine(resource.Name);
            }
        }

        public bool Test()
        {
            return Extract(null);
        }

        public bool Extract(string TargetPath)
        {
            bool NoErrors = true;

            if (TargetPath != null)
            {
                if (TargetPath.Length == 0)
                    TargetPath = App.appPath + Packer.FilePath;

                Console.WriteLine("Extracting embeded files to: {0}", TargetPath);
            }
            else
                Console.WriteLine("Testing Setup Integrity...");

            StreamReader indexStram = new StreamReader(Assembly.GetExecutingAssembly().GetManifestResourceStream("PrivateSetup.Resources.FILE_IDX.TXT"));

            Console.WriteLine("Packed Files:");
            while (!indexStram.EndOfStream)
            {
                var rawLine = indexStram.ReadLine();
                var line = rawLine.Split('\t');

                string resName = line[0];

                string hashStr = "";
                using (Stream inStream = Assembly.GetExecutingAssembly().GetManifestResourceStream("PrivateSetup.Resources." + resName))
                {
#if (!DEBUG)
                    try
#endif
                    {
                        using (Stream rawStream = new DeflateStream(inStream, CompressionMode.Decompress))
                        {
                            MemoryStream memStream = new MemoryStream();
                            rawStream.CopyTo(memStream);
                            memStream.Seek(0, SeekOrigin.Begin);

                            using (var md5 = MD5.Create())
                            {
                                byte[] hash = null;
                                if (TargetPath != null)
                                {
                                    string filePath = TargetPath + @"\" + line[2];
                                    Directory.CreateDirectory(Path.GetDirectoryName(filePath));

                                    using (Stream outStream = File.Create(filePath))
                                    {
                                        memStream.CopyTo(outStream);
                                    }

                                    using (var testStream = File.OpenRead(filePath))
                                    {
                                        hash = md5.ComputeHash(testStream);
                                    }
                                }
                                else
                                {
                                    hash = md5.ComputeHash(memStream);
                                }
                                hashStr = BitConverter.ToString(hash).Replace("-", "");
                            }
                        }
                    }
#if (!DEBUG)
                    catch
                    {
                        Console.WriteLine("Packed resource is corrupted!");
                    }
#endif
                }

                if (line[1].Equals(hashStr))
                    Console.WriteLine(rawLine + "\t OK");
                else
                {
                    Console.WriteLine(rawLine + "\t CORRUPTED");
                    NoErrors = false;
                }
            }
            Console.WriteLine("+++");

            indexStram.Dispose();

            return NoErrors;
        }

        /*public void Empty()
        {
            var assemblyDef = AssemblyDefinition.ReadAssembly(App.exePath);

            var resources = assemblyDef.MainModule.Resources;

            foreach (var resource in resources.ToList().Where(x => x.Name.Contains("PrivateSetup.Resources.FILE_")))
            {
                resources.Remove(resource);
            }

            assemblyDef.Write(App.appPath + @"\PrivateSetup_empty.exe");

            Console.WriteLine("Successfully emptyed setup {0}", App.appPath + @"\PrivateSetup_empty.exe");
        }*/

        public class FileInfo
        {
            public string FileName;
            public string Hash;
            public string Alias;
        }

        public List<FileInfo> EnumFiles()
        {
            List<FileInfo> files = new List<FileInfo>();

            if (IsValid())
            {
                StreamReader indexStram = new StreamReader(Assembly.GetExecutingAssembly().GetManifestResourceStream("PrivateSetup.Resources.FILE_IDX.TXT"));
                while (!indexStram.EndOfStream)
                {
                    var rawLine = indexStram.ReadLine();
                    var line = rawLine.Split('\t');

                    files.Add(new FileInfo() { FileName = line[2], Alias = line[0], Hash = line[1] });
                }
            }
            else
            {
                string SourcePath = App.appPath + FilePath;
                var foundFiles = MiscFunc.EnumAllFiles(SourcePath);
                foreach (var filePath in foundFiles)
                {
                    var fileName = filePath.Substring(SourcePath.Length + (SourcePath[SourcePath.Length - 1].Equals('\\') ? 0 : 1));

                    string hashStr;
                    using (var md5 = MD5.Create())
                    {
                        using (var inStream = File.OpenRead(filePath))
                        {
                            byte[] hash = md5.ComputeHash(inStream);
                            hashStr = BitConverter.ToString(hash).Replace("-", "");
                        }
                    }

                    files.Add(new FileInfo() { FileName = fileName, Hash = hashStr });
                }
            }

            return files;
        }

        public bool ExtractFile(FileInfo file, string installationPath)
        {
            for (int i = 0; i < 3; i++)
            {
                try
                {
                    string targetPath = installationPath + @"\" + file.FileName;
                    if (File.Exists(targetPath))
                        MiscFunc.SafeDelete(targetPath);
                    else
                        Directory.CreateDirectory(Path.GetDirectoryName(targetPath));

                    if (IsValid())
                    {
                        using (Stream inStream = Assembly.GetExecutingAssembly().GetManifestResourceStream("PrivateSetup.Resources." + file.Alias))
                        {
                            using (Stream rawStream = new DeflateStream(inStream, CompressionMode.Decompress))
                            {
                                MemoryStream memStream = new MemoryStream();
                                rawStream.CopyTo(memStream);
                                memStream.Seek(0, SeekOrigin.Begin);

                                using (var md5 = MD5.Create())
                                {
                                    using (Stream outStream = File.Create(targetPath))
                                    {
                                        memStream.CopyTo(outStream);
                                    }

                                    using (var testStream = File.OpenRead(targetPath))
                                    {
                                        byte[] hash = md5.ComputeHash(testStream);
                                        var hashStr = BitConverter.ToString(hash).Replace("-", "");

                                        if (!file.Hash.Equals(hashStr))
                                        {
                                            return false;
                                        }
                                    }
                                }
                            }
                        }
                    }
                    else
                    {
                        File.Copy(App.appPath + FilePath + @"\" + file.FileName, targetPath);
                    }

                    return true;
                }
                catch
                {
                    Thread.Sleep(1000 * (i + 1));
                }
            }
            return false;
        }
    }
}
