namespace AzureCostCli.CostApi;

public record CostDailyItem(DateOnly Date, string Name, double Cost, double CostUsd, string Currency, string Tags);
public record CostDailyItemWithoutTags(DateOnly Date, string Name, double Cost, double CostUsd, string Currency);
public record CostDailyItemExpanded(string SubscriptionId, string SubscriptionName, string ResourceGroupName, string ResourceType, DateOnly Date, string Name, double Cost, double CostUsd, string Currency, string Tags);
public record CostDailyItemExpandedWithoutTags(string SubscriptionId, string SubscriptionName, string ResourceGroupName, string ResourceType, DateOnly Date, string Name, double Cost, double CostUsd, string Currency);
