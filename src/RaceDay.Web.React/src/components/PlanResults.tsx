import React, { useEffect, useState } from 'react';
import type { RaceNutritionPlan } from '../types';
import { formatDuration, formatPhase } from '../utils';
import { api } from '../api';

interface PlanResultsProps {
  plan: RaceNutritionPlan | null;
}

interface NutritionTargets {
  carbsGPerHour: number;
  fluidsMlPerHour: number;
  sodiumMgPerHour: number;
  totalCarbsG: number;
  totalFluidsML: number;
  totalSodiumMg: number;
}

interface ProgressBarProps {
  value: number;
  max: number;
  percentage: number;
  label: string;
}

const ProgressBar: React.FC<ProgressBarProps> = ({ value, max, percentage, label }) => {
  const isOptimal = percentage >= 95 && percentage <= 105;
  const isHigh = percentage > 105;
  const isLow = percentage < 95;
  
  let fillClassName = 'progress-fill';
  if (isOptimal) {
    fillClassName += ' optimal';
  } else if (isHigh) {
    fillClassName += ' high';
  } else if (isLow) {
    fillClassName += ' low';
  }
  
  let percentClassName = 'progress-percent';
  if (isOptimal) {
    percentClassName += ' optimal';
  } else if (isHigh) {
    percentClassName += ' high';
  } else if (isLow) {
    percentClassName += ' low';
  }
  
  return (
    <div className="progress-item">
      <div className="progress-header">
        <span className="progress-label">{label}</span>
        <span className="progress-value">{value.toFixed(0)} / {max.toFixed(0)}</span>
      </div>
      <div className="progress-bar-container">
        <div className="progress-bar">
          <div 
            className={fillClassName}
            style={{ width: `${Math.min(percentage, 100)}%` }}
          />
        </div>
        <span className={percentClassName}>
          {percentage.toFixed(0)}%
        </span>
      </div>
    </div>
  );
};

export const PlanResults: React.FC<PlanResultsProps> = ({ plan }) => {
  const [targets, setTargets] = useState<NutritionTargets | null>(null);
  const [loadingTargets, setLoadingTargets] = useState(false);

  useEffect(() => {
    const fetchTargets = async () => {
      if (!plan || !plan.race || !plan.athlete) return;

      setLoadingTargets(true);
      try {
        const calculatedTargets = await api.calculateNutritionTargets(
          plan.athlete.weightKg,
          plan.race.sportType,
          plan.race.durationHours,
          plan.race.temperature,
          plan.race.intensity
        );
        setTargets(calculatedTargets);
      } catch (error) {
        console.error('Error fetching nutrition targets:', error);
      } finally {
        setLoadingTargets(false);
      }
    };

    fetchTargets();
  }, [plan]);

  if (!plan?.nutritionSchedule) {
    return null;
  }

  // Group schedule items by time
  const schedule = plan.nutritionSchedule;

  // Calculate totals
  const totalCarbs = schedule.length > 0 ? schedule[schedule.length - 1].totalCarbsSoFar : 0;
  const totalCaffeine = schedule.reduce((sum, event) => sum + (event.caffeineMg || 0), 0);
  const duration = plan.race?.durationHours || 0;

  // Calculate percentages based on loaded targets
  const carbsPercentage = targets?.totalCarbsG && targets.totalCarbsG > 0 ? (totalCarbs / targets.totalCarbsG) * 100 : 0;
  const caffeinePercentage = (totalCaffeine / 300) * 100; // 300mg conservative max

  // Create a unique key based on plan content to force re-render on plan changes
  const planKey = `${plan.race?.durationHours}-${plan.race?.intensity}-${schedule.length}`;

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
              {loadingTargets ? (
                <p className="loading">Loading nutrition targets...</p>
              ) : targets ? (
                <div className="targets-container">
                  <div className="target-info">
                    <div className="info-item">
                      <span className="info-label">Target Carbs (per hour):</span>
                      <span className="info-value">{targets.carbsGPerHour.toFixed(0)}g/h × {duration.toFixed(1)}h = {targets.totalCarbsG.toFixed(0)}g</span>
                    </div>
                  </div>
                  
                  <ProgressBar 
                    value={totalCarbs}
                    max={targets.totalCarbsG}
                    percentage={carbsPercentage}
                    label="Plan Carbs vs Target"
                  />
                  
                  <ProgressBar 
                    value={totalCaffeine}
                    max={300}
                    percentage={caffeinePercentage}
                    label="Plan Caffeine vs Target"
                  />
                </div>
              ) : (
                <p className="error">Failed to load nutrition targets</p>
              )}
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
