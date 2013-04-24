using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using System;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace CopyToAzure
{
    public static class Program
    {
        private static void Main(string[] args)
        {

            string baseDirectoryPath;
            string blobContainerName;

            try
            {
                baseDirectoryPath = args[0];
                blobContainerName = args[1];
            }
            catch
            {
                Trace.WriteLine("Invalid Parameters Submitted", "CopyToAzure");
                Console.WriteLine("Usage: CopyToAzure [base directory path] [blob container name] [version to publish]");
                Console.WriteLine(@"Example: CopyToAzure C:\dev\MMSD\DARC-WPF\Applications\WPF\DarcWpfClient\DarcWpfClient\publish\ darcdev");
                return;
            }

            // Load up the storage account from your connection string
            CloudStorageAccount storageAccount = CloudStorageAccount.Parse(
                ConfigurationManager.ConnectionStrings["StorageConnectionString"].ConnectionString);

            // Create the blob client
            CloudBlobClient blobClient = storageAccount.CreateCloudBlobClient();

            //Retrieve a reference to the container
            CloudBlobContainer blobContainer = blobClient.GetContainerReference(blobContainerName);

            //Create the container if it doesn't already exist
            blobContainer.CreateIfNotExists();

            //???
            blobContainer.SetPermissions(new BlobContainerPermissions{PublicAccess=BlobContainerPublicAccessType.Container});


            // Now recurse through the submitted directory and upload all files
            ProcessFolder(baseDirectoryPath, baseDirectoryPath, blobContainer, GetVersionToPublish(baseDirectoryPath));

            Trace.WriteLine("Copy to Azure Complete", "CopyToAzure");
            Console.WriteLine("Done!");
        }

        public static string GetVersionToPublish(string baseDirectoryPath)
        {
            // Get the latest version from the folder of app data
            var appFilesDir = new DirectoryInfo(baseDirectoryPath + "Application Files");

            var first = appFilesDir.GetDirectories().OrderByDescending(x => x.CreationTimeUtc).FirstOrDefault();

            if (first != null)
            {
                var versionNumber = first.Name.Substring(first.Name.IndexOf("_", StringComparison.Ordinal) + 1);
                return versionNumber;
            }
            return string.Empty;
        }

        private static void ProcessFolder(string baseDirectoryPath, string folderToProcess, CloudBlobContainer blobContainer, string versionToPublish)
        {
            var directory =
                new DirectoryInfo(folderToProcess);
            foreach (FileSystemInfo info in directory.EnumerateFileSystemInfos())
            {
                // Determine if entry is a directory or a file
                if ((info.Attributes & FileAttributes.Directory) == FileAttributes.Directory)
                {
                    // Ignore older versions to speed up publishing
                    if (info.FullName.Equals(folderToProcess + @"Application Files"))
                    {
                        ProcessFolder(baseDirectoryPath, info.FullName, blobContainer, versionToPublish);
                    }
                    else if (info.FullName.Contains(@"Application Files\") && info.FullName.Contains(versionToPublish.Replace(".","_")))
                    {
                        ProcessFolder(baseDirectoryPath, info.FullName, blobContainer, versionToPublish);
                    }
                }
                else
                {
                    UploadFile(baseDirectoryPath, info.FullName, blobContainer);
                }
            }
        }

        private static void UploadFile(string baseDirectoryPath, string fullFilePath, CloudBlobContainer blobContainer)
        {
            var trimmedPath = fullFilePath.Replace(baseDirectoryPath, string.Empty);
            var name = trimmedPath;
            if (trimmedPath.Contains(@"\"))
            {
                name = SwapSlashes(trimmedPath);
            }
            CloudBlockBlob blob = blobContainer.GetBlockBlobReference(name);
            using (var fileStream = File.OpenRead(fullFilePath))
            {
                blob.UploadFromStream(fileStream);
            }
            Console.WriteLine(trimmedPath);
            Trace.WriteLine(trimmedPath, "CopyToAzure");
        }

        private static string SwapSlashes(string inString)
        {
            return inString.Replace(@"\", "/");
        }
    }
}
