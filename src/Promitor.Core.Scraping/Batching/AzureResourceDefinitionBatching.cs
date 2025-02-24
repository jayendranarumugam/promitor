using System.Collections.Generic;
using System.Linq;
using GuardNet;
using Promitor.Core.Contracts;
using Promitor.Core.Scraping.Configuration.Model.Metrics;

namespace Promitor.Core.Scraping.Batching
{
    public static class AzureResourceDefinitionBatching
    {
        /// <summary>
        /// groups scrape definitions based on following conditions:
        /// 1. Definitions in a batch must target the same resource type 
        /// 2. Definitions in a batch must target the same Azure metric with identical dimensions
        /// 3. Definitions in a batch must have the same time granularity 
        /// 4. Batch size cannot exceed configured maximum 
        /// <see href="https://learn.microsoft.com/en-us/azure/azure-monitor/essentials/migrate-to-batch-api?tabs=individual-response#batching-restrictions"/> 
        /// </summary>
        public static List<BatchScrapeDefinition<IAzureResourceDefinition>> GroupScrapeDefinitions(IEnumerable<ScrapeDefinition<IAzureResourceDefinition>> allScrapeDefinitions, int maxBatchSize) 
        {
            // ReSharper disable PossibleMultipleEnumeration
            Guard.NotNull(allScrapeDefinitions, nameof(allScrapeDefinitions));

            return  allScrapeDefinitions.GroupBy(def => def.BuildScrapingBatchInfo()) 
                        .ToDictionary(group => group.Key, group => group.ToList()) // first pass to build batches that could exceed max 
                        .ToDictionary(group => group.Key, group => SplitScrapeDefinitionBatch(group.Value, maxBatchSize)) // split to right-sized batches 
                        .SelectMany(group => group.Value.Select(batch => new BatchScrapeDefinition<IAzureResourceDefinition>(group.Key, batch)))
                        .ToList(); // flatten 
        }

        /// <summary>
        /// splits the "raw" batch according to max batch size configured
        /// </summary>
        private static List<List<ScrapeDefinition<IAzureResourceDefinition>>> SplitScrapeDefinitionBatch(List<ScrapeDefinition<IAzureResourceDefinition>> batchToSplit, int maxBatchSize) 
        {
            // ReSharper disable PossibleMultipleEnumeration
            Guard.NotNull(batchToSplit, nameof(batchToSplit));
            
            int numNewGroups = ((batchToSplit.Count - 1) / maxBatchSize) + 1;

            return Enumerable.Range(0, numNewGroups)
                .Select(i => batchToSplit.Skip(i * maxBatchSize).Take(maxBatchSize).ToList())
                .ToList();
        }
    }
}