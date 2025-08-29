using System;
using Azure.ResourceManager;
using System.Threading.Tasks;
using Azure.ResourceManager.CosmosDB;
using Azure.ResourceManager.CosmosDB.Models;
using Azure.ResourceManager.Resources;
using System.Collections.Generic;
using Azure.Core;

namespace cosmosv2
{
    public class DatabaseAccount
    {
        public enum Api
        {
            Sql,
            MongoDB,
            Cassandra,
            Gremlin,
            Table
        }

        public async Task ManageAccountOperations(ArmClient armClient, SubscriptionResource subscription, ResourceGroupResource resourceGroup, string location)
        {
            bool exit = false;

            while (exit == false)
            {
                Console.Clear();
                Console.WriteLine($"Database Account Operations");
                Console.WriteLine($"---------------------------");
                Console.WriteLine($"[a]   Create SQL Account");
                Console.WriteLine($"[b]   Create MongoDB Account");
                Console.WriteLine($"[c]   Create Cassandra Account");
                Console.WriteLine($"[d]   List Accounts");
                Console.WriteLine($"[e]   Get Account Details");
                Console.WriteLine($"[f]   List Account Keys");
                Console.WriteLine($"[g]   Update Account");
                Console.WriteLine($"[h]   Add Region");
                Console.WriteLine($"[i]   Change Failover Priority");
                Console.WriteLine($"[j]   Initiate Manual Failover");
                Console.WriteLine($"[k]   Regenerate Keys");
                Console.WriteLine($"[x]   Exit");

                ConsoleKeyInfo result = Console.ReadKey(true);

                if (result.KeyChar == 'a')
                {
                    Console.Clear();
                    string accountName = RandomResourceName("cosmos-sql-");
                    CosmosDBAccountResource account = await CreateAccountAsync(armClient, resourceGroup, location, accountName, Api.Sql);
                    Console.WriteLine($"Created SQL account: {account.Data.Name}");
                    Console.WriteLine("Press any key to continue.");
                    Console.ReadKey();
                }
                else if (result.KeyChar == 'b')
                {
                    Console.Clear();
                    string accountName = RandomResourceName("cosmos-mongo-");
                    CosmosDBAccountResource account = await CreateAccountAsync(armClient, resourceGroup, location, accountName, Api.MongoDB);
                    Console.WriteLine($"Created MongoDB account: {account.Data.Name}");
                    Console.WriteLine("Press any key to continue.");
                    Console.ReadKey();
                }
                else if (result.KeyChar == 'c')
                {
                    Console.Clear();
                    string accountName = RandomResourceName("cosmos-cassandra-");
                    CosmosDBAccountResource account = await CreateAccountAsync(armClient, resourceGroup, location, accountName, Api.Cassandra);
                    Console.WriteLine($"Created Cassandra account: {account.Data.Name}");
                    Console.WriteLine("Press any key to continue.");
                    Console.ReadKey();
                }
                else if (result.KeyChar == 'd')
                {
                    Console.Clear();
                    List<string> accounts = await ListAccountsAsync(resourceGroup);

                    foreach (string acct in accounts)
                    {
                        Console.WriteLine($"Account Name: {acct}");
                    }

                    Console.WriteLine("Press any key to continue.");
                    Console.ReadKey();
                }
                else if (result.KeyChar == 'e')
                {
                    Console.Clear();
                    string acct = await SelectAccount(resourceGroup);
                    if (!string.IsNullOrEmpty(acct))
                    {
                        Console.Clear();
                        await GetAccountAsync(resourceGroup, acct);
                    }
                }
                else if (result.KeyChar == 'f')
                {
                    Console.Clear();
                    string accountName = await SelectAccount(resourceGroup);
                    if (!string.IsNullOrEmpty(accountName))
                    {
                        await ListKeysAsync(resourceGroup, accountName);
                    }
                }
                else if (result.KeyChar == 'k')
                {
                    Console.Clear();
                    string accountName = await SelectAccount(resourceGroup);
                    if (!string.IsNullOrEmpty(accountName))
                    {
                        await RegenerateKeyAsync(resourceGroup, accountName);
                    }
                }
                else if (result.KeyChar == 'x')
                {
                    exit = true;
                }
            }
        }

        public async Task<CosmosDBAccountResource> CreateAccountAsync(
            ArmClient armClient,
            ResourceGroupResource resourceGroup,
            string resourceLocation,
            string accountName,
            Api apiType)
        {
            var createUpdateData = new CosmosDBAccountCreateOrUpdateContent(new AzureLocation(resourceLocation))
            {
                Locations =
                {
                    new CosmosDBAccountLocation
                    {
                        LocationName = "West US",
                        FailoverPriority = 0,
                        IsZoneRedundant = false
                    },
                    new CosmosDBAccountLocation
                    {
                        LocationName = "East US",
                        FailoverPriority = 1,
                        IsZoneRedundant = false
                    }
                },
                ConsistencyPolicy = new CosmosDBAccountConsistencyPolicy
                {
                    DefaultConsistencyLevel = DefaultConsistencyLevel.Session
                },
                IsAutomaticFailoverEnabled = true,
                IsKeyBasedMetadataWriteAccessDisabled = false,
                IsFreeTierEnabled = false,
                IsMultipleWriteLocationsEnabled = false,
                CreateMode = CosmosDBAccountCreateMode.Default
            };

            SetApi(createUpdateData, apiType);

            var operation = await resourceGroup.GetCosmosDBAccounts().CreateOrUpdateAsync(
                Azure.WaitUntil.Completed,
                accountName,
                createUpdateData);

            return operation.Value;
        }

        public async Task<List<string>> ListAccountsAsync(ResourceGroupResource resourceGroup)
        {
            var cosmosAccounts = resourceGroup.GetCosmosDBAccountsAsync();
            List<string> accountNames = new List<string>();

            await foreach (var account in cosmosAccounts)
            {
                accountNames.Add(account.Data.Name);
            }
            return accountNames;
        }

        public async Task<CosmosDBAccountResource> GetAccountAsync(ResourceGroupResource resourceGroup, string accountName)
        {
            var databaseAccount = await resourceGroup.GetCosmosDBAccountAsync(accountName);

            Console.WriteLine($"Resource Id: {databaseAccount.Value.Data.Id}");
            Console.WriteLine($"Name: {databaseAccount.Value.Data.Name}");
            Console.WriteLine($"Api: {GetApi(databaseAccount.Value.Data)}");
            Console.WriteLine($"Default Consistency Level: {databaseAccount.Value.Data.ConsistencyPolicy?.DefaultConsistencyLevel}");
            Console.WriteLine($"Connection Endpoint: {databaseAccount.Value.Data.DocumentEndpoint}");

            foreach (string capability in GetCapabilities(databaseAccount.Value.Data))
            {
                Console.WriteLine($"Capability: {capability}");
            }

            if (databaseAccount.Value.Data.DatabaseAccountOfferType == "MongoDB")
                Console.WriteLine($"Server Version: {databaseAccount.Value.Data.ApiProperties?.ServerVersion}");

            bool isMultiMaster = false;
            if (databaseAccount.Value.Data.IsMultipleWriteLocationsEnabled.GetValueOrDefault())
            {
                isMultiMaster = true;
                Console.WriteLine("Multi-Region Writes Enabled: true");
            }
            Console.WriteLine($"Free Tier: {databaseAccount.Value.Data.IsFreeTierEnabled.GetValueOrDefault()}");
            Console.WriteLine("\nList Replicated Regions for Cosmos DB Account\n------------------------------------");
            
            foreach (var location in databaseAccount.Value.Data.Locations)
            {
                Console.WriteLine($"Location Id: {location.Id}");
                Console.WriteLine($"Location Region: {location.LocationName}");
                Console.WriteLine($"Location Failover Priority: {location.FailoverPriority}");
                if (location.FailoverPriority.GetValueOrDefault() == 0 && !isMultiMaster)
                    Console.WriteLine("Is Write Region: true");
                Console.WriteLine($"Is Availability Zone: {location.IsZoneRedundant}");
                Console.WriteLine("------------------------------------");
            }

            Console.WriteLine($"Enable Analytical Storage: {databaseAccount.Value.Data.IsAnalyticalStorageEnabled.GetValueOrDefault()}");
            Console.WriteLine($"Backup Policy: {databaseAccount.Value.Data.BackupPolicy?.GetType().Name}");
            Console.WriteLine($"Enable System-Managed Failover: {databaseAccount.Value.Data.IsAutomaticFailoverEnabled.GetValueOrDefault()}");
            Console.WriteLine($"Disable DataPlane SDK access to Control Plane: {databaseAccount.Value.Data.IsKeyBasedMetadataWriteAccessDisabled.GetValueOrDefault()}");
            Console.WriteLine($"Default Identity: {databaseAccount.Value.Data.DefaultIdentity}");

            if (string.IsNullOrEmpty(databaseAccount.Value.Data.KeyVaultKeyUri))
                Console.WriteLine($"Encryption using Service-Managed Key");
            else
                Console.WriteLine($"Encryption using Customer-Managed Key. KeyVault Uri: {databaseAccount.Value.Data.KeyVaultKeyUri}");

            Console.WriteLine("Press any key to continue.");
            Console.ReadKey();

            return databaseAccount.Value;
        }

        public async Task RegenerateKeyAsync(ResourceGroupResource resourceGroup, string accountName)
        {
            var cosmosAccount = await resourceGroup.GetCosmosDBAccountAsync(accountName);
            
            var regenerateKeyContent = new CosmosDBAccountRegenerateKeyContent(CosmosDBAccountKeyKind.Primary);
            await cosmosAccount.Value.RegenerateKeyAsync(Azure.WaitUntil.Completed, regenerateKeyContent);
            
            Console.WriteLine("Primary key regenerated successfully.");
            Console.WriteLine("Press any key to continue.");
            Console.ReadKey();
        }

        public async Task ListKeysAsync(ResourceGroupResource resourceGroup, string accountName)
        {
            var cosmosAccount = await resourceGroup.GetCosmosDBAccountAsync(accountName);
            var keys = await cosmosAccount.Value.GetKeysAsync();

            Console.WriteLine("Account Keys:");
            Console.WriteLine($"Primary Key: {keys.Value.PrimaryMasterKey}");
            Console.WriteLine($"Secondary Key: {keys.Value.SecondaryMasterKey}");
            Console.WriteLine($"Primary Readonly Key: {keys.Value.PrimaryReadonlyMasterKey}");
            Console.WriteLine($"Secondary Readonly Key: {keys.Value.SecondaryReadonlyMasterKey}");

            Console.WriteLine("Press any key to continue.");
            Console.ReadKey();
        }

        private async Task<string> SelectAccount(ResourceGroupResource resourceGroup)
        {
            Console.Clear();
            List<string> accounts = await ListAccountsAsync(resourceGroup);

            if (accounts.Count == 0)
            {
                Console.WriteLine("No accounts found in resource group.");
                Console.WriteLine("Press any key to continue.");
                Console.ReadKey();
                return "";
            }

            Console.WriteLine("Select an account:");
            for (int i = 0; i < accounts.Count; i++)
            {
                Console.WriteLine($"[{i + 1}] {accounts[i]}");
            }

            Console.Write("Enter selection (1-" + accounts.Count + "): ");
            if (int.TryParse(Console.ReadLine(), out int selection) && selection > 0 && selection <= accounts.Count)
            {
                return accounts[selection - 1];
            }

            Console.WriteLine("Invalid selection.");
            Console.WriteLine("Press any key to continue.");
            Console.ReadKey();
            return "";
        }

        private void SetApi(CosmosDBAccountCreateOrUpdateContent createUpdateParameters, Api apiType)
        {
            switch (apiType)
            {
                case Api.Sql:
                    createUpdateParameters.DatabaseAccountOfferType = CosmosDBAccountOfferType.Standard;
                    break;
                case Api.MongoDB:
                    createUpdateParameters.DatabaseAccountOfferType = CosmosDBAccountOfferType.Standard;
                    createUpdateParameters.Kind = CosmosDBAccountKind.MongoDB;
                    createUpdateParameters.ApiProperties = new CosmosDBApiProperties
                    {
                        ServerVersion = CosmosDBServerVersion.Four0
                    };
                    break;
                case Api.Cassandra:
                    createUpdateParameters.DatabaseAccountOfferType = CosmosDBAccountOfferType.Standard;
                    createUpdateParameters.Capabilities.Add(new CosmosDBAccountCapability { Name = "EnableCassandra" });
                    break;
                case Api.Gremlin:
                    createUpdateParameters.DatabaseAccountOfferType = CosmosDBAccountOfferType.Standard;
                    createUpdateParameters.Capabilities.Add(new CosmosDBAccountCapability { Name = "EnableGremlin" });
                    break;
                case Api.Table:
                    createUpdateParameters.DatabaseAccountOfferType = CosmosDBAccountOfferType.Standard;
                    createUpdateParameters.Capabilities.Add(new CosmosDBAccountCapability { Name = "EnableTable" });
                    break;
            }
        }

        private string GetApi(CosmosDBAccountData cosmosAccount)
        {
            if (cosmosAccount.Kind == CosmosDBAccountKind.MongoDB)
                return "MongoDB";

            foreach (var capability in cosmosAccount.Capabilities)
            {
                if (capability.Name == "EnableCassandra")
                    return "Cassandra";
                if (capability.Name == "EnableGremlin")
                    return "Gremlin";
                if (capability.Name == "EnableTable")
                    return "Table";
            }

            return "SQL";
        }

        private List<string> GetCapabilities(CosmosDBAccountData cosmosAccount)
        {
            List<string> capabilities = new List<string>();
            foreach (var capability in cosmosAccount.Capabilities)
            {
                capabilities.Add(capability.Name);
            }
            return capabilities;
        }

        private string RandomResourceName(string prefix = "")
        {
            Random random = new Random();
            return prefix + random.Next(1000, 9999).ToString();
        }
    }
}