import type { ProductInfo, ActivityInfo } from './types';

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
};
