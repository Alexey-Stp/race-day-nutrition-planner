import React from 'react';
import type { RaceNutritionPlan } from '../types';
import { formatDuration } from '../utils';

interface PlanResultsProps {
  plan: RaceNutritionPlan | null;
  productMap?: Map<string, { brand: string; type: string; carbsG: number; sodiumMg: number }>;
}

export const PlanResults: React.FC<PlanResultsProps> = ({ plan, productMap }) => {
  if (!plan) {
    return null;
  }

  // Group schedule items by time
  const uniqueTimes = Array.from(new Set(plan.schedule.map(s => s.timeMin))).sort((a, b) => a - b);

  return (
    <div className="results-section">
      <div className="results-card">
        <h2>Race Plan</h2>
        
        {uniqueTimes.length === 0 ? (
          <p className="empty-message">No schedule generated</p>
        ) : (
          <div className="schedule-table">
            <table>
              <thead>
                <tr>
                  <th>Time</th>
                  <th>Product</th>
                  <th>Brand</th>
                  <th className="text-right">Portions</th>
                  <th className="text-right">Carbs (g)</th>
                  <th className="text-right">Sodium (mg)</th>
                </tr>
              </thead>
              <tbody>
                {uniqueTimes.map(timeMin => {
                  const itemsAtTime = plan.schedule.filter(s => s.timeMin === timeMin);
                  return itemsAtTime.map((item, idx) => {
                    const productInfo = productMap?.get(item.productName);
                    const carbsTotal = productInfo ? productInfo.carbsG * item.amountPortions : 0;
                    const sodiumTotal = productInfo ? productInfo.sodiumMg * item.amountPortions : 0;
                    
                    return (
                      <tr key={`${timeMin}-${idx}`}>
                        <td>
                          {idx === 0 && (
                            <strong>{formatDuration(timeMin / 60)}</strong>
                          )}
                        </td>
                        <td>{item.productName}</td>
                        <td>{productInfo?.brand || '-'}</td>
                        <td className="text-right">{item.amountPortions}</td>
                        <td className="text-right">{carbsTotal.toFixed(1)}</td>
                        <td className="text-right">{sodiumTotal.toFixed(0)}</td>
                      </tr>
                    );
                  });
                })}
              </tbody>
            </table>
          </div>
        )}
      </div>
    </div>
  );
};
