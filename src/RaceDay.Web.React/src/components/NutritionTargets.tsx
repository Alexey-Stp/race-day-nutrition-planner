import React from 'react';
import type { NutritionTargets as NutritionTargetsType } from '../types';

interface NutritionTargetsProps {
  targets: NutritionTargetsType;
  totalCarbsG: number;
  totalFluidsMl: number;
  totalSodiumMg: number;
  durationHours: number;
}

export const NutritionTargetsDisplay: React.FC<NutritionTargetsProps> = ({
  targets,
  totalCarbsG,
  totalFluidsMl,
  totalSodiumMg,
  durationHours
}) => {
  const carbsPerHour = targets.carbsGPerHour;
  const fluidsPerHour = targets.fluidsMlPerHour;
  const sodiumPerHour = targets.sodiumMgPerHour;

  return (
    <div className="form-card targets-card">
      <h2>Nutrition Targets</h2>
      
      <div className="targets-grid">
        {/* Carbohydrates */}
        <div className="target-item">
          <div className="target-header">
            <h3>Carbohydrates</h3>
          </div>
          <div className="target-values">
            <div className="value-row">
              <span className="label">Per Hour:</span>
              <span className="value">{carbsPerHour.toFixed(1)} g</span>
            </div>
            <div className="value-row">
              <span className="label">Total ({durationHours}h):</span>
              <span className="value">{totalCarbsG.toFixed(1)} g</span>
            </div>
          </div>
        </div>

        {/* Calories */}
        <div className="target-item">
          <div className="target-header">
            <h3>Calories</h3>
          </div>
          <div className="target-values">
            <div className="value-row">
              <span className="label">Per Hour:</span>
              <span className="value">{(carbsPerHour * 4).toFixed(0)} kcal</span>
            </div>
            <div className="value-row">
              <span className="label">Total ({durationHours}h):</span>
              <span className="value">{(totalCarbsG * 4).toFixed(0)} kcal</span>
            </div>
          </div>
        </div>

        {/* Fluids */}
        <div className="target-item">
          <div className="target-header">
            <h3>Fluids</h3>
          </div>
          <div className="target-values">
            <div className="value-row">
              <span className="label">Per Hour:</span>
              <span className="value">{fluidsPerHour.toFixed(0)} ml</span>
            </div>
            <div className="value-row">
              <span className="label">Total ({durationHours}h):</span>
              <span className="value">{totalFluidsMl.toFixed(0)} ml</span>
            </div>
          </div>
        </div>

        {/* Sodium */}
        <div className="target-item">
          <div className="target-header">
            <h3>Sodium</h3>
          </div>
          <div className="target-values">
            <div className="value-row">
              <span className="label">Per Hour:</span>
              <span className="value">{sodiumPerHour.toFixed(0)} mg</span>
            </div>
            <div className="value-row">
              <span className="label">Total ({durationHours}h):</span>
              <span className="value">{totalSodiumMg.toFixed(0)} mg</span>
            </div>
          </div>
        </div>
      </div>
    </div>
  );
};
