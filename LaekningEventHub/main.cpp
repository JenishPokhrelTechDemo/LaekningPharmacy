#include <azure/identity/default_azure_credential.hpp>
#include <azure/keyvault/secrets/secret_client.hpp>
#include <azure/messaging/eventhubs.hpp>
#include <nlohmann/json.hpp>
#include <iostream>
#include <thread>
#include <chrono>
#include <string>
#ifndef KEYVAULT_NAME
#define KEYVAULT_NAME "defaultvault"
#endif

using namespace Azure::Security::KeyVault::Secrets;
using namespace Azure::Identity;
using json = nlohmann::json;

int main()
{
    try
    {
        // 1. Authenticate with DefaultAzureCredential
        auto credential = std::make_shared<DefaultAzureCredential>();

        //  2. Connect to Key Vault (replace with your vault URL)
        std::string keyVaultUrl = "https://" + std::string(KEYVAULT_NAME) + ".vault.azure.net/";
        SecretClient secretClient(keyVaultUrl, credential);

        // 3. Fetch secrets
        auto eventhubConnSecret = secretClient.GetSecret("EventHubConnectionString").Value;
        auto eventhubNameSecret = secretClient.GetSecret("EventHubName").Value;

        std::string eventhubConnectionString = eventhubConnSecret.Value.Value();
        std::string eventhubName = eventhubNameSecret.Value.Value();

        if (eventhubConnectionString.empty() || eventhubName.empty())
        {
            std::cerr << "Failed to fetch secrets from Key Vault." << std::endl;
            return 1;
        }

        // Existing Event Hub consumer setup
        Azure::Messaging::EventHubs::ConsumerClient consumerClient(
            eventhubConnectionString, eventhubName);

        std::cout << "Connected to Event Hub: " << eventhubName << std::endl;

        auto eventhubProperties = consumerClient.GetEventHubProperties();
        auto partitionId = eventhubProperties.PartitionIds[0];

        Azure::Messaging::EventHubs::PartitionClientOptions partitionClientOptions;
        partitionClientOptions.StartPosition.Earliest = true;
        partitionClientOptions.StartPosition.Inclusive = true;

        Azure::Messaging::EventHubs::PartitionClient partitionClient(
            consumerClient.CreatePartitionClient(partitionId, partitionClientOptions));

        std::cout << "Listening for events on Event Hub: " << eventhubName << std::endl;

        while (true)
        {
            auto events = partitionClient.ReceiveEvents(10);

            for (const auto& event : events)
            {
                try
                {
                    std::string body(event->Body.begin(), event->Body.end());
                    auto payload = json::parse(body);

                    std::string eventType = payload.value("EventType", "Unknown");
                    std::cout << "============================" << std::endl;
                    std::cout << "EventType: " << eventType << std::endl;

                    if (eventType == "PrescriptionUploaded")
                    {
                        std::cout << "PrescriptionId: " << payload.value("PrescriptionId", "N/A") << std::endl;
                        std::cout << "FileName: " << payload.value("FileName", "N/A") << std::endl;
                        std::cout << "Timestamp: " << payload.value("Timestamp", "N/A") << std::endl;
                    }
                    else if (eventType == "PrescriptionAnalyzed")
                    {
                        std::cout << "PrescriptionId: " << payload.value("PrescriptionId", "N/A") << std::endl;
                        std::cout << "FileName: " << payload.value("FileName", "N/A") << std::endl;
                        std::cout << "Timestamp: " << payload.value("Timestamp", "N/A") << std::endl;
                        std::cout << "ExtractedInscription: " << payload.value("ExtractedInscription", "N/A") << std::endl;
                        std::cout << "ExtractedPatientDetails: " << payload.value("ExtractedPatientDetails", "N/A") << std::endl;
                    }
                    else if (eventType == "ProductsIdentified")
                    {
                        std::cout << "Timestamp: " << payload.value("Timestamp", "N/A") << std::endl;

                        if (payload.contains("IdentifiedProducts") && payload["IdentifiedProducts"].is_array())
                        {
                            std::cout << "IdentifiedProducts:" << std::endl;
                            for (const auto& product : payload["IdentifiedProducts"])
                            {
                                std::cout << "  - Name: " << product.value("Name", "Unknown")
                                          << ", Category: " << product.value("Category", "Unknown")
                                          << ", Price: " << product.value("Price", 0.0)
                                          << std::endl;
                            }
                        }
                        else
                        {
                            std::cout << "IdentifiedProducts: []" << std::endl;
                        }
                    }
                    else if (eventType == "OrderPlaced")
                    {
                        std::cout << "OrderId: " << payload.value("OrderId", "N/A") << std::endl;
                        std::cout << "Customer: " << payload.value("Customer", "N/A") << std::endl;
                        std::cout << "ItemCount: " << payload.value("ItemCount", 0) << std::endl;
                        std::cout << "GiftWrap: " << payload.value("GiftWrap", false) << std::endl;
                    }

                    std::cout << "----------------------------" << std::endl;
                }
                catch (const std::exception& ex)
                {
                    std::cerr << "Error parsing event: " << ex.what() << std::endl;
                }
            }

            std::this_thread::sleep_for(std::chrono::milliseconds(500));
        }
    }
    catch (const std::exception& ex)
    {
        std::cerr << "Fatal error: " << ex.what() << std::endl;
        return 1;
    }

    return 0;
}
