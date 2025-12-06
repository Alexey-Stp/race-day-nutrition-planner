using RaceDay.Core.Models;

namespace RaceDay.Core.Utilities;

/// <summary>
/// Shopping summary item
/// </summary>
public record ShoppingItem(
    string ProductName,
    double TotalPortions,
    double TotalCarbs
);

/// <summary>
/// Shopping list summary
/// </summary>
public record ShoppingSummary(
    List<ShoppingItem> Items,
    int TotalProductCount,
    double TotalCarbs
);

/// <summary>
/// Extension methods for calculating shopping summaries from nutrition plans
/// </summary>
public static class ShoppingListExtensions
{
    /// <summary>
    /// Calculate shopping list from nutrition events
    /// Groups products by name and sums quantities and carbs
    /// </summary>
    public static ShoppingSummary CalculateShoppingList(this IEnumerable<NutritionEvent> nutritionEvents)
    {
        var events = nutritionEvents.ToList();
        
        if (events.Count == 0)
        {
            return new ShoppingSummary(new List<ShoppingItem>(), 0, 0);
        }

        // Group by product name and sum portions
        var itemsMap = events
            .GroupBy(e => e.ProductName)
            .Select(group => new ShoppingItem(
                ProductName: group.Key,
                TotalPortions: group.Sum(e => e.AmountPortions),
                TotalCarbs: group.Sum(e => e.AmountPortions) // Simplified: carbs per portion
            ))
            .OrderBy(item => item.ProductName)
            .ToList();

        var totalProductCount = (int)events.Sum(e => e.AmountPortions);
        var totalCarbs = events[events.Count - 1].TotalCarbsSoFar;

        return new ShoppingSummary(
            Items: itemsMap,
            TotalProductCount: totalProductCount,
            TotalCarbs: totalCarbs
        );
    }

    /// <summary>
    /// Format shopping list for display or export
    /// </summary>
    public static string FormatShoppingList(this ShoppingSummary summary)
    {
        if (summary.Items.Count == 0)
        {
            return "No items to purchase";
        }

        var itemsList = string.Join("\n", summary.Items
            .Select(item => $"• {item.ProductName}: {item.TotalPortions:F1} portion(s) ({item.TotalCarbs:F0}g carbs)"));

        return $"{itemsList}\n\nTotal: {summary.TotalProductCount} items • {summary.TotalCarbs:F0}g carbs";
    }

    /// <summary>
    /// Format shopping list as CSV for export
    /// </summary>
    public static string FormatShoppingListCsv(this ShoppingSummary summary)
    {
        var csv = "Product,Portions,Carbs (g)\n";
        
        csv += string.Join("\n", summary.Items
            .Select(item => $"\"{item.ProductName}\",{item.TotalPortions:F1},{item.TotalCarbs:F0}"));

        csv += $"\nTOTAL,{summary.TotalProductCount},{summary.TotalCarbs:F0}";

        return csv;
    }
}
