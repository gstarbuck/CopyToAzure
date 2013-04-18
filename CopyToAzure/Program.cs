using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using System;
using System.Configuration;
using System.IO;

namespace CopyToAzure
{
    internal class Program
    {
        private static void Main(string[] args)
        {

            string baseDirectoryPath;
            string blobContainerName;
            string versionToPublish;

            try
            {
                baseDirectoryPath = args[0];
                blobContainerName = args[1];
                versionToPublish = args[2];
            }
            catch
            {
                Console.WriteLine("Usage: CopyToAzure [base directory path] [blob container name] [version to publish]");
                Console.WriteLine(@"Example: CopyToAzure C:\dev\MMSD\DARC-WPF\Applications\WPF\DarcWpfClient\DarcWpfClient\publish\ darcdev ");
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
            ProcessFolder(baseDirectoryPath, baseDirectoryPath, blobContainer, versionToPublish);
            Console.WriteLine("Done!");
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
                    if (info.FullName.Contains(@"Application Files\") && info.FullName.Contains(versionToPublish.Replace(".","_")))
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
        }

        private static string SwapSlashes(string inString)
        {
            return inString.Replace(@"\", "/");
        }
    }
}
