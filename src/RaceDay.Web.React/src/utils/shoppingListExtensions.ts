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

/**
 * Format shopping summary for plain text display
 */
export const formatShoppingList = (summary: ShoppingSummary): string => {
  if (summary.items.length === 0) {
    return 'No items to purchase';
  }

  const itemsList = summary.items
    .map(item => `• ${item.productName}: ${item.totalPortions.toFixed(1)} portion(s) (${item.totalCarbs.toFixed(0)}g carbs)`)
    .join('\n');

  return `${itemsList}\n\nTotal: ${summary.totalProductCount} items • ${summary.totalCarbs.toFixed(0)}g carbs`;
};

/**
 * Format shopping summary as CSV for export
 */
export const formatShoppingListCsv = (summary: ShoppingSummary): string => {
  if (summary.items.length === 0) {
    return 'Product,Portions,Carbs (g)\n';
  }

  let csv = 'Product,Portions,Carbs (g)\n';
  
  csv += summary.items
    .map(item => `"${item.productName}",${item.totalPortions.toFixed(1)},${item.totalCarbs.toFixed(0)}`)
    .join('\n');

  csv += `\nTOTAL,${summary.totalProductCount},${summary.totalCarbs.toFixed(0)}`;

  return csv;
};
