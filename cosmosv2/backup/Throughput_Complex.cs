using System;
using Azure.ResourceManager.CosmosDB.Models;

namespace cosmosv2
{
    static class Throughput
    {
        static public CosmosDBCreateUpdateConfig Create(int throughput, bool isAutoScale)
        {
            if (isAutoScale)
            {
                return new CosmosDBCreateUpdateConfig
                {
                    AutoscaleSettings = new CosmosDBAutoscaleSettings
                    {
                        MaxThroughput = throughput
                    }
                };
            }
            else
            {
                return new CosmosDBCreateUpdateConfig
                {
                    Throughput = throughput
                };
            }
        }

        static public void Print(object resource)
        {
            try
            {
                Console.WriteLine("Throughput information:");
                Console.WriteLine(resource.ToString());
            }
            catch 
            {
                Console.WriteLine("Error reading throughput information");
            }
        }

        static public CosmosDBCreateUpdateConfig Update(
            object resource,
            int throughput,
            bool autoScale = false)
        {
            Console.WriteLine($"Updating throughput to {throughput}, AutoScale: {autoScale}");

            if (autoScale == false) // manual throughput
            {
                return new CosmosDBCreateUpdateConfig
                {
                    Throughput = throughput
                };
            }
            else // autoscale
            {
                return new CosmosDBCreateUpdateConfig
                {
                    AutoscaleSettings = new CosmosDBAutoscaleSettings
                    {
                        MaxThroughput = throughput
                    }
                };
            }
        }
    }
}