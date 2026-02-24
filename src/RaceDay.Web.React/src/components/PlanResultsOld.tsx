import React, { useEffect, useState } from 'react';
import type { RaceNutritionPlan, SportType, TemperatureCondition, IntensityLevel } from '../types';
import { formatDuration, formatPhase, formatAction } from '../utils';
import { api } from '../api';

interface PlanResultsProps {
  plan: RaceNutritionPlan | null;
  useCaffeine: boolean;
  athleteWeight: number;
  sportType: SportType;
  duration: number;
  temperature: TemperatureCondition;
  intensity: IntensityLevel;
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

const MAX_CAFFEINE_MG = 300;

const ProgressBar: React.FC<ProgressBarProps> = ({ value, max, percentage, label }) => {
  const isOptimal = percentage >= 95 && percentage <= 105;
  const isHigh = percentage > 105;
  const isLow = percentage < 95;

  let fillClassName = 'progress-fill';
  if (isOptimal) fillClassName += ' optimal';
  else if (isHigh) fillClassName += ' high';
  else if (isLow) fillClassName += ' low';

  let percentClassName = 'progress-percent';
  if (isOptimal) percentClassName += ' optimal';
  else if (isHigh) percentClassName += ' high';
  else if (isLow) percentClassName += ' low';

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

export const PlanResults: React.FC<PlanResultsProps> = ({
  plan,
  useCaffeine,
  athleteWeight,
  sportType,
  duration,
  temperature,
  intensity
}) => {
  const [targets, setTargets] = useState<NutritionTargets | null>(null);
  const [loadingTargets, setLoadingTargets] = useState(false);
  const [copySuccess, setCopySuccess] = useState(false);

  useEffect(() => {
    const fetchTargets = async () => {
      if (!(plan?.race && plan?.athlete)) return;

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

  const schedule = useCaffeine
    ? plan.nutritionSchedule
    : plan.nutritionSchedule.filter(event => !event.hasCaffeine);

  // Calculate totals from CarbsInEvent (sum of individual events) for accuracy
  const totalCarbs = schedule.reduce((sum, e) => sum + (e.carbsInEvent ?? 0), 0) ||
    (schedule.length > 0 ? schedule.at(-1)!.totalCarbsSoFar : 0);
  const totalCaffeine = schedule.reduce((sum, event) => sum + (event.caffeineMg || 0), 0);
  const raceDuration = plan.race?.durationHours ?? 0;

  const getSportTypeDisplay = (type: string) => {
    switch(type) {
      case 'Run': return '🏃';
      case 'Bike': return '🚴';
      case 'Triathlon': return '🏊‍♂️🚴🏃';
      default: return type;
    }
  };

  const getTemperatureDisplay = (temp: string) => {
    switch(temp) {
      case 'Cold': return '❄️ Cold';
      case 'Moderate': return '🌤️ Moderate';
      case 'Hot': return '🌡️ Hot';
      default: return temp;
    }
  };

  const planTitle = `${getSportTypeDisplay(sportType)} ${sportType} | ${athleteWeight}kg | ${formatDuration(duration)} | ${intensity} | ${getTemperatureDisplay(temperature)}`;

  const copyPlanToClipboard = () => {
    const planText = generatePlanText();
    navigator.clipboard.writeText(planText).then(() => {
      setCopySuccess(true);
      setTimeout(() => setCopySuccess(false), 2000);
    }).catch(err => {
      console.error('Failed to copy:', err);
    });
  };

  const generatePlanText = () => {
    const lines: string[] = [];

    lines.push('═══════════════════════════════════════════════');
    lines.push('           RACE NUTRITION PLAN');
    lines.push('═══════════════════════════════════════════════');
    lines.push('');
    lines.push('ATHLETE INFORMATION');
    lines.push(`  Weight: ${athleteWeight} kg`);
    lines.push('');
    lines.push('RACE INFORMATION');
    lines.push(`  Sport: ${sportType}`);
    lines.push(`  Duration: ${formatDuration(duration)}`);
    lines.push(`  Intensity: ${intensity}`);
    lines.push(`  Temperature: ${temperature}`);
    lines.push('');

    if (targets) {
      lines.push('NUTRITION TARGETS');
      lines.push(`  Carbs Target: ${targets.carbsGPerHour.toFixed(0)}g/h × ${formatDuration(raceDuration)} = ${targets.totalCarbsG.toFixed(0)}g`);
      lines.push(`  Plan Total Carbs: ${totalCarbs.toFixed(0)}g`);
      if (useCaffeine) {
        lines.push(`  Total Caffeine: ${totalCaffeine.toFixed(0)}mg`);
      }
      lines.push('');
    }

    lines.push('NUTRITION SCHEDULE');
    lines.push('───────────────────────────────────────────────');
    lines.push('');

    schedule.forEach((event, index) => {
      const timeStr = event.timeMin < 0 ? `Pre-race (${Math.abs(event.timeMin)}min before)` : formatDuration(event.timeMin / 60);
      const phaseStr = formatPhase(event.phase);
      const actionStr = formatAction(event);
      lines.push(`[${timeStr}] ${phaseStr}`);
      lines.push(`  Product: ${event.productName}`);
      lines.push(`  ${actionStr}`);
      if (event.carbsInEvent) {
        lines.push(`  Carbs: ${event.carbsInEvent.toFixed(1)}g (cumulative: ${event.totalCarbsSoFar.toFixed(0)}g)`);
      } else {
        lines.push(`  Cumulative Carbs: ${event.totalCarbsSoFar.toFixed(0)}g`);
      }
      if (event.hasCaffeine && event.caffeineMg) {
        lines.push(`  Caffeine: ${event.caffeineMg}mg`);
      }
      if (index < schedule.length - 1) {
        lines.push('');
      }
    });

    lines.push('');
    lines.push('═══════════════════════════════════════════════');
    lines.push(`Generated: ${new Date().toLocaleString()}`);
    lines.push('═══════════════════════════════════════════════');

    return lines.join('\n');
  };

  const carbsPercentage = targets?.totalCarbsG && targets.totalCarbsG > 0 ? (totalCarbs / targets.totalCarbsG) * 100 : 0;
  const caffeinePercentage = (totalCaffeine / MAX_CAFFEINE_MG) * 100;
  const planKey = `${plan.race?.durationHours}-${plan.race?.intensity}-${schedule.length}`;

  return (
    <div className="results-section" key={planKey}>
      <div className="results-card">
        {/* Sticky header with title and copy button */}
        <div className="plan-header-sticky">
          <h2 className="plan-title">{planTitle}</h2>
          <button
            onClick={copyPlanToClipboard}
            className="btn btn-secondary btn-compact"
            title="Copy plan to clipboard"
          >
            {copySuccess ? '✓ Copied!' : '📋 Copy'}
          </button>
        </div>

        {schedule.length === 0 ? (
          <p className="empty-message">No schedule generated</p>
        ) : (
          <>
            {/* Targets Section - compact */}
            <div className="targets-section">
              {loadingTargets && <p className="loading">Loading targets...</p>}
              {!loadingTargets && targets && (
                <div className="targets-container">
                  <div className="target-info">
                    <span className="info-label">Target:</span>
                    <span className="info-value">{targets.carbsGPerHour.toFixed(0)}g/h × {formatDuration(raceDuration)} = {targets.totalCarbsG.toFixed(0)}g</span>
                    <span className="info-label" style={{ marginLeft: '1rem' }}>Plan:</span>
                    <span className="info-value"><strong>{totalCarbs.toFixed(0)}g</strong></span>
                  </div>

                  <ProgressBar
                    value={totalCarbs}
                    max={targets.totalCarbsG}
                    percentage={carbsPercentage}
                    label="Carbs"
                  />

                  {useCaffeine && (
                    <ProgressBar
                      value={totalCaffeine}
                      max={MAX_CAFFEINE_MG}
                      percentage={caffeinePercentage}
                      label="Caffeine"
                    />
                  )}
                </div>
              )}
              {!loadingTargets && !targets && (
                <p className="error">Failed to load nutrition targets</p>
              )}
            </div>

            {/* Schedule table - compact */}
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
                    {useCaffeine && <th>Caffeine</th>}
                  </tr>
                </thead>
                <tbody>
                  {schedule.map((event, index) => {
                    const isSip = event.action === 'Sip';
                    return (
                      <tr
                        key={`${index}-${event.timeMin}-${event.productName}`}
                        title={event.phaseDescription}
                        className={isSip ? 'sip-row' : ''}
                      >
                        <td><strong>{event.timeMin < 0 ? `Pre (${Math.abs(event.timeMin)}m)` : formatDuration(event.timeMin / 60)}</strong></td>
                        <td>{formatPhase(event.phase)}</td>
                        <td>{event.productName}</td>
                        <td>{formatAction(event)}</td>
                        <td className="text-right">{(event.carbsInEvent ?? 0).toFixed(1)}g</td>
                        <td className="text-right">{event.totalCarbsSoFar.toFixed(0)}g</td>
                        {useCaffeine && <td>{event.hasCaffeine ? `☕ ${event.caffeineMg || '?'}mg` : '-'}</td>}
                      </tr>
                    );
                  })}
                </tbody>
              </table>
            </div>
          </>
        )}
      </div>
    </div>
  );
};
