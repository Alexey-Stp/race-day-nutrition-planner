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

  const nutritionData = [
    {
      type: 'Carbohydrates',
      unit: 'g',
      perHour: carbsPerHour,
      total: totalCarbsG
    },
    {
      type: 'Calories',
      unit: 'kcal',
      perHour: carbsPerHour * 4,
      total: totalCarbsG * 4
    },
    {
      type: 'Fluids',
      unit: 'ml',
      perHour: fluidsPerHour,
      total: totalFluidsMl
    },
    {
      type: 'Sodium',
      unit: 'mg',
      perHour: sodiumPerHour,
      total: totalSodiumMg
    }
  ];

  return (
    <div className="form-card targets-card">
      <h2>Nutrition Targets</h2>
      
      <table className="targets-table">
        <thead>
          <tr>
            <th>Type</th>
            <th>Per Hour</th>
            <th>Total ({durationHours}h)</th>
          </tr>
        </thead>
        <tbody>
          {nutritionData.map((item, index) => (
            <tr key={index}>
              <td className="type-cell">{item.type}</td>
              <td className="value-cell">{item.perHour.toFixed(1)} {item.unit}</td>
              <td className="value-cell">{item.total.toFixed(1)} {item.unit}</td>
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  );
};
