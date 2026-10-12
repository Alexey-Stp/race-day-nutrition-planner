namespace RaceDay.Core.Tests;

using RaceDay.Core.Constants;
using RaceDay.Core.Models;

public class ProductEffectivenessProfilesTests
{
    // ── GetMidpointWindow ────────────────────────────────────────────────────

    [Fact]
    public void GetMidpointWindow_Drink_ReturnsDrinkMidpoint()
    {
        int expected = (ProductEffectivenessProfiles.DrinkSipMinMin + ProductEffectivenessProfiles.DrinkSipMaxMin) / 2;

        var result = ProductEffectivenessProfiles.GetMidpointWindow(ProductTexture.Drink, 0);

        result.ShouldBe(expected);
    }

    [Fact]
    public void GetMidpointWindow_LightGel_ReturnsLightGelMidpoint()
    {
        int expected = (ProductEffectivenessProfiles.LightGelMinMin + ProductEffectivenessProfiles.LightGelMaxMin) / 2;

        var result = ProductEffectivenessProfiles.GetMidpointWindow(ProductTexture.LightGel, 18);

        result.ShouldBe(expected);
    }

    [Fact]
    public void GetMidpointWindow_Chew_ReturnsChewMidpoint()
    {
        int expected = (ProductEffectivenessProfiles.ChewMinMin + ProductEffectivenessProfiles.ChewMaxMin) / 2;

        var result = ProductEffectivenessProfiles.GetMidpointWindow(ProductTexture.Chew, 22);

        result.ShouldBe(expected);
    }

    [Fact]
    public void GetMidpointWindow_Bake_ReturnsBakeMidpoint()
    {
        int expected = (ProductEffectivenessProfiles.BakeMinMin + ProductEffectivenessProfiles.BakeMaxMin) / 2;

        var result = ProductEffectivenessProfiles.GetMidpointWindow(ProductTexture.Bake, 45);

        result.ShouldBe(expected);
    }

    [Fact]
    public void GetMidpointWindow_LargeGel_ReturnsDenseGelMidpoint()
    {
        // Large gel = carbsG >= LargeGelThresholdG (35)
        int expected = (ProductEffectivenessProfiles.DenseGelMinMin + ProductEffectivenessProfiles.DenseGelMaxMin) / 2;

        var result = ProductEffectivenessProfiles.GetMidpointWindow(ProductTexture.Gel, ProductEffectivenessProfiles.LargeGelThresholdG);

        result.ShouldBe(expected);
    }

    [Fact]
    public void GetMidpointWindow_StandardGel_ReturnsStandardGelMidpoint()
    {
        // Standard gel = carbsG < LargeGelThresholdG (35)
        int expected = (ProductEffectivenessProfiles.StandardGelMinMin + ProductEffectivenessProfiles.StandardGelMaxMin) / 2;

        var result = ProductEffectivenessProfiles.GetMidpointWindow(ProductTexture.Gel, 25);

        result.ShouldBe(expected);
    }

    // ── Constants sanity checks ──────────────────────────────────────────────

    [Fact]
    public void Constants_DrinkWindowMin_LessThanMax()
    {
        (ProductEffectivenessProfiles.DrinkSipMinMin < ProductEffectivenessProfiles.DrinkSipMaxMin).ShouldBeTrue();
    }

    [Fact]
    public void Constants_LargeGelThreshold_GreaterThanSmallGelThreshold()
    {
        (ProductEffectivenessProfiles.LargeGelThresholdG > ProductEffectivenessProfiles.SmallGelThresholdG).ShouldBeTrue();
    }

    [Fact]
    public void Constants_DrinkTimerMaxGap_GreaterThanThreshold()
    {
        (ProductEffectivenessProfiles.DrinkTimerMaxGapMin > ProductEffectivenessProfiles.DrinkTimerThresholdMin).ShouldBeTrue();
    }
}
