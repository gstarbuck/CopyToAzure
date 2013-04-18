using System;
using System.Globalization;
using System.IO;
using System.Net;
using System.Security.Cryptography;
using System.Text;

namespace CopyToAzure
{
    internal class AzureStorageHelper
    {
        public String CreateAuthorizationHeader(String canonicalizedString)
        {
            String signature;
            //UTF8Encoding utf8Encoding = new UTF8Encoding();
            //byte[] storageKey = utf8Encoding.GetBytes(AzureStorageConstants.Key);
            byte[] storageKey = Convert.FromBase64String(AzureStorageConstants.Key);

            using (var hmacSha256 = new HMACSHA256(storageKey))
            {
                Byte[] dataToHmac = Encoding.UTF8.GetBytes(canonicalizedString);
                signature = Convert.ToBase64String(hmacSha256.ComputeHash(dataToHmac));
            }

            string authorizationHeader = String.Format(
                CultureInfo.InvariantCulture,
                "{0} {1}:{2}",
                AzureStorageConstants.SharedKeyAuthorizationScheme,
                AzureStorageConstants.Account,
                signature);

            return authorizationHeader;
        }

        public void PutBlob(String containerName, String blobName)
        {
            const string requestMethod = "PUT";

            string urlPath = String.Format("{0}/{1}", containerName, blobName);

            const string storageServiceVersion = "2011-08-18"; // "2009-09-19";

            string dateInRfc1123Format = DateTime.UtcNow.ToString("R", CultureInfo.InvariantCulture);

            string content = "The Name of This Band is Talking Heads";
            var utf8Encoding = new UTF8Encoding();
            byte[] blobContent = utf8Encoding.GetBytes(content);
            int blobLength = blobContent.Length;

            const String blobType = "BlockBlob";


            string canonicalizedHeaders = String.Format(
                "x-ms-blob-type:{0}\nx-ms-date:{1}\nx-ms-version:{2}",
                blobType,
                dateInRfc1123Format,
                storageServiceVersion);
            /*
            String canonicalizedHeaders = String.Format(
             "x-mx-blob-content-length:{3}\nx-ms-blob-type:{0}\nx-ms-date:{1}\nx-ms-version:{2}",
             blobType,
             dateInRfc1123Format,
             storageServiceVersion,
             blobLength.ToString());
            */
            string canonicalizedResource = String.Format("/{0}/{1}", AzureStorageConstants.Account, urlPath);
            string stringToSign = String.Format(
                "{0}\n\n\n{1}\n\n\n\n\n\n\n\n\n{2}\n{3}",
                requestMethod,
                blobLength,
                canonicalizedHeaders,
                canonicalizedResource);
            String authorizationHeader = CreateAuthorizationHeader(stringToSign);

            var uri = new Uri(AzureStorageConstants.BlobEndPoint + urlPath);
            var request = (HttpWebRequest) WebRequest.Create(uri);
            request.Method = requestMethod;
            request.Headers.Add("x-ms-blob-type", blobType);
            request.Headers.Add("x-ms-date", dateInRfc1123Format);
            request.Headers.Add("x-ms-version", storageServiceVersion);
            request.Headers.Add("Authorization", authorizationHeader);
            request.ContentLength = blobLength;

            try
            {
                using (Stream requestStream = request.GetRequestStream())
                {
                    requestStream.Write(blobContent, 0, blobLength);
                }

                using (var response = (HttpWebResponse) request.GetResponse())
                {
                    String ETag = response.Headers["ETag"];
                }
            }
            catch (WebException webEx)
            {
                if (webEx != null)
                {
                    WebResponse resp = webEx.Response;
                    if (resp != null)
                    {
                        using (var sr = new StreamReader(resp.GetResponseStream(), true))
                        {
                            string responseText = sr.ReadToEnd();
                            //This is where details about this 403 message can be found
                        }
                    }
                }
            }
        }

        private class AzureStorageConstants
        {
            public const string Key = "xxxx";
            public const string SharedKeyAuthorizationScheme = "SharedKey";
            public const string Account = "xxxx";
            public const string BlobEndPoint = "http://xxxxx.blob.core.windows.net/";
        }
    }
}