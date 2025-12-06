import React from 'react';
import type { RaceNutritionPlan } from '../types';
import { getShoppingSummary } from '../utils/shoppingListExtensions';

interface ShoppingListProps {
  plan: RaceNutritionPlan | null;
}

export const ShoppingList: React.FC<ShoppingListProps> = ({ plan }) => {
  const summary = getShoppingSummary(plan);

  if (!plan || summary.items.length === 0) {
    return null;
  }

  return (
    <div className="results-card shopping-card">
      <h2>Shopping List</h2>
      
      <div className="shopping-summary">
        <div className="summary-stat">
          <span className="label">Total Items</span>
          <span className="value">{summary.totalProductCount}</span>
        </div>
        <div className="summary-stat">
          <span className="label">Total Carbs</span>
          <span className="value">{summary.totalCarbs.toFixed(0)}g</span>
        </div>
      </div>

      <div className="shopping-items">
        {summary.items.map((item) => (
          <div key={item.productName} className="shopping-item">
            <div className="item-name">{item.productName}</div>
            <div className="shopping-item-details">
              <span className="portion">{item.totalPortions.toFixed(1)} portion(s)</span>
              <span className="carbs">{item.totalCarbs.toFixed(0)}g</span>
            </div>
          </div>
        ))}
      </div>

      <div className="shopping-actions">
        <button 
          className="btn btn-secondary"
          onClick={() => {
            const list = summary.items
              .map(item => `${item.productName} (${item.totalPortions.toFixed(1)} portions)`)
              .join('\n');
            navigator.clipboard.writeText(list);
          }}
        >
          📋 Copy List
        </button>
      </div>
    </div>
  );
};
