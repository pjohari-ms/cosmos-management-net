using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using Azure.ResourceManager;
using Azure.ResourceManager.CosmosDB;
using Azure.ResourceManager.CosmosDB.Models;
using Azure.ResourceManager.Resources;

namespace cosmosv2
{
    public class Gremlin
    {
        public async Task ManageGremlinOperations(ArmClient armClient, SubscriptionResource subscription, ResourceGroupResource resourceGroup, string location)
        {
            bool exit = false;

            while (exit == false)
            {
                Console.Clear();
                Console.WriteLine($"Gremlin API Operations");
                Console.WriteLine($"---------------------");
                Console.WriteLine($"[a]   Create Database with Shared Autoscale Throughput");
                Console.WriteLine($"[b]   Create Database with no Throughput");
                Console.WriteLine($"[c]   Create Graph with Dedicated Manual Throughput");
                Console.WriteLine($"[d]   Create Graph with Dedicated Autoscale Throughput");
                Console.WriteLine($"[e]   List Databases");
                Console.WriteLine($"[f]   Get Database");
                Console.WriteLine($"[g]   List Graphs");
                Console.WriteLine($"[h]   Get Graph");
                Console.WriteLine($"[x]   Exit");

                ConsoleKeyInfo result = Console.ReadKey(true);

                string accountName = await SelectAccount(resourceGroup);
                if (string.IsNullOrEmpty(accountName))
                {
                    if (result.KeyChar != 'x')
                        continue;
                }

                var cosmosAccount = await resourceGroup.GetCosmosDBAccountAsync(accountName);

                if (result.KeyChar == 'a')
                {
                    Console.Clear();
                    string databaseName = "gremlin-database-" + new Random().Next(1000, 9999);
                    var database = await CreateDatabaseAsync(cosmosAccount.Value, databaseName, throughput: 1000, autoScale: true);
                    Console.WriteLine($"Created Gremlin database with autoscale: {database.Data.Name}");
                    Console.WriteLine("Press any key to continue.");
                    Console.ReadKey();
                }
                else if (result.KeyChar == 'x')
                {
                    exit = true;
                }
                else
                {
                    Console.WriteLine("Feature not yet implemented in this sample.");
                    Console.WriteLine("Press any key to continue.");
                    Console.ReadKey();
                }
            }
        }

        public async Task<CosmosDBSqlDatabaseResource> CreateDatabaseAsync(
            CosmosDBAccountResource cosmosAccount,
            string databaseName,
            int? throughput = null,
            bool? autoScale = false)
        {
            var createOptions = new CosmosDBGremlinDatabaseCreateOrUpdateContent(
                new CosmosDBGremlinDatabaseResourceInfo(databaseName));

            if (throughput != null)
            {
                createOptions.Options = Throughput.Create(throughput.Value, autoScale.Value);
            }

            var operation = await cosmosAccount.GetCosmosDBGremlinDatabases().CreateOrUpdateAsync(
                Azure.WaitUntil.Completed,
                databaseName,
                createOptions);

            return null!;
        }

        private async Task<string> SelectAccount(ResourceGroupResource resourceGroup)
        {
            var accounts = new List<string>();
            await foreach (var account in resourceGroup.GetCosmosDBAccountsAsync())
            {
                // Only show accounts that have Gremlin capability
                bool hasGremlin = false;
                foreach (var capability in account.Data.Capabilities)
                {
                    if (capability.Name == "EnableGremlin")
                    {
                        hasGremlin = true;
                        break;
                    }
                }
                if (hasGremlin)
                {
                    accounts.Add(account.Data.Name);
                }
            }

            if (accounts.Count == 0)
            {
                Console.WriteLine("No Gremlin-enabled Cosmos DB accounts found in resource group.");
                Console.WriteLine("Press any key to continue.");
                Console.ReadKey();
                return "";
            }

            Console.WriteLine("Select a Gremlin-enabled Cosmos DB account:");
            for (int i = 0; i < accounts.Count; i++)
            {
                Console.WriteLine($"[{i + 1}] {accounts[i]}");
            }

            Console.Write("Enter selection (1-" + accounts.Count + "): ");
            if (int.TryParse(Console.ReadLine(), out int selection) && selection > 0 && selection <= accounts.Count)
            {
                return accounts[selection - 1];
            }

            return "";
        }
    }
}