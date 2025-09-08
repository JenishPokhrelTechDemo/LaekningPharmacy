using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using System.IO;
using System.Threading.Tasks;
using Azure.Identity;
using Azure.Security.KeyVault.Secrets;
using Azure.Storage.Blobs;
using Azure.AI.DocumentIntelligence;
using Microsoft.Extensions.Logging;
using Laekning.Services;
using System;

namespace Laekning.Pages
{
    public class PrescriptionOCRModel : PageModel
    {
        // ASP.NET Core hosting environment (for paths, etc.)
        private readonly IWebHostEnvironment _env;
        // Logger for logging information and warnings
        private readonly ILogger<PrescriptionOCRModel> _logger;
        // Configuration access (e.g., Key Vault URLs)
        private readonly IConfiguration _config;
        // EventHub sender for sending telemetry/events
        private readonly EventHubSender _eventHub;

        // Values loaded from Key Vault
        private string BlobUri; 
        private string ContainerName; 
        private string ModelId; 
        private string Endpoint; 
        private string ApiKey;

        // Constructor: inject dependencies
        public PrescriptionOCRModel(IWebHostEnvironment env, ILogger<PrescriptionOCRModel> logger, EventHubSender eventHub, IConfiguration config)
        {
            _env = env;
            _logger = logger;
            _eventHub = eventHub;
            _config = config;
        }

        // Uploaded file from form
        [BindProperty]
        public IFormFile uploadedFile { get; set; }

        // Filename of uploaded file (supports GET for query parameters)
        [BindProperty(SupportsGet = true)]
        public string UploadedFileName { get; set; }

        // URL of uploaded blob (supports GET for query parameters)
        [BindProperty(SupportsGet = true)]
        public string UploadedFileUrl { get; set; }

        // UI properties
        public string Title { get; set; } = "Prescription Drug Identifier";
        public string Description { get; set; } = "Drag and drop a file or browse.";
        public string UploadResult { get; set; }

        // Extracted fields from OCR
        public string extractedInscription { get; set; }
        public string ExtractedPatientDetails { get; set; }

        // Initialize secrets from Azure Key Vault
        private async Task InitializeSecretsAsync()
        {
            // If Endpoint is already initialized, skip
            if (!string.IsNullOrEmpty(Endpoint))
                return;

            string vaultUri = _config["AzureKeyVault:KeyVaultUrl"];
            var client = new SecretClient(new Uri(vaultUri), new DefaultAzureCredential());

            // Retrieve secrets for blob storage and Document Intelligence
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

        // Handles file upload from user
        public async Task OnPostAsync()
        {
            await InitializeSecretsAsync();

            if (uploadedFile != null && uploadedFile.Length > 0)
            {
                string containerName = ContainerName;
                string blobName = uploadedFile.FileName;

                // Create BlobServiceClient to connect to Azure Blob Storage
                var blobServiceClient = new BlobServiceClient(new Uri(BlobUri));
                var containerClient = blobServiceClient.GetBlobContainerClient(containerName);
                await containerClient.CreateIfNotExistsAsync();

                // Upload the file to blob
                var blobClient = containerClient.GetBlobClient(blobName);
                using (var stream = uploadedFile.OpenReadStream())
                {
                    await blobClient.UploadAsync(stream, overwrite: true);
                }

                // Set UI properties
                UploadedFileName = blobName;
                UploadedFileUrl = blobClient.Uri.ToString(); 
                UploadResult = $"Uploaded to blob storage: {UploadedFileName}";

                _logger.LogInformation("File uploaded to blob storage: {File}", UploadedFileName);

                // Send event to EventHub
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

        // Handles analyzing the uploaded file via Azure Document Intelligence
        public async Task<IActionResult> OnPostUploadToBlobAsync()
        {
            await InitializeSecretsAsync();

            if (!string.IsNullOrEmpty(UploadedFileName) && !string.IsNullOrEmpty(UploadedFileUrl))
            {
                // Create client for Document Intelligence
                var credential = new AzureKeyCredential(ApiKey);
                var client = new DocumentIntelligenceClient(new Uri(Endpoint), credential);

                // Analyze document from blob URL using pre-trained model
                var operation = await client.AnalyzeDocumentAsync(WaitUntil.Completed, ModelId, new Uri(UploadedFileUrl));
                AnalyzeResult analyzeResult = operation.Value;

                if (analyzeResult.Documents.Count > 0)
                {
                    var doc = analyzeResult.Documents[0].Fields;

                    // Extract prescription instructions
                    if (doc.TryGetValue("Inscription", out var inscriptionField))
                        extractedInscription = inscriptionField.Content;

                    // Extract patient details
                    if (doc.TryGetValue("Patient Details", out var patientField))
                        ExtractedPatientDetails = patientField.Content;

                    _logger.LogInformation("Extracted: Inscription = {Inscription}, Patient Details = {Patient}",
                        extractedInscription, ExtractedPatientDetails);

                    // Update UI
                    UploadResult += $" → Inscription: {extractedInscription}";
                    if (!string.IsNullOrEmpty(ExtractedPatientDetails))
                        UploadResult += $" | Patient: {ExtractedPatientDetails}";

                    // Send "PrescriptionAnalyzed" event to EventHub
                    var analyzedEvent = new
                    {
                        EventType = "PrescriptionAnalyzed",
                        PrescriptionId = Guid.NewGuid().ToString(),
                        ExtractedInscription = extractedInscription,
                        ExtractedPatientDetails = ExtractedPatientDetails,
                        Timestamp = DateTime.UtcNow,
                        ProcessedBy = "DocumentIntelligence-OCR"
                    };
                    await _eventHub.SendAsync(analyzedEvent);

                    // Redirect to search results page with extracted inscription
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

        // Deletes uploaded file from Azure Blob Storage
        public async Task<IActionResult> OnPostDeleteAsync()
        {
            await InitializeSecretsAsync();

            if (!string.IsNullOrEmpty(UploadedFileName))
            {
                string containerName = ContainerName;

                var blobServiceClient = new BlobServiceClient(new Uri(BlobUri));
                var containerClient = blobServiceClient.GetBlobContainerClient(containerName);
                var blobClient = containerClient.GetBlobClient(UploadedFileName);

                // Delete the blob if it exists
                await blobClient.DeleteIfExistsAsync();
                _logger.LogInformation("Deleted blob: {BlobName}", UploadedFileName);
            }

            // Clear properties for UI
            UploadedFileName = null;
            UploadedFileUrl = null;
            UploadResult = null;

            return RedirectToPage();
        }
    }
}
