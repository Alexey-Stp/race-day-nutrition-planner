using System.Globalization;
using System.Text;
using RaceDay.Core.Models;

namespace RaceDay.Core.Utilities;

/// <summary>
/// Shopping summary item
/// </summary>
/// <param name="ProductName">Product name</param>
/// <param name="TotalPortions">Portions consumed (fractional for sipped drinks)</param>
/// <param name="TotalCarbs">Carbs delivered by this product across the plan (g)</param>
public record ShoppingItem(
    string ProductName,
    double TotalPortions,
    double TotalCarbs
);

/// <summary>
/// Shopping list summary
/// </summary>
/// <param name="Items">Per-product totals, ordered by name</param>
/// <param name="TotalProductCount">Units to buy: whole portions rounded up per product (sips → bottles)</param>
/// <param name="TotalCarbs">Total carbs across the plan (g)</param>
public record ShoppingSummary(
    IReadOnlyList<ShoppingItem> Items,
    int TotalProductCount,
    double TotalCarbs
);

/// <summary>
/// Extension methods for calculating shopping summaries from nutrition plans
/// </summary>
public static class ShoppingListExtensions
{
    /// <summary>
    /// Calculate shopping list from nutrition events.
    /// Groups products by name and sums portions and the carbs each event actually delivers.
    /// </summary>
    public static ShoppingSummary CalculateShoppingList(this IEnumerable<NutritionEvent> nutritionEvents)
    {
        var events = nutritionEvents.ToList();
        if (events.Count == 0)
            return new ShoppingSummary(Array.Empty<ShoppingItem>(), 0, 0);

        var items = events
            .GroupBy(e => e.ProductName)
            .Select(group => new ShoppingItem(
                ProductName: group.Key,
                TotalPortions: group.Sum(e => e.AmountPortions),
                TotalCarbs: group.Sum(e => e.CarbsInEvent)))
            .OrderBy(item => item.ProductName, StringComparer.Ordinal)
            .ToList();

        // Each product is bought in whole units: 2.3 bottles' worth of sips means 3 bottles.
        int totalProductCount = items.Sum(item => (int)Math.Ceiling(Math.Round(item.TotalPortions, 6)));
        double totalCarbs = events.Sum(e => e.CarbsInEvent);

        return new ShoppingSummary(items, totalProductCount, totalCarbs);
    }

    /// <summary>
    /// Format shopping list for display or export
    /// </summary>
    public static string FormatShoppingList(this ShoppingSummary summary)
    {
        if (summary.Items.Count == 0)
            return "No items to purchase";

        var culture = CultureInfo.InvariantCulture;
        var itemsList = string.Join("\n", summary.Items.Select(item => string.Format(culture,
            "• {0}: {1:F1} portion(s) ({2:F0}g carbs)", item.ProductName, item.TotalPortions, item.TotalCarbs)));

        return string.Format(culture, "{0}\n\nTotal: {1} items • {2:F0}g carbs",
            itemsList, summary.TotalProductCount, summary.TotalCarbs);
    }

    /// <summary>
    /// Format shopping list as CSV for export (RFC 4180 quoting, invariant culture).
    /// </summary>
    public static string FormatShoppingListCsv(this ShoppingSummary summary)
    {
        var culture = CultureInfo.InvariantCulture;
        var csv = new StringBuilder("Product,Portions,Carbs (g)");

        foreach (var item in summary.Items)
        {
            csv.Append('\n')
               .Append(Quote(item.ProductName)).Append(',')
               .Append(item.TotalPortions.ToString("F1", culture)).Append(',')
               .Append(item.TotalCarbs.ToString("F0", culture));
        }

        csv.Append('\n')
           .Append("TOTAL,")
           .Append(summary.TotalProductCount.ToString(culture)).Append(',')
           .Append(summary.TotalCarbs.ToString("F0", culture));

        return csv.ToString();
    }

    private static string Quote(string value) => "\"" + value.Replace("\"", "\"\"") + "\"";
}
