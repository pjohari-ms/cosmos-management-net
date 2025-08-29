using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using Azure.ResourceManager;
using Azure.ResourceManager.CosmosDB;
using Azure.ResourceManager.CosmosDB.Models;
using Azure.ResourceManager.Resources;

namespace cosmosv2
{
    public class Cassandra
    {
        public async Task ManageCassandraOperations(ArmClient armClient, SubscriptionResource subscription, ResourceGroupResource resourceGroup, string location)
        {
            bool exit = false;

            while (exit == false)
            {
                Console.Clear();
                Console.WriteLine($"Cassandra API Operations");
                Console.WriteLine($"-----------------------");
                Console.WriteLine($"[a]   Create Keyspace with Shared Autoscale Throughput");
                Console.WriteLine($"[b]   Create Keyspace with no Throughput");
                Console.WriteLine($"[c]   Create Table with Dedicated Manual Throughput");
                Console.WriteLine($"[d]   Create Table with Dedicated Autoscale Throughput");
                Console.WriteLine($"[e]   List Keyspaces");
                Console.WriteLine($"[f]   Get Keyspace");
                Console.WriteLine($"[g]   List Tables");
                Console.WriteLine($"[h]   Get Table");
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
                    string keyspaceName = "keyspace-" + new Random().Next(1000, 9999);
                    var keyspace = await CreateKeyspaceAsync(cosmosAccount.Value, keyspaceName, throughput: 1000, autoScale: true);
                    Console.WriteLine($"Created Cassandra keyspace with autoscale: {keyspace.Data.Name}");
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

        public async Task<CosmosDBSqlDatabaseResource> CreateKeyspaceAsync(
            CosmosDBAccountResource cosmosAccount,
            string keyspaceName,
            int? throughput = null,
            bool? autoScale = false)
        {
            var createOptions = new CosmosDBCassandraKeyspaceCreateOrUpdateContent(
                new CosmosDBCassandraKeyspaceResourceInfo(keyspaceName));

            if (throughput != null)
            {
                createOptions.Options = Throughput.Create(throughput.Value, autoScale.Value);
            }

            var operation = await cosmosAccount.GetCosmosDBCassandraKeyspaces().CreateOrUpdateAsync(
                Azure.WaitUntil.Completed,
                keyspaceName,
                createOptions);

            return null!;
        }

        private async Task<string> SelectAccount(ResourceGroupResource resourceGroup)
        {
            var accounts = new List<string>();
            await foreach (var account in resourceGroup.GetCosmosDBAccountsAsync())
            {
                // Only show accounts that have Cassandra capability
                bool hasCassandra = false;
                foreach (var capability in account.Data.Capabilities)
                {
                    if (capability.Name == "EnableCassandra")
                    {
                        hasCassandra = true;
                        break;
                    }
                }
                if (hasCassandra)
                {
                    accounts.Add(account.Data.Name);
                }
            }

            if (accounts.Count == 0)
            {
                Console.WriteLine("No Cassandra-enabled Cosmos DB accounts found in resource group.");
                Console.WriteLine("Press any key to continue.");
                Console.ReadKey();
                return "";
            }

            Console.WriteLine("Select a Cassandra-enabled Cosmos DB account:");
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