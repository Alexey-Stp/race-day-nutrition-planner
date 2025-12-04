import React from 'react';
import type { RaceNutritionPlan } from '../types';

interface PlanResultsProps {
  plan: RaceNutritionPlan | null;
}

export const PlanResults: React.FC<PlanResultsProps> = ({ plan }) => {
  if (!plan) {
    return null;
  }

  const copyToClipboard = () => {
    const text = [
      '=== Race Day Nutrition Plan ===\n',
      'HOURLY TARGETS:',
      `Carbs: ${plan.targets.carbsGPerHour.toFixed(0)} g/hour`,
      `Fluids: ${plan.targets.fluidsMlPerHour.toFixed(0)} ml/hour`,
      `Sodium: ${plan.targets.sodiumMgPerHour.toFixed(0)} mg/hour\n`,
      'INTAKE SCHEDULE:',
      ...plan.schedule.map(item => 
        `${item.timeMin.toString().padStart(3, '0')} min → ${item.amountPortions.toFixed(1)}x ${item.productName}`
      )
    ].join('\n');

    navigator.clipboard.writeText(text).catch(err => {
      console.error('Failed to copy:', err);
    });
  };

  const printSchedule = () => {
    window.print();
  };

  return (
    <div className="results-section">
      <div className="summary-card results-card">
        <h2>📊 Nutrition Summary</h2>
        <div className="targets-grid">
          <div className="target-item">
            <div className="target-label">Carbs/Hour</div>
            <div className="target-value">{plan.targets.carbsGPerHour.toFixed(0)}</div>
            <div className="target-unit">g</div>
          </div>
          <div className="target-item">
            <div className="target-label">Fluids/Hour</div>
            <div className="target-value">{plan.targets.fluidsMlPerHour.toFixed(0)}</div>
            <div className="target-unit">ml</div>
          </div>
          <div className="target-item">
            <div className="target-label">Sodium/Hour</div>
            <div className="target-value">{plan.targets.sodiumMgPerHour.toFixed(0)}</div>
            <div className="target-unit">mg</div>
          </div>
        </div>

        <div className="totals-grid" style={{ marginTop: '20px' }}>
          <div className="total-item">
            <div className="total-label">Total Carbs</div>
            <div className="total-value">{plan.totalCarbsG.toFixed(0)} g</div>
          </div>
          <div className="total-item">
            <div className="total-label">Total Fluids</div>
            <div className="total-value">{plan.totalFluidsMl.toFixed(0)} ml</div>
          </div>
          <div className="total-item">
            <div className="total-label">Total Sodium</div>
            <div className="total-value">{plan.totalSodiumMg.toFixed(0)} mg</div>
          </div>
        </div>
      </div>

      <div className="results-card">
        <div className="results-header">
          <h2>⏱️ Intake Schedule (20-min intervals)</h2>
          <div className="action-buttons">
            <button className="btn btn-sm btn-outline-secondary" onClick={copyToClipboard} title="Copy schedule as text">
              📋 Copy
            </button>
            <button className="btn btn-sm btn-outline-secondary" onClick={printSchedule} title="Print schedule">
              🖨️ Print
            </button>
          </div>
        </div>

        <div className="schedule-table">
          <div className="schedule-header">
            <div className="col-time">Time</div>
            <div className="col-product">Product</div>
            <div className="col-amount">Amount</div>
          </div>
          {plan.schedule.map((item, index) => (
            <div key={index} className={`schedule-row ${index % 2 === 0 ? 'even' : 'odd'}`}>
              <div className="col-time">{item.timeMin}:{(item.timeMin % 60).toString().padStart(2, '0')} min</div>
              <div className="col-product">{item.productName}</div>
              <div className="col-amount">{item.amountPortions.toFixed(1)} portion{item.amountPortions !== 1 ? 's' : ''}</div>
            </div>
          ))}
        </div>
      </div>
    </div>
  );
};
