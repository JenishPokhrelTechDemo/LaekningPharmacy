cmake_minimum_required(VERSION 4.1.0)

# Path to vcpkg
set(CMAKE_TOOLCHAIN_FILE "G:/vcpkg/scripts/buildsystems/vcpkg.cmake")

project(laekning_eventhub VERSION 0.1.0 LANGUAGES C CXX)

find_package(azure-identity-cpp CONFIG REQUIRED)
find_package(azure-security-keyvault-secrets-cpp CONFIG REQUIRED)
find_package(azure-security-keyvault-keys-cpp CONFIG REQUIRED)
find_package(azure-messaging-eventhubs-cpp CONFIG REQUIRED)
find_package(azure-messaging-eventhubs-checkpointstore-blob-cpp CONFIG REQUIRED)
find_package(nlohmann_json CONFIG REQUIRED)

# 🔹 Allow passing vault name at configure time
if(NOT DEFINED KEYVAULT_NAME)
    set(KEYVAULT_NAME "mydefaultvault") # fallback
endif()

message(STATUS "Using Key Vault Name: ${KEYVAULT_NAME}")

# Define macro for C++ build
add_compile_definitions(KEYVAULT_NAME="${KEYVAULT_NAME}")

add_executable(laekning_eventhub main.cpp)

target_link_libraries(laekning_eventhub PRIVATE 
    Azure::azure-identity 
    Azure::azure-security-keyvault-secrets
    Azure::azure-security-keyvault-keys
    Azure::azure-messaging-eventhubs
    Azure::azure-messaging-eventhubs-checkpointstore-blob
    nlohmann_json::nlohmann_json
)
