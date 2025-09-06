using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using System.IO;
using System.Threading.Tasks;
using Azure.Identity;
using Azure.Security.KeyVault.Secrets;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Specialized;
using Azure;
using Azure.AI.DocumentIntelligence;
using Microsoft.Extensions.Logging;
using Laekning.Services;
using System;

namespace Laekning.Pages
{
    public class PrescriptionOCRModel : PageModel
    {
        private readonly IWebHostEnvironment _env;
        private readonly ILogger<PrescriptionOCRModel> _logger;
		private readonly IConfiguration _config;

        // Values loaded from Key Vault		
		private string BlobUri; 
		private string ContainerName; 
		private string ModelId; 
		private string Endpoint; 
		private string ApiKey;
		private readonly EventHubSender _eventHub;

        public PrescriptionOCRModel(IWebHostEnvironment env, ILogger<PrescriptionOCRModel> logger, EventHubSender eventHub, IConfiguration config)
        {
            _env = env;
            _logger = logger;
			_eventHub = eventHub;
			_config = config;
		
        }


        [BindProperty]
        public IFormFile uploadedFile { get; set; }

        [BindProperty(SupportsGet = true)]
        public string UploadedFileName { get; set; }
		
		[BindProperty(SupportsGet = true)]
		public string UploadedFileUrl {get; set;}

        public string Title { get; set; } = "Prescription Drug Identifier";
        public string Description { get; set; } = "Drag and drop a file or browse.";
        public string UploadResult { get; set; }

        public string extractedInscription { get; set; }
        public string ExtractedPatientDetails { get; set; }
		
		// Initialize secrets from Key Vault
        private async Task InitializeSecretsAsync()
        {
            if (!string.IsNullOrEmpty(Endpoint))
                return; // already initialized

            string vaultUri = _config["AzureKeyVault:KeyVaultUrl"];
            var client = new SecretClient(new Uri(vaultUri), new DefaultAzureCredential());
			
			KeyVaultSecret secretBlobUri = await client.GetSecretAsync("BlobUri");
			KeyVaultSecret secretContainerName = await client.GetSecretAsync("ContainerName");
			KeyVaultSecret secretOCRTrainingModelID = await client.GetSecretAsync("OCRTrainingModelID");
			KeyVaultSecret secretDocumentIntelligenceEndpoint = await client.GetSecretAsync("DocumentIntelligenceEndpoint");
			KeyVaultSecret secretDocumentIntelligenceAPIKey = await client.GetSecretAsync("DocumentIntelligenceAPIKey");

            BlobUri = secretBlobUri.Value; 
            ContainerName = secretContainerName.Value;
            ModelId = secretOCRTrainingModelID.Value;
			Endpoint = secretDocumentIntelligenceEndpoint.Value;
            ApiKey = secretDocumentIntelligenceAPIKey.Value;
        }

		

        public async Task OnPostAsync()
		{
			await InitializeSecretsAsync();
			if (uploadedFile != null && uploadedFile.Length > 0)
				{
				// Use your container name (e.g., "prescriptionimages")
				string containerName = ContainerName;
				string blobName = uploadedFile.FileName;

				// Create the container client (connection string should be in config or KeyVault)
				var blobServiceClient = new BlobServiceClient(new Uri(BlobUri));
                var containerClient = blobServiceClient.GetBlobContainerClient(containerName);
                await containerClient.CreateIfNotExistsAsync();

				// Get a reference to the blob
				var blobClient = containerClient.GetBlobClient(blobName);

				// Upload the file stream
				using (var stream = uploadedFile.OpenReadStream())
				{
					await blobClient.UploadAsync(stream, overwrite: true);
				}

				UploadedFileName = blobName;
				UploadedFileUrl = blobClient.Uri.ToString(); 
				UploadResult = $"Uploaded to blob storage: {UploadedFileName}";
				
				_logger.LogInformation("File uploaded to blob storage: {File}", UploadedFileName);

				// Send "PrescriptionUploaded" event
				var uploadEvent = new
				{
					EventType = "PrescriptionUploaded",
					PrescriptionId = Guid.NewGuid().ToString(),
					FileName = UploadedFileName,
					UploadedBy = User.Identity?.Name ?? "Anonymous",
					Timestamp = DateTime.UtcNow,
					BlobUrl = blobClient.Uri.ToString()
				};
				await _eventHub.SendAsync(uploadEvent);
			}
		}
		
		
		public async Task<IActionResult> OnPostUploadToBlobAsync()
		{
			await InitializeSecretsAsync();

			if (!string.IsNullOrEmpty(UploadedFileName) && !string.IsNullOrEmpty(UploadedFileUrl))
			{	
                    // Analyze using Document Intelligence
					// Build client
					var credential = new AzureKeyCredential(ApiKey);
					var client = new DocumentIntelligenceClient(new Uri(Endpoint), credential);

					// Analyze the document from blob URL
					// Start the analysis
					var operation = await client.AnalyzeDocumentAsync(WaitUntil.Completed, ModelId, new Uri(UploadedFileUrl));

					// Get the result from the operation
					AnalyzeResult analyzeResult = operation.Value;


                    if (analyzeResult.Documents.Count > 0)
                    {
                        var doc = analyzeResult.Documents[0].Fields;

                        if (doc.TryGetValue("Inscription", out var inscriptionField))
                        {
                            extractedInscription = inscriptionField.Content;
                        }

                        if (doc.TryGetValue("Patient Details", out var patientField))
                        {
                            ExtractedPatientDetails = patientField.Content;
                        }

                        _logger.LogInformation("Extracted: Inscription = {Inscription}, Patient Details = {Patient}",
                            extractedInscription, ExtractedPatientDetails);

                        UploadResult += $" → Inscription: {extractedInscription}";
                        if (!string.IsNullOrEmpty(ExtractedPatientDetails))
                            UploadResult += $" | Patient: {ExtractedPatientDetails}";
						
						
						//Send "PrescriptionAnalyzed" event
						var analyzedEvent = new
						{
							EventType = "PrescriptionAnalyzed",
							PrescriptionId = Guid.NewGuid().ToString(), // or reuse from uploadEvent if you link them
							ExtractedInscription = extractedInscription,
							ExtractedPatientDetails = ExtractedPatientDetails,
							Timestamp = DateTime.UtcNow,
							ProcessedBy = "DocumentIntelligence-OCR"
						};
						
						await _eventHub.SendAsync(analyzedEvent);


                        //  FIX: Redirect with correct query string name
                        return RedirectToPage("SearchResults", new { ExtractedInscription = extractedInscription });
                    }
                    else
                    {
                        _logger.LogWarning("No documents found in analysis result.");
                        UploadResult += " → No data extracted.";
                    }
                }
                else
                {
                    _logger.LogWarning("File not found");
                    UploadResult = "The product is either out of stock or doesn't exist.";
                }
			
			return Page();
			
		}
        

       
		public async Task<IActionResult> OnPostDeleteAsync()
			{
				await InitializeSecretsAsync();
				if (!string.IsNullOrEmpty(UploadedFileName))
					{
						// Use the same container name as in upload
						string containerName = ContainerName;

						// Create BlobServiceClient from connection string
						var blobServiceClient = new BlobServiceClient(new Uri(BlobUri));
						var containerClient = blobServiceClient.GetBlobContainerClient(containerName);
						
						// Get a reference to the blob
						var blobClient = containerClient.GetBlobClient(UploadedFileName);

						// Delete if exists
						await blobClient.DeleteIfExistsAsync();

						_logger.LogInformation("Deleted blob: {BlobName}", UploadedFileName);
					}

				UploadedFileName = null;
				UploadedFileUrl = null;
				UploadResult = null;

				return RedirectToPage();
			}

    }
}




