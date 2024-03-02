using System.Dynamic;
using System.Globalization;
using AzureCostCli.Commands.AccumulatedCost;
using AzureCostCli.Commands.Budgets;
using AzureCostCli.Commands.CostByResource;
using AzureCostCli.Commands.CostByTag;
using AzureCostCli.Commands.DailyCost;
using AzureCostCli.Commands.DetectAnomaly;
using AzureCostCli.Commands.Regions;
using AzureCostCli.Commands.WhatIf;
using AzureCostCli.CostApi;
using CsvHelper;
using CsvHelper.Configuration;
using CsvHelper.TypeConversion;

namespace AzureCostCli.OutputFormatters;

public class CsvOutputFormatter : BaseOutputFormatter
{
    public override Task WriteAccumulatedCost(AccumulatedCostSettings settings, AccumulatedCostDetails accumulatedCostDetails)
    {
        return ExportToCsv(settings.SkipHeader, accumulatedCostDetails.Costs);
    }

    public override Task WriteCostByResource(CostByResourceSettings settings, IEnumerable<CostResourceItem> resources)
    {
        return ExportToCsv(settings.SkipHeader, resources);
    }
    
    public override Task WriteBudgets(BudgetsSettings settings, IEnumerable<BudgetItem> budgets)
    {
        return ExportToCsv(settings.SkipHeader, budgets);
    }

    public override Task WriteDailyCost(DailyCostSettings settings, IEnumerable<CostDailyItem> dailyCosts)
    {
        // code to create the column Tags only when needed
        // small trick with the records dailyCostItemWithoutTags, and dailyCostItem
        if (settings.IncludeTags == false)
        {
            var dailyCostsWithoutTags = new List<CostDailyItemWithoutTags>();
            foreach (var item in dailyCosts)
            {
                var newItem = new CostDailyItemWithoutTags(
                    Name: item.Name,
                    Date: item.Date,
                    Cost: item.Cost,
                    Currency: item.Currency,
                    CostUsd: item.CostUsd
                );
                dailyCostsWithoutTags.Add(newItem);
            }
            return ExportToCsv(settings.SkipHeader, dailyCostsWithoutTags); 
        }
        else 
        {
            return ExportToCsv(settings.SkipHeader, dailyCosts);
        }
    }

    public override Task WriteDailyCostExpanded(DailyCostSettings settings, IEnumerable<CostDailyItemExpanded> dailyCosts)
    {
        // code to create the column Tags only when needed
        // small trick with the records dailyCostItemWithoutTags, and dailyCostItem
        if (settings.IncludeTags == false)
        {
            var dailyCostsWithoutTags = new List<CostDailyItemExpandedWithoutTags>();
            foreach (var item in dailyCosts)
            {
                var newItem = new CostDailyItemExpandedWithoutTags(
                    SubscriptionId: item.SubscriptionId,
                    SubscriptionName: item.SubscriptionName,
                    ResourceGroupName: item.ResourceGroupName,
                    ResourceType: item.ResourceType,
                    Name: item.Name,
                    Date: item.Date,
                    Cost: item.Cost,
                    Currency: item.Currency,
                    CostUsd: item.CostUsd
                );
                dailyCostsWithoutTags.Add(newItem);
            }
            return ExportToCsv(settings.SkipHeader, dailyCostsWithoutTags);
        }
        else
        {
            return ExportToCsv(settings.SkipHeader, dailyCosts);
        }
    }

    public override Task WriteAnomalyDetectionResults(DetectAnomalySettings settings, List<AnomalyDetectionResult> anomalies)
    {
        return ExportToCsv(settings.SkipHeader, anomalies);
    }

    public override Task WriteRegions(RegionsSettings settings, IReadOnlyCollection<AzureRegion> regions)
    {
        return ExportToCsv(settings.SkipHeader, regions);
    }

    public override Task WriteCostByTag(CostByTagSettings settings, Dictionary<string, Dictionary<string, List<CostResourceItem>>> byTags)
    {
        // Flatten the hierarchy to a single list, including the tag and value
        var resourcesWithTagAndValue = new List<dynamic>();
        foreach (var (tag, value) in byTags)
        {
            foreach (var (tagValue, resources) in value)
            {
                foreach (var resource in resources)
                {
                    dynamic expando = new ExpandoObject();
                    expando.Tag = tag;
                    expando.Value = tagValue;
                    expando.ResourceId = resource.ResourceId;
                    expando.ResourceType = resource.ResourceType;
                    expando.ResourceGroup = resource.ResourceGroupName;
                    expando.ResourceLocation = resource.ResourceLocation;
                    expando.Cost = resource.Cost;
                    expando.Currency = resource.Currency;
                    expando.CostUsd = resource.CostUSD;
                    
                    resourcesWithTagAndValue.Add(expando);
                }
            }
        }
      
        
        return ExportToCsv(settings.SkipHeader, resourcesWithTagAndValue);
    }

 public override Task WritePricesPerRegion(WhatIfSettings settings, Dictionary<UsageDetails, List<PriceRecord>> pricesByRegion)
{
    // Flatten the dictionary to a single list
    // We need to end up with the properties of the CostResourceItem and then each column for each region in the pricesByRegion

    // Get the list of regions
    var regions = pricesByRegion.Select(a => a.Value.Select(b => b.Location)).SelectMany(a => a).Distinct().OrderBy(a => a).ToList();

    // Create the list of properties for the CSV
    var properties = typeof(UsageDetails).GetProperties().Select(a => a.Name).ToList();
    properties.AddRange(regions);

    // Create the list of objects to be written to the CSV
    var resources = new List<dynamic>();
    foreach (var (resource, prices) in pricesByRegion)
    {
        dynamic expando = new ExpandoObject();
        foreach (var property in typeof(UsageDetails).GetProperties())
        {
            ((IDictionary<string, object>)expando)[property.Name] = property.GetValue(resource);
        }

        foreach (var region in regions)
        {
            var price = prices.FirstOrDefault(a => a.Location == region);
            ((IDictionary<string, object>)expando)[region] = price?.RetailPrice;
        }

        resources.Add(expando);
    }

    // Write the CSV
    return ExportToCsv(settings.SkipHeader, resources);
}


    

    private static Task ExportToCsv(bool skipHeader, IEnumerable<object> resources)
    {
        var config = new CsvConfiguration(CultureInfo.CurrentCulture)
        {
            HasHeaderRecord = skipHeader == false
        };
        
        using (var writer = new StringWriter())
        using (var csv = new CsvWriter(writer, config))
        {
            csv.Context.TypeConverterCache.AddConverter<double>(new CustomDoubleConverter());
            csv.WriteRecords(resources);

            Console.Write(writer.ToString());
        }

        return Task.CompletedTask;
    }

   
    
    
}

public class CustomDoubleConverter : DoubleConverter
{
    public override string ConvertToString(object value, IWriterRow row, MemberMapData memberMapData)
    {
        double number = (double)value;
        return number.ToString("F8", CultureInfo.InvariantCulture);
    }
}