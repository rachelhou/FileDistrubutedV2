using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.IO;
using System.Configuration;

namespace FileDistributorV2
{
    class Program
    {
        public static long GetDirectorySize(string parentDirectory)
        {
            return new DirectoryInfo(parentDirectory).GetFiles("*.*", SearchOption.AllDirectories).Sum(file => file.Length);
        }

        private static void createSubDirs(string dstFolder, string srcFolder, int count )
        {
            Console.WriteLine("Creating sub directories");
            int lastSlash = srcFolder.LastIndexOf("\\");
            string rootFolder = srcFolder.Substring(lastSlash + 1);
            for (int i = 0; i < count; i++)
            {
                string folderName = dstFolder + "\\" + rootFolder + "-" + i.ToString();
                if (!Directory.Exists(folderName))
                    Directory.CreateDirectory(folderName);   
            }
        }
        static void Main(string[] args)
        {
            long sizePerSubDir = 0;
            int currFileCount = 0;
            int subDirCreated = 0;
            int count = 0;
            long CopiedBytes = 0;
            List<string> ProcessedFile = new List<string>();

            int ThreadCount = Convert.ToInt32(ConfigurationSettings.AppSettings["ThreadCount"]);
            string srcFolder = ConfigurationSettings.AppSettings["SrcDir"].ToString();
            string dstFolder = ConfigurationSettings.AppSettings["DstDir"].ToString();
            string username = ConfigurationSettings.AppSettings["Username"].ToString();
            string password = ConfigurationSettings.AppSettings["Password"].ToString();
            long totalSize = GetDirectorySize(srcFolder);
            sizePerSubDir = totalSize/ThreadCount;
            Console.WriteLine("Total CTM Folder Size: "+totalSize.ToString());
            string[] FileList = Directory.GetFiles(srcFolder, "*.*", SearchOption.AllDirectories);
            
            long numOfFiles = FileList.Length;
            
            createSubDirs(dstFolder,srcFolder,ThreadCount);
            Console.WriteLine("Logging in the shared network drive");
            ProcessStartInfo mapCmd = new ProcessStartInfo();
            mapCmd.FileName = "cmd.exe";
            mapCmd.Arguments = "/c net user Y: " +srcFolder+" /USER:"+username+" "+password+" /y";
            try
            {
                // Start the process with the info we specified.
                // Call WaitForExit and then the using statement will close.
                using (Process exeProcess = Process.Start(mapCmd))
                {
                    exeProcess.WaitForExit();
                }
            }
            catch (Exception exp)
            {
                throw exp;
            }

            while (CopiedBytes < totalSize)
            {
                long CurrentSize = 0;

                foreach (string FileName in FileList)
                {
                    if (!ProcessedFile.Contains(FileName))
                    {
                        int lastSlash = srcFolder.LastIndexOf("\\");
                        string rootFolder = srcFolder.Substring(lastSlash + 1);
                        string tempSrcFolder = srcFolder + '\\';
                        int charCount = tempSrcFolder.Length;
                        int index = FileName.IndexOf(srcFolder);
                        string tempFileName = FileName.Substring(index + charCount);
                        string folderName = dstFolder + "\\" + rootFolder+"-"+count.ToString()+"\\"+tempFileName;
                        string fullDstPath = dstFolder + "\\" + rootFolder + "-" + count.ToString() + "\\" + tempFileName;
                        folderName = Path.GetDirectoryName(folderName);
                        if (!Directory.Exists(folderName))
                            Directory.CreateDirectory(folderName);
                        ProcessedFile.Add(FileName);
                        FileInfo info = new FileInfo(FileName);
                        CurrentSize += info.Length;
                        Console.WriteLine("File Name is::" + FileName);
                        CopiedBytes += info.Length;

                        ProcessStartInfo startInfo= new ProcessStartInfo();
                        startInfo.FileName = "xcopy";
                        startInfo.Arguments = FileName+" "+fullDstPath+"* /f /y ";
                        try
                        {
                            // Start the process with the info we specified.
                            // Call WaitForExit and then the using statement will close.
                            using (Process exeProcess = Process.Start(startInfo))
                            {
                                exeProcess.WaitForExit();
                            }
                        }
                        catch (Exception exp)
                        {
                            throw exp;
                        }


                        //File.Copy(FileName, fullDstPath);
                        currFileCount++;
                        if (CurrentSize > sizePerSubDir)
                            break;
                    }
                }
                count = count + 1;
                if (count > ThreadCount+1)
                {
                    break;
                }
                if (currFileCount < numOfFiles)
                {
                    Console.WriteLine("All the files that were not copied");
                    Console.WriteLine("Total file that are pending::", numOfFiles - currFileCount);
                }
                else
                {
                    Console.WriteLine("Number of files that are not copied");
                    StreamWriter write = new StreamWriter(@"C:\Uncopied.txt");
                    for (; currFileCount < FileList.Length; currFileCount++)
                    {
                        Console.WriteLine(FileList[currFileCount]);
                        write.WriteLine(FileList[currFileCount]);
                    }
                    write.Close();
                }
            }
        }
    }
}
