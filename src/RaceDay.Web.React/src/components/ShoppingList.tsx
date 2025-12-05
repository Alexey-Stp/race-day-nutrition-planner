import React from 'react';
import type { ProductSummary } from '../types';

interface ShoppingListProps {
  productSummaries: ProductSummary[];
  productMap?: Map<string, { brand: string; type: string; carbsG: number; sodiumMg: number }>;
}

export const ShoppingList: React.FC<ShoppingListProps> = ({ productSummaries, productMap }) => {
  if (!productSummaries || productSummaries.length === 0) {
    return null;
  }

  // Calculate totals
  let totalCarbsG = 0;
  let totalSodiumMg = 0;

  productSummaries.forEach(summary => {
    const productInfo = productMap?.get(summary.productName);
    if (productInfo) {
      totalCarbsG += productInfo.carbsG * summary.totalPortions;
      totalSodiumMg += productInfo.sodiumMg * summary.totalPortions;
    }
  });

  return (
    <div className="results-card">
      <h2>Shopping List</h2>
      
      <div className="shopping-list-wrapper">
        <table className="shopping-list-table">
          <thead>
            <tr>
              <th>Product</th>
              <th>Brand</th>
              <th>Type</th>
              <th className="text-right">Quantity</th>
              <th className="text-right">Carbs (g)</th>
              <th className="text-right">Sodium (mg)</th>
            </tr>
          </thead>
          <tbody>
            {productSummaries.map((summary, index) => {
              const productInfo = productMap?.get(summary.productName);
              const carbsTotal = productInfo ? productInfo.carbsG * summary.totalPortions : 0;
              const sodiumTotal = productInfo ? productInfo.sodiumMg * summary.totalPortions : 0;
              
              return (
                <tr key={`${summary.productName}-${index}`}>
                  <td className="product-col">{summary.productName}</td>
                  <td>{productInfo?.brand || '-'}</td>
                  <td>{productInfo?.type || '-'}</td>
                  <td className="text-right">{summary.totalPortions.toFixed(1)}</td>
                  <td className="text-right">{carbsTotal.toFixed(1)}</td>
                  <td className="text-right">{sodiumTotal.toFixed(0)}</td>
                </tr>
              );
            })}
          </tbody>
          <tfoot>
            <tr className="totals-row">
              <td colSpan={4} className="text-right"><strong>TOTAL</strong></td>
              <td className="text-right"><strong>{totalCarbsG.toFixed(1)}</strong></td>
              <td className="text-right"><strong>{totalSodiumMg.toFixed(0)}</strong></td>
            </tr>
          </tfoot>
        </table>
      </div>
    </div>
  );
};
