using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using Azure.ResourceManager;
using Azure.ResourceManager.CosmosDB;
using Azure.ResourceManager.CosmosDB.Models;
using Azure.ResourceManager.Resources;

namespace cosmosv2
{
    public class NoSql
    {
        public async Task ManageNoSqlOperations(ArmClient armClient, SubscriptionResource subscription, ResourceGroupResource resourceGroup, string location)
        {
            bool exit = false;

            while (exit == false)
            {
                Console.Clear();
                Console.WriteLine($"NoSQL API Operations");
                Console.WriteLine($"-------------------");
                Console.WriteLine($"[a]   Create Database with Shared Autoscale Throughput");
                Console.WriteLine($"[b]   Create Database with no Throughput");
                Console.WriteLine($"[c]   Create Container with Dedicated Manual Throughput");
                Console.WriteLine($"[d]   Create Container with Dedicated Autoscale Throughput");
                Console.WriteLine($"[e]   List Databases");
                Console.WriteLine($"[f]   Get Database");
                Console.WriteLine($"[g]   List Containers");
                Console.WriteLine($"[h]   Get Container");
                Console.WriteLine($"[i]   Update Database Throughput");
                Console.WriteLine($"[j]   Update Container Throughput");
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
                    string databaseName = "database-" + new Random().Next(1000, 9999);
                    var database = await CreateDatabaseAsync(cosmosAccount.Value, databaseName, throughput: 1000, autoScale: true);
                    Console.WriteLine($"Created database with autoscale: {database.Data.Name}");
                    Console.WriteLine("Press any key to continue.");
                    Console.ReadKey();
                }
                else if (result.KeyChar == 'b')
                {
                    Console.Clear();
                    string databaseName = "database-" + new Random().Next(1000, 9999);
                    var database = await CreateDatabaseAsync(cosmosAccount.Value, databaseName);
                    Console.WriteLine($"Created database without throughput: {database.Data.Name}");
                    Console.WriteLine("Press any key to continue.");
                    Console.ReadKey();
                }
                else if (result.KeyChar == 'c')
                {
                    Console.Clear();
                    string databaseName = await SelectDatabase(cosmosAccount.Value);
                    if (!string.IsNullOrEmpty(databaseName))
                    {
                        string containerName = "container-" + new Random().Next(1000, 9999);
                        List<string> partitionKey = new List<string> { "/partitionKey" };
                        var container = await CreateContainerAsync(cosmosAccount.Value, databaseName, containerName, partitionKey, throughput: 400, autoScale: false);
                        Console.WriteLine($"Created container with manual throughput: {container.Data.Name}");
                        Console.WriteLine("Press any key to continue.");
                        Console.ReadKey();
                    }
                }
                else if (result.KeyChar == 'd')
                {
                    Console.Clear();
                    string databaseName = await SelectDatabase(cosmosAccount.Value);
                    if (!string.IsNullOrEmpty(databaseName))
                    {
                        string containerName = "container-" + new Random().Next(1000, 9999);
                        List<string> partitionKey = new List<string> { "/partitionKey" };
                        var container = await CreateContainerAsync(cosmosAccount.Value, databaseName, containerName, partitionKey, throughput: 1000, autoScale: true);
                        Console.WriteLine($"Created container with autoscale: {container.Data.Name}");
                        Console.WriteLine("Press any key to continue.");
                        Console.ReadKey();
                    }
                }
                else if (result.KeyChar == 'e')
                {
                    Console.Clear();
                    List<string> databases = await ListDatabasesAsync(cosmosAccount.Value);
                    Console.WriteLine("Databases:");
                    foreach (string db in databases)
                    {
                        Console.WriteLine($"  {db}");
                    }
                    Console.WriteLine("Press any key to continue.");
                    Console.ReadKey();
                }
                else if (result.KeyChar == 'f')
                {
                    Console.Clear();
                    string databaseName = await SelectDatabase(cosmosAccount.Value);
                    if (!string.IsNullOrEmpty(databaseName))
                    {
                        await GetDatabaseAsync(cosmosAccount.Value, databaseName);
                    }
                }
                else if (result.KeyChar == 'g')
                {
                    Console.Clear();
                    string databaseName = await SelectDatabase(cosmosAccount.Value);
                    if (!string.IsNullOrEmpty(databaseName))
                    {
                        List<string> containers = await ListContainersAsync(cosmosAccount.Value, databaseName);
                        Console.WriteLine("Containers:");
                        foreach (string container in containers)
                        {
                            Console.WriteLine($"  {container}");
                        }
                        Console.WriteLine("Press any key to continue.");
                        Console.ReadKey();
                    }
                }
                else if (result.KeyChar == 'h')
                {
                    Console.Clear();
                    string databaseName = await SelectDatabase(cosmosAccount.Value);
                    if (!string.IsNullOrEmpty(databaseName))
                    {
                        string containerName = await SelectContainer(cosmosAccount.Value, databaseName);
                        if (!string.IsNullOrEmpty(containerName))
                        {
                            await GetContainerAsync(cosmosAccount.Value, databaseName, containerName);
                        }
                    }
                }
                else if (result.KeyChar == 'x')
                {
                    exit = true;
                }
            }
        }

        public async Task<CosmosDBSqlDatabaseResource> CreateDatabaseAsync(
            CosmosDBAccountResource cosmosAccount,
            string databaseName,
            int? throughput = null,
            bool? autoScale = false)
        {
            var createOptions = new CosmosDBSqlDatabaseCreateOrUpdateContent(
                new CosmosDBSqlDatabaseResourceInfo(databaseName));

            if (throughput != null)
            {
                createOptions.Options = Throughput.Create(throughput.Value, autoScale.Value);
            }

            var operation = await cosmosAccount.GetCosmosDBSqlDatabases().CreateOrUpdateAsync(
                Azure.WaitUntil.Completed,
                databaseName,
                createOptions);

            return operation.Value;
        }

        public async Task<List<string>> ListDatabasesAsync(CosmosDBAccountResource cosmosAccount)
        {
            var sqlDatabases = cosmosAccount.GetCosmosDBSqlDatabasesAsync();
            List<string> databaseNames = new List<string>();

            await foreach (var sqlDatabase in sqlDatabases)
            {
                databaseNames.Add(sqlDatabase.Data.Resource.DatabaseName);
            }

            return databaseNames;
        }

        public async Task<CosmosDBSqlDatabaseResource> GetDatabaseAsync(
            CosmosDBAccountResource cosmosAccount,
            string databaseName)
        {
            var sqlDatabase = await cosmosAccount.GetCosmosDBSqlDatabaseAsync(databaseName);

            Console.WriteLine($"Resource Id: {sqlDatabase.Value.Data.Id}");
            Console.WriteLine($"Database Name: {sqlDatabase.Value.Data.Resource.DatabaseName}");

            try
            {
                var throughputResponse = await sqlDatabase.Value.GetCosmosDBSqlDatabaseThroughputSettingAsync();
                Console.WriteLine("\nDatabase Throughput\n-----------------------");
                Throughput.Print(throughputResponse.Value.Data);
            }
            catch
            {
                Console.WriteLine("Database throughput not set");
            }

            Console.WriteLine("Press any key to continue.");
            Console.ReadKey();

            return sqlDatabase.Value;
        }

        public async Task<CosmosDBSqlContainerResource> CreateContainerAsync(
            CosmosDBAccountResource cosmosAccount,
            string databaseName,
            string containerName,
            List<string> partitionKey,
            int? throughput = null,
            bool? autoScale = false)
        {
            var containerResource = new CosmosDBSqlContainerResourceInfo(containerName)
            {
                PartitionKey = new CosmosDBContainerPartitionKey
                {
                    Paths = { partitionKey[0] },
                    Kind = CosmosDBPartitionKind.Hash
                },
                IndexingPolicy = new CosmosDBIndexingPolicy
                {
                    IsAutomatic = true,
                    IndexingMode = CosmosDBIndexingMode.Consistent,
                    IncludedPaths =
                    {
                        new CosmosDBIncludedPath { Path = "/*" }
                    },
                    ExcludedPaths =
                    {
                        new CosmosDBExcludedPath { Path = "/myPathToNotIndex/*" },
                        new CosmosDBExcludedPath { Path = "/myPropertyToNotIndex/?" },
                        new CosmosDBExcludedPath { Path = "/_etag/?" }
                    }
                },
                UniqueKeyPolicy = new CosmosDBUniqueKeyPolicy
                {
                    UniqueKeys =
                    {
                        new CosmosDBUniqueKey
                        {
                            Paths = { "/myUniqueKey1", "/myUniqueKey2" }
                        }
                    }
                },
                ConflictResolutionPolicy = new CosmosDBConflictResolutionPolicy
                {
                    Mode = CosmosDBConflictResolutionMode.LastWriterWins,
                    ConflictResolutionPath = "/myConflictResolverPath"
                }
            };

            var createOptions = new CosmosDBSqlContainerCreateOrUpdateContent(containerResource);

            if (throughput != null)
            {
                createOptions.Options = Throughput.Create(throughput.Value, autoScale.Value);
            }

            var database = await cosmosAccount.GetCosmosDBSqlDatabaseAsync(databaseName);
            var operation = await database.Value.GetCosmosDBSqlContainers().CreateOrUpdateAsync(
                Azure.WaitUntil.Completed,
                containerName,
                createOptions);

            return operation.Value;
        }

        public async Task<List<string>> ListContainersAsync(
            CosmosDBAccountResource cosmosAccount,
            string databaseName)
        {
            var database = await cosmosAccount.GetCosmosDBSqlDatabaseAsync(databaseName);
            var sqlContainers = database.Value.GetCosmosDBSqlContainersAsync();

            List<string> containerNames = new List<string>();

            await foreach (var sqlContainer in sqlContainers)
            {
                containerNames.Add(sqlContainer.Data.Resource.ContainerName);
            }

            return containerNames;
        }

        public async Task<CosmosDBSqlContainerResource> GetContainerAsync(
            CosmosDBAccountResource cosmosAccount,
            string databaseName,
            string containerName)
        {
            var database = await cosmosAccount.GetCosmosDBSqlDatabaseAsync(databaseName);
            var sqlContainer = await database.Value.GetCosmosDBSqlContainerAsync(containerName);

            Console.WriteLine($"Azure Resource Id: {sqlContainer.Value.Data.Id}");
            Console.WriteLine($"Container Name: {sqlContainer.Value.Data.Resource.ContainerName}");

            try
            {
                var throughputResponse = await sqlContainer.Value.GetCosmosDBSqlContainerThroughputSettingAsync();
                Console.WriteLine("\nContainer Throughput\n-----------------------");
                Throughput.Print(throughputResponse.Value.Data);
            }
            catch
            {
                Console.WriteLine("Container throughput not set");
            }

            Console.WriteLine($"Partition Key: {sqlContainer.Value.Data.Resource.PartitionKey?.Paths?.FirstOrDefault()}");
            Console.WriteLine($"Indexing Mode: {sqlContainer.Value.Data.Resource.IndexingPolicy?.IndexingMode}");
            
            Console.WriteLine("Press any key to continue.");
            Console.ReadKey();

            return sqlContainer.Value;
        }

        private async Task<string> SelectAccount(ResourceGroupResource resourceGroup)
        {
            var accounts = new List<string>();
            await foreach (var account in resourceGroup.GetCosmosDBAccountsAsync())
            {
                accounts.Add(account.Data.Name);
            }

            if (accounts.Count == 0)
            {
                Console.WriteLine("No Cosmos DB accounts found in resource group.");
                Console.WriteLine("Press any key to continue.");
                Console.ReadKey();
                return "";
            }

            Console.WriteLine("Select a Cosmos DB account:");
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

        private async Task<string> SelectDatabase(CosmosDBAccountResource cosmosAccount)
        {
            var databases = await ListDatabasesAsync(cosmosAccount);

            if (databases.Count == 0)
            {
                Console.WriteLine("No databases found in account.");
                Console.WriteLine("Press any key to continue.");
                Console.ReadKey();
                return "";
            }

            Console.WriteLine("Select a database:");
            for (int i = 0; i < databases.Count; i++)
            {
                Console.WriteLine($"[{i + 1}] {databases[i]}");
            }

            Console.Write("Enter selection (1-" + databases.Count + "): ");
            if (int.TryParse(Console.ReadLine(), out int selection) && selection > 0 && selection <= databases.Count)
            {
                return databases[selection - 1];
            }

            return "";
        }

        private async Task<string> SelectContainer(CosmosDBAccountResource cosmosAccount, string databaseName)
        {
            var containers = await ListContainersAsync(cosmosAccount, databaseName);

            if (containers.Count == 0)
            {
                Console.WriteLine("No containers found in database.");
                Console.WriteLine("Press any key to continue.");
                Console.ReadKey();
                return "";
            }

            Console.WriteLine("Select a container:");
            for (int i = 0; i < containers.Count; i++)
            {
                Console.WriteLine($"[{i + 1}] {containers[i]}");
            }

            Console.Write("Enter selection (1-" + containers.Count + "): ");
            if (int.TryParse(Console.ReadLine(), out int selection) && selection > 0 && selection <= containers.Count)
            {
                return containers[selection - 1];
            }

            return "";
        }
    }
}