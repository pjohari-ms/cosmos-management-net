using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using Azure.ResourceManager;
using Azure.ResourceManager.CosmosDB;
using Azure.ResourceManager.CosmosDB.Models;
using Azure.ResourceManager.Resources;

namespace cosmosv2
{
    public class Table
    {
        public async Task ManageTableOperations(ArmClient armClient, SubscriptionResource subscription, ResourceGroupResource resourceGroup, string location)
        {
            bool exit = false;

            while (exit == false)
            {
                Console.Clear();
                Console.WriteLine($"Table API Operations");
                Console.WriteLine($"-------------------");
                Console.WriteLine($"[a]   Create Table with Manual Throughput");
                Console.WriteLine($"[b]   Create Table with Autoscale Throughput");
                Console.WriteLine($"[c]   List Tables");
                Console.WriteLine($"[d]   Get Table");
                Console.WriteLine($"[e]   Update Table Throughput");
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
                    string tableName = "table-" + new Random().Next(1000, 9999);
                    var table = await CreateTableAsync(cosmosAccount.Value, tableName, throughput: 400, autoScale: false);
                    Console.WriteLine($"Created Table with manual throughput: {table.Data.Name}");
                    Console.WriteLine("Press any key to continue.");
                    Console.ReadKey();
                }
                else if (result.KeyChar == 'b')
                {
                    Console.Clear();
                    string tableName = "table-" + new Random().Next(1000, 9999);
                    var table = await CreateTableAsync(cosmosAccount.Value, tableName, throughput: 1000, autoScale: true);
                    Console.WriteLine($"Created Table with autoscale throughput: {table.Data.Name}");
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

        public async Task<CosmosDBTableResource> CreateTableAsync(
            CosmosDBAccountResource cosmosAccount,
            string tableName,
            int? throughput = null,
            bool? autoScale = false)
        {
            var createOptions = new CosmosDBTableCreateOrUpdateContent(
                new CosmosDBTableResourceInfo(tableName));

            if (throughput != null)
            {
                createOptions.Options = Throughput.Create(throughput.Value, autoScale.Value);
            }

            var operation = await cosmosAccount.GetCosmosDBTables().CreateOrUpdateAsync(
                Azure.WaitUntil.Completed,
                tableName,
                createOptions);

            return operation.Value;
        }

        private async Task<string> SelectAccount(ResourceGroupResource resourceGroup)
        {
            var accounts = new List<string>();
            await foreach (var account in resourceGroup.GetCosmosDBAccountsAsync())
            {
                // Only show accounts that have Table capability
                bool hasTable = false;
                foreach (var capability in account.Data.Capabilities)
                {
                    if (capability.Name == "EnableTable")
                    {
                        hasTable = true;
                        break;
                    }
                }
                if (hasTable)
                {
                    accounts.Add(account.Data.Name);
                }
            }

            if (accounts.Count == 0)
            {
                Console.WriteLine("No Table-enabled Cosmos DB accounts found in resource group.");
                Console.WriteLine("Press any key to continue.");
                Console.ReadKey();
                return "";
            }

            Console.WriteLine("Select a Table-enabled Cosmos DB account:");
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