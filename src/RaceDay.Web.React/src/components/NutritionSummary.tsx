import React, { useMemo } from 'react';
import type { RaceNutritionPlan } from '../types';
import { ProgressBar } from './ProgressBar';
import { MAX_CAFFEINE_MG } from '../constants';
import { formatDuration } from '../utils';

interface NutritionTargets {
  carbsGPerHour: number;
  fluidsMlPerHour: number;
  sodiumMgPerHour: number;
  totalCarbsG: number;
  totalFluidsML: number;
  totalSodiumMg: number;
}

interface NutritionSummaryProps {
  plan: RaceNutritionPlan;
  targets: NutritionTargets | null;
  useCaffeine: boolean;
  loadingTargets: boolean;
}

export const NutritionSummary: React.FC<NutritionSummaryProps> = ({
  plan,
  targets,
  useCaffeine,
  loadingTargets
}) => {
  // Calculate totals from schedule - memoized to prevent recalculation
  const { totalCarbs, totalCaffeine } = useMemo(() => {
    const schedule = useCaffeine
      ? plan.nutritionSchedule
      : plan.nutritionSchedule.filter(event => !event.hasCaffeine);

    const carbs = schedule.reduce((sum, e) => sum + (e.carbsInEvent ?? 0), 0) ||
      (schedule.length > 0 ? schedule.at(-1)!.totalCarbsSoFar : 0);
    
    const caffeine = schedule.reduce((sum, event) => sum + (event.caffeineMg || 0), 0);

    return { totalCarbs: carbs, totalCaffeine: caffeine };
  }, [plan.nutritionSchedule, useCaffeine]);

  const raceDuration = plan.race?.durationHours ?? 0;

  if (loadingTargets) {
    return (
      <div className="nutrition-summary">
        <p className="loading-text">Loading targets...</p>
      </div>
    );
  }

  if (!targets) {
    return (
      <div className="nutrition-summary">
        <p className="error-text">Failed to load nutrition targets</p>
      </div>
    );
  }

  return (
    <div className="nutrition-summary">
      {/* Quick stats row */}
      <div className="nutrition-stats">
        <div className="nutrition-stat">
          <span className="nutrition-stat__label">Target</span>
          <span className="nutrition-stat__value">
            {targets.carbsGPerHour.toFixed(0)}g/h × {formatDuration(raceDuration)} = {targets.totalCarbsG.toFixed(0)}g
          </span>
        </div>
        <div className="nutrition-stat nutrition-stat--highlight">
          <span className="nutrition-stat__label">Plan</span>
          <span className="nutrition-stat__value">
            <strong>{totalCarbs.toFixed(0)}g</strong>
          </span>
        </div>
      </div>

      {/* Progress bars */}
      <div className="nutrition-progress">
        <ProgressBar
          value={totalCarbs}
          max={targets.totalCarbsG}
          label="Carbs"
          unit="g"
        />

        {useCaffeine && (
          <ProgressBar
            value={totalCaffeine}
            max={MAX_CAFFEINE_MG}
            label="Caffeine"
            unit="mg"
          />
        )}
      </div>
    </div>
  );
};
