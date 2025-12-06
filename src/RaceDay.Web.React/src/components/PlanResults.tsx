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
  const totalCaffeine = schedule.reduce((sum, event) => sum + (event.caffeineMg || 0), 0);
  const duration = plan.race?.durationHours || 0;

  return (
    <div className="results-section">
      <div className="results-card">
        <h2>Race Nutrition Plan</h2>
        
        {schedule.length === 0 ? (
          <p className="empty-message">No schedule generated</p>
        ) : (
          <>
            {/* Targets Section */}
            <div className="targets-section">
              <h3>Targets</h3>
              <table className="targets-table">
                <tbody>
                  <tr>
                    <td className="target-label">Total Carbs</td>
                    <td className="target-value">{totalCarbs.toFixed(0)}g</td>
                  </tr>
                  <tr>
                    <td className="target-label">Total Events</td>
                    <td className="target-value">{schedule.length}</td>
                  </tr>
                  <tr>
                    <td className="target-label">Duration</td>
                    <td className="target-value">{duration.toFixed(2)}h</td>
                  </tr>
                  <tr>
                    <td className="target-label">Total Caffeine</td>
                    <td className="target-value">{totalCaffeine.toFixed(0)}mg</td>
                  </tr>
                </tbody>
              </table>
            </div>

            <div className="schedule-table">
              <table>
                <thead>
                  <tr>
                    <th>Time</th>
                    <th>Sport Type</th>
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
                      <td>{plan.race?.sportType || event.phase}</td>
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
