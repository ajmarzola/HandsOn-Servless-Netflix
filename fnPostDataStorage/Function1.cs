using Azure.Storage.Blobs;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace fnPostDataStorage
{
    public class Function1
    {
        private readonly ILogger<Function1> _logger;

        public Function1(ILogger<Function1> logger)
        {
            _logger = logger;
        }

        [Function("dataStorage")]
        public IActionResult Run([HttpTrigger(AuthorizationLevel.Function, "post")] HttpRequest req)
        {
            _logger.LogInformation("Processando a Imagem");

            try
            {
                if (!req.Headers.TryGetValue("file-type", out var fileTypeHeader))
                {
                    return new BadRequestObjectResult("O cabeçalho 'file-type' é obrigatório");
                }

                var fileType = fileTypeHeader.ToString();
                var form = req.ReadFormAsync().Result;
                var file = form.Files["file"];

                if (file == null || file.Length == 0)
                {
                    return new BadRequestObjectResult("O arquivo não foi enviado");
                }

                string connectionString = System.Environment.GetEnvironmentVariable("AzureWebJobsStorage");
                string containerName = fileType;
                BlobClient blobClient = new BlobClient(connectionString, containerName, file.FileName);
                BlobContainerClient containerClient = new BlobContainerClient(connectionString, containerName);

                containerClient.CreateIfNotExists();
                containerClient.SetAccessPolicy(Azure.Storage.Blobs.Models.PublicAccessType.BlobContainer);

                var blobName = file.FileName;
                var blob = containerClient.GetBlobClient(blobName);

                using (var stream = file.OpenReadStream())
                {
                    blob.Upload(stream, true);
                }

                _logger.LogInformation("Imagem processada com sucesso");

                return new OkObjectResult(new
                {
                    Message = "Imagem processada com sucesso",
                    BlobUri = blob.Uri.ToString()
                });
            }
            catch (System.Exception ex)
            {
                _logger.LogError(ex, "Erro ao processar a imagem");
                return new BadRequestObjectResult("Erro ao processar a imagem");
            }
        }
    }
}
