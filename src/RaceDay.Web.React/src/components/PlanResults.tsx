import React from 'react';
import type { RaceNutritionPlan } from '../types';

interface PlanResultsProps {
  plan: RaceNutritionPlan | null;
}

export const PlanResults: React.FC<PlanResultsProps> = ({ plan }) => {
  if (!plan) {
    return null;
  }

  return (
    <div className="results-section">
      <div className="summary-card results-card">
        <h2>Targets</h2>
        <div className="targets-grid">
          <div className="target-item">
            <div className="target-label">Carbs/Hr</div>
            <div className="target-value">{plan.targets.carbsGPerHour.toFixed(0)}</div>
            <div className="target-unit">g</div>
          </div>
          <div className="target-item">
            <div className="target-label">Fluids/Hr</div>
            <div className="target-value">{plan.targets.fluidsMlPerHour.toFixed(0)}</div>
            <div className="target-unit">ml</div>
          </div>
          <div className="target-item">
            <div className="target-label">Sodium/Hr</div>
            <div className="target-value">{plan.targets.sodiumMgPerHour.toFixed(0)}</div>
            <div className="target-unit">mg</div>
          </div>
        </div>

        <div className="totals-grid" style={{ marginTop: '10px' }}>
          <div className="total-item">
            <div className="total-label">Total Carbs</div>
            <div className="total-value">{plan.totalCarbsG.toFixed(0)}g</div>
          </div>
          <div className="total-item">
            <div className="total-label">Total Fluids</div>
            <div className="total-value">{plan.totalFluidsMl.toFixed(0)}ml</div>
          </div>
          <div className="total-item">
            <div className="total-label">Total Sodium</div>
            <div className="total-value">{plan.totalSodiumMg.toFixed(0)}mg</div>
          </div>
        </div>
      </div>
    </div>
  );
};
