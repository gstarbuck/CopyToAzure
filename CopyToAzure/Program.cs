using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;

namespace CopyToAzure
{
    internal class Program
    {
        private static void Main(string[] args)
        {

            string storageAccountConnectionString;
            string blobContainerName;
            string baseDirectoryPath;

            try
            {
                storageAccountConnectionString = args[0];
                blobContainerName = args[1];
                baseDirectoryPath = args[2];
            }
            catch
            {
                Console.WriteLine("Usage: CopyToAzure [storage connection string] [blob container name] [base directory path]");
            }

            // Load up the storage account from your connection string
            CloudStorageAccount storageAccount = CloudStorageAccount.Parse(
                ConfigurationManager.ConnectionStrings["StorageConnectionString"].ConnectionString);

            // Create the blob client
            CloudBlobClient blobClient = storageAccount.CreateCloudBlobClient();

            //Retrieve a reference to the container
            //TODO: replace hardcoded container name with command line argument
            CloudBlobContainer blobContainer = blobClient.GetContainerReference("darcdev");

            //Create the container if it doesn't already exist
            blobContainer.CreateIfNotExists();

            //???
            blobContainer.SetPermissions(new BlobContainerPermissions{PublicAccess=BlobContainerPublicAccessType.Container});


            // Now recurse through the submitted directory and upload all files
            //TODO: replace directory with command line reference
            var baseDirectoryPath = @"C:\dev\MMSD\DARC-WPF\Applications\WPF\DarcWpfClient\DarcWpfClient\publish\";
            ProcessFolder(baseDirectoryPath, baseDirectoryPath, blobContainer);
            Console.WriteLine("Done!");
        }

        private static void ProcessFolder(string baseDirectoryPath, string folderToProcess, CloudBlobContainer blobContainer)
        {
            var directory =
                new DirectoryInfo(folderToProcess);
            foreach (FileSystemInfo info in directory.EnumerateFileSystemInfos())
            {
                // Determine if entry is a directory or a file
                //if ((info.Attributes & FileAttributes.Directory) != FileAttributes.Directory)
                if ((info.Attributes & FileAttributes.Directory) == FileAttributes.Directory)
                {
                    ProcessFolder(baseDirectoryPath, info.FullName, blobContainer);
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
