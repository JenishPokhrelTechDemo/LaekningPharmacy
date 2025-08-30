#include <azure/messaging/eventhubs.hpp>
#include <azure/identity.hpp>
#include <azure/identity/azure_cli_credential.hpp>
#include <azure/identity/chained_token_credential.hpp>
#include <azure/identity/managed_identity_credential.hpp>
#include <azure/keyvault/secrets.hpp>
#include <nlohmann/json.hpp>  // JSON parsing library
#include <iostream>
#include <thread>
#include <chrono>
#include <cstdlib>
#include <memory>
#include <string>

using json = nlohmann::json;
using namespace Azure::Security::KeyVault::Secrets;

int main()
{
    std::cout << "Starting Program!" << std::endl;

    auto const keyVaultUrl = "https://laekningtestkeyvault.vault.azure.net/";

        // create a credential
    auto credential = std::make_shared<Azure::Identity::ChainedTokenCredential>(
        Azure::Identity::ChainedTokenCredential::Sources{
            std::make_shared<Azure::Identity::ManagedIdentityCredential>(),
            std::make_shared<Azure::Identity::AzureCliCredential>()});


    SecretClient secretClient(keyVaultUrl, credential);
    auto eventhubConnSecret = secretClient.GetSecret("EventHubConnectionString").Value;
    auto eventhubNameSecret = secretClient.GetSecret("EventHubName").Value;

    std::string eventhubConnectionString = eventhubConnSecret.Value.Value();
    std::string eventhubName = eventhubNameSecret.Value.Value();

    if (eventhubConnectionString.empty() || eventhubName.empty())
    {
        std::cerr << "Missing environment variables EVENTHUB_CONNECTION_STRING or EVENTHUB_NAME" << std::endl;
        return 1;
    }

    // Create consumer client
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
        // Receive up to 10 events at a time
        std::vector<std::shared_ptr<const Azure::Messaging::EventHubs::Models::ReceivedEventData>> events
            = partitionClient.ReceiveEvents(10);

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
							std::string name = product.value("Name", "Unknown");
							std::string category = product.value("Category", "Unknown");
							double price = product.value("Price", 0.0);

							std::cout << "  - Name: " << name
									  << ", Category: " << category
									  << ", Price: " << price
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

        std::this_thread::sleep_for(std::chrono::milliseconds(500)); // small delay to avoid tight loop
    }

    return 0;
}
