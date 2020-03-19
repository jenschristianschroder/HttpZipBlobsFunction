using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using ICSharpCode.SharpZipLib.Zip;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;

namespace Micrososft.HttpZipBlobs
{
    public static class HttpZipBlobs
    {
        [FunctionName("HttpZipBlobs")]
        public static async Task<string> Run([HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = null)] HttpRequest req, ILogger log)
        {
            string zipUri = null;
            
            try
            {
                string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
                string[] files = JsonConvert.DeserializeObject<string[]>(requestBody);
                if(files.Length <= 0)
                    throw new Exception("Did not receive any files. Will exit (0)");
                else
                    log.LogInformation($"Received: {files}");
                    
                //destination
                string destinationStorageAddress = Environment.GetEnvironmentVariable("destinationStorageConnectionString");
                string destinationContainer = Environment.GetEnvironmentVariable("destinationContainer");

                CloudStorageAccount storageAccountDestination = CloudStorageAccount.Parse(Environment.GetEnvironmentVariable("destinationStorageConnectionString"));
                var cloudBlobClientDestination = storageAccountDestination.CreateCloudBlobClient();
                var cloudBlobContainer = cloudBlobClientDestination.GetContainerReference(destinationContainer);

                await cloudBlobContainer.CreateIfNotExistsAsync();

                CloudBlockBlob zipBlob = cloudBlobContainer.GetBlockBlobReference(Guid.NewGuid().ToString() + ".zip");
                zipUri = zipBlob.Uri.ToString();

                //source
                string sourceStorage = Environment.GetEnvironmentVariable("sourceStorage");
                string sourceContainer = Environment.GetEnvironmentVariable("sourceContainer");

                CloudStorageAccount storageAccountSource = CloudStorageAccount.Parse(Environment.GetEnvironmentVariable("sourceStorageConnectionString"));
                var cloudBlobClientSource = storageAccountSource.CreateCloudBlobClient();
                var cloudBlobContainerSource = cloudBlobClientSource.GetContainerReference(sourceContainer);

                //zip source files into destination
                MemoryStream outputStream = new MemoryStream();
                                    
                using (var zipOutputStream = new ZipOutputStream(outputStream))
                {
                    foreach(string filename in files)
                    {
                        using(MemoryStream blobMemStream = new MemoryStream()){

                            CloudBlockBlob myBlob = new CloudBlockBlob(new Uri(storageAccountSource.BlobStorageUri.PrimaryUri.AbsoluteUri + sourceContainer + "/" + filename));
                            await myBlob.DownloadToStreamAsync(blobMemStream);

                            zipOutputStream.SetLevel(0); // 0 = no compression, 9 = maximum compression

                            var entry = new ZipEntry(filename)
                            {
                                DateTime = DateTime.Now,
                                Size = blobMemStream.Length
                            };

                            zipOutputStream.PutNextEntry(entry);
                            blobMemStream.WriteTo(zipOutputStream);
                            blobMemStream.Flush();
                        }
                    }
                    zipOutputStream.Finish();
                        
                    outputStream.Position = 0;
                    await zipBlob.UploadFromStreamAsync(outputStream);
                    zipOutputStream.Close();
                }
            }
            catch (Exception e)
            {
                log.LogError(e.Message);
            }

            return zipUri;
        }
    }
}
