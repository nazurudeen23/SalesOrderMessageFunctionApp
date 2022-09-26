using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.Xml;
using System.Text;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;

namespace SalesOrderMessageFunctionApp
{
    public static class SalesOrderMessageFunction
    {
        [FunctionName("SalesOrderMessageFunction")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = null)] HttpRequest req,
            ILogger log, ExecutionContext context)
        {
            string convertedMessage = string.Empty;
            string errorMessage = "File Not Created";
            string containerName = string.Empty;
            string receivedContentType = string.Empty;
            try
            {
                string httpRequestBody = await new StreamReader(req.Body).ReadToEndAsync();               
                receivedContentType = req.ContentType;
                
                if (receivedContentType == "application/json")
                {
                    XmlDocument doc = JsonConvert.DeserializeXmlNode(httpRequestBody, "Root");
                    convertedMessage = doc.OuterXml.ToString();
                    containerName = "salesorderjsontoxml";
                }
                else if (receivedContentType == "application/xml")
                {
                    XmlDocument doc = new XmlDocument();
                    doc.LoadXml(httpRequestBody);
                    convertedMessage = JsonConvert.SerializeXmlNode(doc);
                    containerName = "salesorderxmltojson";
                }             

                byte[] byteArray = Encoding.ASCII.GetBytes(convertedMessage);
                MemoryStream stream = new MemoryStream(byteArray);
                string blobFileName = "SalesOrder_" + DateTime.Now.ToString("dd_MM_yyyy HH:mm:ss");

                string connectionString = Environment.GetEnvironmentVariable("AzureWebJobsStorage");
                BlobServiceClient blobServiceClient = new BlobServiceClient(connectionString);              
                BlobContainerClient createContainerClient = new BlobContainerClient(connectionString, containerName);
                createContainerClient.CreateIfNotExists(PublicAccessType.BlobContainer);

                BlobClient blobClient = createContainerClient.GetBlobClient(blobFileName);
                await blobClient.UploadAsync(stream);
            }
            catch (Exception ex)
            {
                errorMessage = ex.Message;
            }
            return new OkObjectResult(errorMessage + "\n\n" + convertedMessage + "\n\n" + receivedContentType);
        }
    }
}
