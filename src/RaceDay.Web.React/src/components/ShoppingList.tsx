import React from 'react';
import type { ProductSummary } from '../types';

interface ShoppingListProps {
  productSummaries: ProductSummary[];
}

export const ShoppingList: React.FC<ShoppingListProps> = ({ productSummaries }) => {
  if (!productSummaries || productSummaries.length === 0) {
    return null;
  }

  return (
    <div className="shopping-list-section">
      <h2>Shopping List</h2>
      <p className="shopping-list-info">
        Total quantities needed for your race
      </p>
      
      <div className="shopping-list-wrapper">
        <table className="shopping-list-table">
          <thead>
            <tr>
              <th>Product</th>
              <th className="number-col">Quantity</th>
            </tr>
          </thead>
          <tbody>
            {productSummaries.map((summary, index) => (
              <tr key={`${summary.productName}-${index}`}>
                <td className="product-col">{summary.productName}</td>
                <td className="number-col">{summary.totalPortions.toFixed(1)}</td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>
    </div>
  );
};
