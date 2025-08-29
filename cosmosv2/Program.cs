using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Azure.Identity;
using Azure.ResourceManager;
using Azure.ResourceManager.CosmosDB;
using Azure.ResourceManager.Resources;
using System.Collections.Generic;
using Azure.Core;

namespace cosmosv2
{
    class Program
    {
        #pragma warning disable CS8618  //Suppress non-nullable fields below

        private static IConfigurationRoot _config;
        private static string _subscriptionId;
        private static string _location;
        private static string _resourceGroupName;
        private static ArmClient _armClient;
        private static SubscriptionResource _subscription;
        private static ResourceGroupResource _resourceGroup;

        static async Task Main(string[] args)
        {
            Console.WriteLine("Azure Cosmos DB Management using Azure.ResourceManager.CosmosDB");
            Console.WriteLine("================================================================");
            
            //=================================================================
            //Load secrets
            IConfigurationBuilder builder = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .AddUserSecrets<Secrets>();

            _config = builder.Build();

            await MainMenu();
        }

        static async Task MainMenu()
        {
            try
            {
                bool exit = false;

                while (exit == false)
                {
                    Console.Clear();
                    Console.WriteLine($"Azure Cosmos DB Management API Samples (Azure.ResourceManager.CosmosDB)");
                    Console.WriteLine($"-----------------------------------------------------------------------");
                    Console.WriteLine($"[a]   Authenticate");
                    Console.WriteLine($"[b]   Set Subscription");
                    Console.WriteLine($"[c]   Set Region for ARM account resource");
                    Console.WriteLine($"[d]   Set or Create a Resource Group");
                    Console.WriteLine($"--------------------------------------");
                    Console.WriteLine($"[e]   Database Account Operations (Basic)");
                    Console.WriteLine($"[f]   NoSQL API Operations (Basic)");
                    Console.WriteLine($"[x]   Exit");

                    ConsoleKeyInfo result = Console.ReadKey(true);

                    if (result.KeyChar == 'a')
                    {
                        Console.Clear();
                        await AuthenticateAsync();
                    }
                    else if (result.KeyChar == 'b')
                    {
                        Console.Clear();
                        await SetSubscriptionAsync();
                    }
                    else if (result.KeyChar == 'c')
                    {
                        Console.Clear();
                        SetRegion();
                    }
                    else if (result.KeyChar == 'd')
                    {
                        Console.Clear();
                        await SetResourceGroupAsync();
                    }
                    else if (result.KeyChar == 'e')
                    {
                        Console.Clear();
                        await AccountOperations();
                    }
                    else if (result.KeyChar == 'f')
                    {
                        Console.Clear();
                        await NoSqlOperations();
                    }
                    else if (result.KeyChar == 'x')
                    {
                        exit = true;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"\nError: {ex.Message}");
                Console.WriteLine("Press any key to continue.");
                Console.ReadKey();
            }
        }

        static void SetRegion()
        {
            Console.Write("Enter region: ");
            _location = Console.ReadLine() ?? "East US";
            Console.WriteLine($"Location: {_location}");
            Console.WriteLine("Press any key to continue.");
            Console.ReadKey();
        }

        static async Task SetResourceGroupAsync()
        {
            Console.Write("Enter resource group name: ");
            _resourceGroupName = Console.ReadLine() ?? RandomResourceName("rg-");

            try
            {
                _resourceGroup = await _subscription.GetResourceGroups().GetAsync(_resourceGroupName);
                Console.WriteLine($"Using existing resource group: {_resourceGroupName}");
            }
            catch
            {
                Console.WriteLine($"Resource group {_resourceGroupName} not found. Creating new resource group.");
                _resourceGroupName = await CreateResourceGroupAsync(_armClient, _subscriptionId, _location);
                _resourceGroup = await _subscription.GetResourceGroups().GetAsync(_resourceGroupName);
            }

            Console.WriteLine("Press any key to continue.");
            Console.ReadKey();
        }

        static async Task AuthenticateAsync()
        {
            string? tenantId = _config["tenantId"];
            string? clientId = _config["appId"];
            string? clientSecret = _config["password"];

            if (!string.IsNullOrEmpty(tenantId) && !string.IsNullOrEmpty(clientId) && !string.IsNullOrEmpty(clientSecret))
            {
                var credential = new ClientSecretCredential(tenantId, clientId, clientSecret);
                _armClient = new ArmClient(credential);
                Console.WriteLine("Authenticated using service principal from configuration.");
            }
            else
            {
                // Fall back to default credential (supports various authentication methods)
                var credential = new DefaultAzureCredential();
                _armClient = new ArmClient(credential);
                Console.WriteLine("Authenticated using DefaultAzureCredential.");
            }

            Console.WriteLine("Press any key to continue.");
            Console.ReadKey();
        }

        static async Task SetSubscriptionAsync()
        {
            _subscriptionId = _config["subscriptionId"] ?? "";
            
            if (string.IsNullOrEmpty(_subscriptionId))
            {
                Console.Write("Enter subscription ID: ");
                _subscriptionId = Console.ReadLine() ?? "";
            }

            try
            {
                _subscription = await _armClient.GetDefaultSubscriptionAsync();
                if (_subscription.Data.SubscriptionId != _subscriptionId)
                {
                    _subscription = await _armClient.GetSubscriptionResource(SubscriptionResource.CreateResourceIdentifier(_subscriptionId)).GetAsync();
                }
                Console.WriteLine($"Using subscription: {_subscription.Data.SubscriptionId}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting subscription: {ex.Message}");
            }

            Console.WriteLine("Press any key to continue.");
            Console.ReadKey();
        }

        static async Task<string> CreateResourceGroupAsync(ArmClient armClient, string subscriptionId, string location)
        {
            string resourceGroupName = RandomResourceName("rg-cosmos-");
            
            var subscription = await armClient.GetSubscriptionResource(SubscriptionResource.CreateResourceIdentifier(subscriptionId)).GetAsync();
            var resourceGroupData = new Azure.ResourceManager.Resources.ResourceGroupData(new AzureLocation(location));
            
            var operation = await subscription.Value.GetResourceGroups().CreateOrUpdateAsync(
                Azure.WaitUntil.Completed, 
                resourceGroupName, 
                resourceGroupData);

            Console.WriteLine($"Created resource group: {resourceGroupName}");
            return resourceGroupName;
        }

        static string RandomResourceName(string prefix = "")
        {
            Random random = new Random();
            return prefix + random.Next(1000, 9999).ToString();
        }

        static async Task AccountOperations()
        {
            Console.WriteLine("Basic Database Account Operations");
            Console.WriteLine("================================");
            Console.WriteLine($"Resource Group: {_resourceGroupName}");
            Console.WriteLine($"Location: {_location}");
            
            Console.WriteLine("\nDemonstrating Azure.ResourceManager.CosmosDB SDK usage:");
            Console.WriteLine("- Uses ArmClient instead of CosmosDBManagementClient");
            Console.WriteLine("- Uses Azure.Identity for authentication");
            Console.WriteLine("- Uses Resource Collections pattern");
            Console.WriteLine("- Different model classes and naming conventions");
            
            try
            {
                // List existing Cosmos DB accounts
                Console.WriteLine("\nListing Cosmos DB accounts in resource group...");
                await foreach (var account in _resourceGroup.GetCosmosDBAccounts())
                {
                    Console.WriteLine($"Account: {account.Data.Name}");
                    Console.WriteLine($"  Location: {account.Data.Location}");
                    Console.WriteLine($"  API Type: {GetApiType(account.Data)}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error listing accounts: {ex.Message}");
            }

            Console.WriteLine("\nPress any key to continue.");
            Console.ReadKey();
        }

        static async Task NoSqlOperations()
        {
            Console.WriteLine("Basic NoSQL API Operations");
            Console.WriteLine("=========================");
            Console.WriteLine("This demonstrates the pattern for NoSQL operations using the new SDK.");
            Console.WriteLine("Full implementation would include database and container operations.");
            
            Console.WriteLine("\nKey differences from the old SDK:");
            Console.WriteLine("- CosmosDBSqlDatabaseResource instead of SqlDatabaseGetResults");
            Console.WriteLine("- Different create/update patterns");
            Console.WriteLine("- Async enumerable patterns for listing");
            Console.WriteLine("- Resource-based navigation");

            Console.WriteLine("\nPress any key to continue.");
            Console.ReadKey();
        }

        private static string GetApiType(Azure.ResourceManager.CosmosDB.CosmosDBAccountData accountData)
        {
            if (accountData.Kind == Azure.ResourceManager.CosmosDB.Models.CosmosDBAccountKind.MongoDB)
                return "MongoDB";

            foreach (var capability in accountData.Capabilities)
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
    }

    class Secrets
    {
        public string ClientId { get; set; } = "";
        public string TenantId { get; set; } = "";
        public string ClientSecret { get; set; } = "";
        public string SubscriptionId { get; set; } = "";
    }
}