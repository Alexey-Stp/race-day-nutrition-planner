# Brand Filter Usage Examples

This document demonstrates how to use the brand filter feature in the nutrition plan API.

## Overview

The API supports filtering products by brand without specifying individual products. When using the filter with certain sports, automatic product type exclusions are applied.

## Automatic Exclusions by Sport

- **Run**: Automatically excludes `drink` and `recovery` types (runners typically don't carry bottles)
- **Bike**: No automatic exclusions (cyclists can easily carry bottles)
- **Triathlon**: No automatic exclusions (bike portion allows for carrying bottles)

## Example 1: SiS Brand with Run Sport

When requesting a nutrition plan for running with SiS brand products, drinks and recovery products are automatically excluded.

**Request:**
```bash
curl -X POST http://localhost:5208/api/plan/generate \
  -H "Content-Type: application/json" \
  -d '{
    "athleteWeightKg": 75,
    "sportType": "Run",
    "durationHours": 3,
    "temperatureC": 20,
    "intensity": "Moderate",
    "filter": {
      "brand": "SiS"
    }
  }'
```

**Result:** 
- Only gels and bars from SiS brand are included
- Drinks (e.g., "SiS GO Electrolyte Drink") are excluded
- Recovery products (e.g., "SiS REGO Rapid Recovery Drink") are excluded

## Example 2: SiS Brand with Bike Sport

For cycling, all SiS products including drinks are available.

**Request:**
```bash
curl -X POST http://localhost:5208/api/plan/generate \
  -H "Content-Type: application/json" \
  -d '{
    "athleteWeightKg": 75,
    "sportType": "Bike",
    "durationHours": 3,
    "temperatureC": 20,
    "intensity": "Moderate",
    "filter": {
      "brand": "SiS"
    }
  }'
```

**Result:** 
- All SiS products are available including gels, bars, and drinks
- The plan generator will select appropriate products based on nutrition targets

## Example 3: Explicit Type Exclusions

You can also explicitly exclude specific product types for any sport.

**Request:**
```bash
curl -X POST http://localhost:5208/api/plan/generate \
  -H "Content-Type: application/json" \
  -d '{
    "athleteWeightKg": 75,
    "sportType": "Bike",
    "durationHours": 3,
    "temperatureC": 20,
    "intensity": "Moderate",
    "filter": {
      "brand": "SiS",
      "excludeTypes": ["bar"]
    }
  }'
```

**Result:** 
- Only SiS gels and drinks are available
- Bars are excluded as specified

## Using Other Brands

The filter works with any brand in the product catalog:

```bash
curl -X POST http://localhost:5208/api/plan/generate \
  -H "Content-Type: application/json" \
  -d '{
    "athleteWeightKg": 75,
    "sportType": "Run",
    "durationHours": 2,
    "temperatureC": 18,
    "intensity": "Hard",
    "filter": {
      "brand": "Maurten"
    }
  }'
```

## Notes

- The brand filter is case-insensitive
- Multiple brands cannot be specified in a single request - use separate requests for different brands
- If no products match the filter criteria, the API returns a 400 Bad Request error
