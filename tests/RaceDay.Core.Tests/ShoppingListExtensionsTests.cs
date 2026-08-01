namespace RaceDay.Core.Tests;

using RaceDay.Core.Models;
using RaceDay.Core.Utilities;

public class ShoppingListExtensionsTests
{
    // Helper to build a NutritionEvent
    private static NutritionEvent MakeEvent(string product, double portions, double totalCarbsSoFar, double carbsInEvent = 0) =>
        new NutritionEvent(
            TimeMin: 10,
            Phase: RacePhase.Run,
            PhaseDescription: "Run",
            ProductName: product,
            AmountPortions: portions,
            Action: "Squeeze",
            TotalCarbsSoFar: totalCarbsSoFar,
            CarbsInEvent: carbsInEvent
        );

    // ── CalculateShoppingList ────────────────────────────────────────────────

    [Fact]
    public void CalculateShoppingList_EmptyEvents_ReturnsEmptySummary()
    {
        var summary = Enumerable.Empty<NutritionEvent>().CalculateShoppingList();

        summary.Items.ShouldBeEmpty();
        summary.TotalProductCount.ShouldBe(0);
        summary.TotalCarbs.ShouldBe(0);
    }

    [Fact]
    public void CalculateShoppingList_SingleEvent_ReturnsSingleItem()
    {
        var events = new[] { MakeEvent("Gel A", 1.0, 25.0, 25.0) };

        var summary = events.CalculateShoppingList();

        summary.Items.Count.ShouldBe(1);
        summary.Items[0].ProductName.ShouldBe("Gel A");
        summary.TotalProductCount.ShouldBe(1);
    }

    [Fact]
    public void CalculateShoppingList_MultipleEventsSameProduct_GroupsIntoOne()
    {
        var events = new[]
        {
            MakeEvent("Gel A", 1.0, 25.0, 25.0),
            MakeEvent("Gel A", 1.0, 50.0, 25.0),
        };

        var summary = events.CalculateShoppingList();

        summary.Items.Count.ShouldBe(1);
        summary.Items[0].TotalPortions.ShouldBe(2.0);
    }

    [Fact]
    public void CalculateShoppingList_MultipleEventsDifferentProducts_ReturnsSortedByName()
    {
        var events = new[]
        {
            MakeEvent("Gel B", 1.0, 25.0, 25.0),
            MakeEvent("Gel A", 1.0, 50.0, 25.0),
        };

        var summary = events.CalculateShoppingList();

        summary.Items.Count.ShouldBe(2);
        summary.Items[0].ProductName.ShouldBe("Gel A");
        summary.Items[1].ProductName.ShouldBe("Gel B");
    }

    [Fact]
    public void CalculateShoppingList_TotalProductCount_SumsAllPortions()
    {
        var events = new[]
        {
            MakeEvent("Gel A", 2.0, 50.0, 50.0),
            MakeEvent("Gel B", 3.0, 125.0, 75.0),
        };

        var summary = events.CalculateShoppingList();

        summary.TotalProductCount.ShouldBe(5); // 2 + 3
    }

    [Fact]
    public void CalculateShoppingList_TotalCarbs_TakenFromLastEventCumulativeTotal()
    {
        var events = new[]
        {
            MakeEvent("Gel A", 1.0, 25.0, 25.0),
            MakeEvent("Gel B", 1.0, 60.0, 35.0),
        };

        var summary = events.CalculateShoppingList();

        summary.TotalCarbs.ShouldBe(60.0);
    }

    // ── FormatShoppingList ───────────────────────────────────────────────────

    [Fact]
    public void FormatShoppingList_EmptyItems_ReturnsNoItemsMessage()
    {
        var summary = new ShoppingSummary(new List<ShoppingItem>(), 0, 0);

        var result = summary.FormatShoppingList();

        result.ShouldBe("No items to purchase");
    }

    [Fact]
    public void FormatShoppingList_WithItems_ContainsProductName()
    {
        var items = new List<ShoppingItem>
        {
            new("Gel A", 2.0, 50.0)
        };
        var summary = new ShoppingSummary(items, 2, 50.0);

        var result = summary.FormatShoppingList();

        result.ShouldContain("Gel A");
        result.ShouldContain("2.0");
    }

    [Fact]
    public void FormatShoppingList_WithItems_ContainsTotalsLine()
    {
        var items = new List<ShoppingItem>
        {
            new("Gel A", 3.0, 75.0)
        };
        var summary = new ShoppingSummary(items, 3, 75.0);

        var result = summary.FormatShoppingList();

        result.ShouldContain("Total:");
    }

    // ── FormatShoppingListCsv ────────────────────────────────────────────────

    [Fact]
    public void FormatShoppingListCsv_AlwaysContainsHeader()
    {
        var summary = new ShoppingSummary(new List<ShoppingItem>(), 0, 0);

        var result = summary.FormatShoppingListCsv();

        result.ShouldContain("Product,Portions,Carbs (g)");
    }

    [Fact]
    public void FormatShoppingListCsv_WithItems_ContainsProductRow()
    {
        var items = new List<ShoppingItem> { new("Energy Gel", 2.0, 50.0) };
        var summary = new ShoppingSummary(items, 2, 50.0);

        var result = summary.FormatShoppingListCsv();

        result.ShouldContain("\"Energy Gel\"");
        result.ShouldContain("2.0");
    }

    [Fact]
    public void FormatShoppingListCsv_WithItems_ContainsTotalsRow()
    {
        var items = new List<ShoppingItem> { new("Gel A", 2.0, 50.0) };
        var summary = new ShoppingSummary(items, 2, 50.0);

        var result = summary.FormatShoppingListCsv();

        result.ShouldContain("TOTAL");
    }
}
