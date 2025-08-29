# Cosmos DB Management - Azure.ResourceManager.CosmosDB Version

This directory contains the **cosmosv2** project that demonstrates using the latest preview of the `Azure.ResourceManager.CosmosDB` SDK instead of the older `Microsoft.Azure.Management.CosmosDB` SDK used in the original `cosmos-management-generated` project.

## Key Differences from the Original Implementation

### Authentication
- **Old**: Uses `ServiceClientCredentials` with `Microsoft.Rest.Azure.Authentication`
- **New**: Uses `Azure.Identity` with `DefaultAzureCredential` or `ClientSecretCredential`

### Client Initialization
- **Old**: Creates `CosmosDBManagementClient` 
- **New**: Uses `ArmClient` from Azure Resource Manager

### Resource Navigation
- **Old**: Direct method calls like `cosmosClient.DatabaseAccounts.CreateOrUpdateAsync()`
- **New**: Resource collection pattern like `resourceGroup.GetCosmosDBAccounts().CreateOrUpdateAsync()`

### Model Classes
- **Old**: Uses classes like `DatabaseAccountCreateUpdateParameters`, `SqlDatabaseCreateUpdateParameters`
- **New**: Uses classes like `CosmosDBAccountCreateOrUpdateContent`, `CosmosDBSqlDatabaseCreateOrUpdateContent`

### Async Patterns
- **Old**: Standard async/await with IEnumerable results
- **New**: Uses `IAsyncEnumerable` with `await foreach` for listing operations

## Project Structure

- `Program.cs` - Main application with Azure Resource Manager authentication and navigation
- `appsettings.json` - Configuration file (same format as original)
- `backup/` - Contains the more complex implementations that were being developed (excluded from build)

## Current Implementation Status

This is a **demonstration version** that shows:
- ✅ Basic project structure with Azure.ResourceManager.CosmosDB SDK
- ✅ Authentication using Azure.Identity
- ✅ Basic account listing functionality  
- ✅ Menu-driven interface similar to the original
- ✅ Successful compilation and execution

The complex API operations (create databases, containers, etc.) are in the backup folder but need further development due to API differences between the expected and actual Azure Resource Manager SDK types.

## Usage

1. Configure authentication credentials in `appsettings.json` or use Azure CLI/Visual Studio authentication
2. Run the application: `dotnet run`
3. Follow the menu options to authenticate and explore basic operations

## Dependencies

- `Azure.ResourceManager.CosmosDB` v1.3.2 (stable version)
- `Azure.Identity` v1.12.0
- `Microsoft.Extensions.Configuration` v8.0.0
- Target Framework: .NET 8.0

## Next Steps

To complete the full conversion:
1. Research the correct API patterns for the stable Azure.ResourceManager.CosmosDB SDK
2. Implement complete CRUD operations for each API type (SQL, MongoDB, Cassandra, etc.)
3. Update throughput management utilities
4. Add comprehensive error handling
5. Update documentation

This demonstrates the core migration pattern from the older management SDK to the modern Azure Resource Manager SDK approach.