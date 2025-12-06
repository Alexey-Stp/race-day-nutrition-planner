import React from 'react';
import type { RaceNutritionPlan } from '../types';
import { formatDuration, formatPhase } from '../utils';

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
  const intensity = plan.race?.intensity || 'Moderate';
  const athleteWeight = plan.athlete?.weightKg || 75;

  // Calculate target carbs based on intensity and weight
  const intensityCarbsMap: Record<string, number> = {
    Easy: 50,      // g/hour
    Moderate: 70,  // g/hour
    Hard: 90       // g/hour
  };
  const carbsPerHour = intensityCarbsMap[intensity] || 70;
  const targetTotalCarbs = carbsPerHour * duration;
  const carbsPercentage = totalCarbs > 0 ? (totalCarbs / targetTotalCarbs) * 100 : 0;

  // Caffeine targets - typically 1.3mg per kg body weight at 1.5 hour mark, max ~200-300mg
  const maxCaffeineTarget = Math.min(athleteWeight * 2, 300); // Conservative estimate
  const caffeinePercentage = totalCaffeine > 0 ? (totalCaffeine / maxCaffeineTarget) * 100 : 0;

  // Create a unique key based on plan content to force re-render on plan changes
  const planKey = `${plan.race?.durationHours}-${plan.race?.intensity}-${schedule.length}`;

  const ProgressBar: React.FC<{ value: number; max: number; percentage: number; label: string }> = ({ value, max, percentage, label }) => {
    const isOptimal = percentage >= 95 && percentage <= 105;
    const isHigh = percentage > 105;
    const isLow = percentage < 95;
    
    return (
      <div className="progress-item">
        <div className="progress-header">
          <span className="progress-label">{label}</span>
          <span className="progress-value">{value.toFixed(0)} / {max.toFixed(0)}</span>
        </div>
        <div className="progress-bar-container">
          <div className="progress-bar">
            <div 
              className={`progress-fill ${isOptimal ? 'optimal' : isHigh ? 'high' : isLow ? 'low' : ''}`}
              style={{ width: `${Math.min(percentage, 100)}%` }}
            />
          </div>
          <span className={`progress-percent ${isOptimal ? 'optimal' : isHigh ? 'high' : isLow ? 'low' : ''}`}>
            {percentage.toFixed(0)}%
          </span>
        </div>
      </div>
    );
  };

  return (
    <div className="results-section" key={planKey}>
      <div className="results-card">
        <h2>Race Nutrition Plan</h2>
        
        {schedule.length === 0 ? (
          <p className="empty-message">No schedule generated</p>
        ) : (
          <>
            {/* Targets Section */}
            <div className="targets-section">
              <h3>Targets</h3>
              <div className="targets-container">
                <div className="target-info">
                  <div className="info-item">
                    <span className="info-label">Target Carbs (per hour):</span>
                    <span className="info-value">{carbsPerHour}g/h × {duration.toFixed(1)}h = {targetTotalCarbs.toFixed(0)}g</span>
                  </div>
                </div>
                
                <ProgressBar 
                  value={totalCarbs}
                  max={targetTotalCarbs}
                  percentage={carbsPercentage}
                  label="Plan Carbs vs Target"
                />
                
                <ProgressBar 
                  value={totalCaffeine}
                  max={maxCaffeineTarget}
                  percentage={caffeinePercentage}
                  label="Plan Caffeine vs Target"
                />
              </div>
            </div>

            <div className="schedule-table">
              <table>
                <thead>
                  <tr>
                    <th>Time</th>
                    <th>Phase</th>
                    <th>Phase Description</th>
                    <th>Product</th>
                    <th>Action</th>
                    <th className="text-right">Carbs</th>
                    <th className="text-right">Total</th>
                    <th>Caffeine</th>
                  </tr>
                </thead>
                <tbody>
                  {schedule.map((event, index) => (
                    <tr key={`${index}-${event.timeMin}-${event.productName}`} title={event.phaseDescription}>
                      <td><strong>{formatDuration(event.timeMin / 60)}</strong></td>
                      <td>{formatPhase(event.phase)}</td>
                      <td className="phase-desc">{event.phaseDescription}</td>
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
