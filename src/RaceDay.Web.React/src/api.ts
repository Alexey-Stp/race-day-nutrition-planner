import type { ProductInfo, ActivityInfo, RaceNutritionPlan, AthleteProfile, RaceProfile, ProductEditor, UIMetadata, TemperatureMetadata, IntensityMetadata, ConfigurationMetadata } from './types';

const API_BASE_URL = import.meta.env.VITE_API_URL || 'http://localhost:5208';

export const api = {
  // Products
  async getProducts(): Promise<ProductInfo[]> {
    const response = await fetch(`${API_BASE_URL}/api/products`);
    if (!response.ok) {
      throw new Error('Failed to fetch products');
    }
    return response.json();
  },

  async getProductsByType(type: string): Promise<ProductInfo[]> {
    const response = await fetch(`${API_BASE_URL}/api/products/type/${type}`);
    if (!response.ok) {
      throw new Error('Failed to fetch products by type');
    }
    return response.json();
  },

  async searchProducts(query: string): Promise<ProductInfo[]> {
    const response = await fetch(`${API_BASE_URL}/api/products/search?query=${encodeURIComponent(query)}`);
    if (!response.ok) {
      throw new Error('Failed to search products');
    }
    return response.json();
  },

  async getBrands(): Promise<string[]> {
    const products = await this.getProducts();
    const brands = Array.from(new Set(products.map(p => p.brand))).sort();
    return brands;
  },

  // Activities
  async getActivities(): Promise<ActivityInfo[]> {
    const response = await fetch(`${API_BASE_URL}/api/activities`);
    if (!response.ok) {
      throw new Error('Failed to fetch activities');
    }
    return response.json();
  },

  async getActivitiesByType(sportType: string): Promise<ActivityInfo[]> {
    const response = await fetch(`${API_BASE_URL}/api/activities/type/${sportType}`);
    if (!response.ok) {
      throw new Error('Failed to fetch activities by type');
    }
    return response.json();
  },

  async searchActivities(query: string): Promise<ActivityInfo[]> {
    const response = await fetch(`${API_BASE_URL}/api/activities/search?query=${encodeURIComponent(query)}`);
    if (!response.ok) {
      throw new Error('Failed to search activities');
    }
    return response.json();
  },

  // Plan Generation
  async generatePlan(
    athlete: AthleteProfile,
    race: RaceProfile,
    products: (ProductEditor & { productType: string })[]
  ): Promise<RaceNutritionPlan> {
    // Map enums to integers for API
    const sportTypeMap: Record<string, number> = { Run: 0, Bike: 1, Triathlon: 2 };
    const intensityMap: Record<string, number> = { Easy: 0, Moderate: 1, Hard: 2 };
    
    // Convert TemperatureCondition enum to representative temperature in Celsius
    const temperatureCMap: Record<string, number> = {
      Cold: 0,      // ≤ 5°C
      Moderate: 15, // 5-25°C
      Hot: 30       // ≥ 25°C
    };
    
    const request = {
      athleteWeightKg: athlete.weightKg,
      sportType: sportTypeMap[race.sportType],
      durationHours: race.durationHours,
      temperatureC: temperatureCMap[race.temperature],
      intensity: intensityMap[race.intensity],
      products: products.map(p => ({
        name: p.name,
        productType: p.productType,
        carbsG: p.carbsG,
        sodiumMg: p.sodiumMg,
        volumeMl: p.volumeMl || 0
      }))
    };

    const response = await fetch(`${API_BASE_URL}/api/plan/generate`, {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
      },
      body: JSON.stringify(request),
    });

    if (!response.ok) {
      const error = await response.text();
      throw new Error(error || 'Failed to generate nutrition plan');
    }

    return response.json();
  },

  // Metadata
  async getUIMetadata(): Promise<UIMetadata> {
    const response = await fetch(`${API_BASE_URL}/api/metadata`);
    if (!response.ok) {
      throw new Error('Failed to fetch UI metadata');
    }
    return response.json();
  },

  async getTemperatureMetadata(): Promise<TemperatureMetadata[]> {
    const response = await fetch(`${API_BASE_URL}/api/metadata/temperatures`);
    if (!response.ok) {
      throw new Error('Failed to fetch temperature metadata');
    }
    return response.json();
  },

  async getIntensityMetadata(): Promise<IntensityMetadata[]> {
    const response = await fetch(`${API_BASE_URL}/api/metadata/intensities`);
    if (!response.ok) {
      throw new Error('Failed to fetch intensity metadata');
    }
    return response.json();
  },

  async getDefaults(): Promise<{ defaultActivityId: string }> {
    const response = await fetch(`${API_BASE_URL}/api/metadata/defaults`);
    if (!response.ok) {
      throw new Error('Failed to fetch defaults');
    }
    return response.json();
  },

  async getConfigurationMetadata(): Promise<ConfigurationMetadata> {
    const response = await fetch(`${API_BASE_URL}/api/metadata/configuration`);
    if (!response.ok) {
      throw new Error('Failed to fetch configuration metadata');
    }
    return response.json();
  },
};
