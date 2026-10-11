import type { RaceNutritionPlan, ShoppingSummary } from '../types';

/**
 * Get shopping summary from nutrition plan (calculated on backend)
 * If not available, return empty summary
 */
export const getShoppingSummary = (plan: RaceNutritionPlan | null): ShoppingSummary => {
  if (!plan?.shoppingSummary) {
    return {
      items: [],
      totalProductCount: 0,
      totalCarbs: 0
    };
  }
  return plan.shoppingSummary;
};
