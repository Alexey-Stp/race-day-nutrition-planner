import React from 'react';
import type { RaceNutritionPlan } from '../types';
import { formatDuration } from '../utils';

interface PlanResultsProps {
  plan: RaceNutritionPlan | null;
}

export const PlanResults: React.FC<PlanResultsProps> = ({ plan }) => {
  if (!plan?.nutritionSchedule) {
    return null;
  }

  // Group schedule items by time
  const schedule = plan.nutritionSchedule;

  // Calculate totals
  const totalCarbs = schedule.length > 0 ? schedule[schedule.length - 1].totalCarbsSoFar : 0;

  return (
    <div className="results-section">
      <div className="results-card">
        <h2>Race Nutrition Plan</h2>
        
        {schedule.length === 0 ? (
          <p className="empty-message">No schedule generated</p>
        ) : (
          <>
            <div className="plan-summary">
              <div className="summary-stat">
                <span className="label">Total Carbs</span>
                <span className="value">{totalCarbs.toFixed(0)}g</span>
              </div>
              <div className="summary-stat">
                <span className="label">Total Events</span>
                <span className="value">{schedule.length}</span>
              </div>
              <div className="summary-stat">
                <span className="label">Duration</span>
                <span className="value">{plan.race?.durationHours || 0}h</span>
              </div>
            </div>

            <div className="schedule-table">
              <table>
                <thead>
                  <tr>
                    <th>Time</th>
                    <th>Phase</th>
                    <th>Product</th>
                    <th>Action</th>
                    <th className="text-right">Carbs</th>
                    <th className="text-right">Total</th>
                    <th>Caffeine</th>
                  </tr>
                </thead>
                <tbody>
                  {schedule.map((event) => (
                    <tr key={`${event.timeMin}-${event.productName}`}>
                      <td><strong>{formatDuration(event.timeMin / 60)}</strong></td>
                      <td>{event.phase}</td>
                      <td>{event.productName}</td>
                      <td>{event.action}</td>
                      <td className="text-right">{event.totalCarbsSoFar.toFixed(0)}g</td>
                      <td className="text-right">{event.amountPortions} portion(s)</td>
                      <td>{event.hasCaffeine ? `☕ ${event.caffeineMg || '?'}mg` : '-'}</td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          </>
        )}
      </div>
    </div>
  );
};
