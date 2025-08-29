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
                    Console.WriteLine($"[e]   Database Account Operations");
                    Console.WriteLine($"[f]   Cassandra API Operations");
                    Console.WriteLine($"[g]   Gremlin API Operations");
                    Console.WriteLine($"[h]   MongoDB API Operations");
                    Console.WriteLine($"[i]   NoSQL API Operations");
                    Console.WriteLine($"[j]   Table API Operations");
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
                        await Account(_armClient, _subscription, _resourceGroup, _location);
                    }
                    else if (result.KeyChar == 'f')
                    {
                        Console.Clear();
                        await Cassandra(_armClient, _subscription, _resourceGroup, _location);
                    }
                    else if (result.KeyChar == 'g')
                    {
                        Console.Clear();
                        await Gremlin(_armClient, _subscription, _resourceGroup, _location);
                    }
                    else if (result.KeyChar == 'h')
                    {
                        Console.Clear();
                        await MongoDB(_armClient, _subscription, _resourceGroup, _location);
                    }
                    else if (result.KeyChar == 'i')
                    {
                        Console.Clear();
                        await NoSql(_armClient, _subscription, _resourceGroup, _location);
                    }
                    else if (result.KeyChar == 'j')
                    {
                        Console.Clear();
                        await Table(_armClient, _subscription, _resourceGroup, _location);
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

        // Placeholder methods for different API operations - these will be implemented
        static async Task Account(ArmClient armClient, SubscriptionResource subscription, ResourceGroupResource resourceGroup, string location)
        {
            DatabaseAccount account = new DatabaseAccount();
            await account.ManageAccountOperations(armClient, subscription, resourceGroup, location);
        }

        static async Task NoSql(ArmClient armClient, SubscriptionResource subscription, ResourceGroupResource resourceGroup, string location)
        {
            NoSql noSql = new NoSql();
            await noSql.ManageNoSqlOperations(armClient, subscription, resourceGroup, location);
        }

        static async Task MongoDB(ArmClient armClient, SubscriptionResource subscription, ResourceGroupResource resourceGroup, string location)
        {
            MongoDB mongoDB = new MongoDB();
            await mongoDB.ManageMongoDBOperations(armClient, subscription, resourceGroup, location);
        }

        static async Task Cassandra(ArmClient armClient, SubscriptionResource subscription, ResourceGroupResource resourceGroup, string location)
        {
            Cassandra cassandra = new Cassandra();
            await cassandra.ManageCassandraOperations(armClient, subscription, resourceGroup, location);
        }

        static async Task Gremlin(ArmClient armClient, SubscriptionResource subscription, ResourceGroupResource resourceGroup, string location)
        {
            Gremlin gremlin = new Gremlin();
            await gremlin.ManageGremlinOperations(armClient, subscription, resourceGroup, location);
        }

        static async Task Table(ArmClient armClient, SubscriptionResource subscription, ResourceGroupResource resourceGroup, string location)
        {
            Table table = new Table();
            await table.ManageTableOperations(armClient, subscription, resourceGroup, location);
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