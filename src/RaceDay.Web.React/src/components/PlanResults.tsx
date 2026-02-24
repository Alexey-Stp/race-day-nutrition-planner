import React, { useEffect, useState, useMemo, useCallback } from 'react';
import type { RaceNutritionPlan, SportType, TemperatureCondition, IntensityLevel } from '../types';
import { formatDuration } from '../utils';
import { api } from '../api';
import { TimelineSection } from './Timeline';
import { NutritionSummary } from './NutritionSummary';
import { Button } from './ui/Button';
import { getSportTypeDisplay, getTemperatureDisplay } from '../constants/icons';

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

  // Fetch nutrition targets when plan changes
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

  // Filter schedule based on caffeine preference - memoized
  const schedule = useMemo(() => {
    if (!plan?.nutritionSchedule) return [];
    return useCaffeine
      ? plan.nutritionSchedule
      : plan.nutritionSchedule.filter(event => !event.hasCaffeine);
  }, [plan?.nutritionSchedule, useCaffeine]);

  // Split events into pre-race and in-race - memoized
  const { preRaceEvents, raceEvents } = useMemo(() => {
    const preRace = schedule.filter(e => e.timeMin < 0);
    const race = schedule.filter(e => e.timeMin >= 0);
    return { preRaceEvents: preRace, raceEvents: race };
  }, [schedule]);

  // Generate plan title - memoized
  const planTitle = useMemo(() => {
    return `${getSportTypeDisplay(sportType)} ${sportType} | ${athleteWeight}kg | ${formatDuration(duration)} | ${intensity} | ${getTemperatureDisplay(temperature)}`;
  }, [sportType, athleteWeight, duration, intensity, temperature]);

  // Generate text for clipboard - useCallback to prevent recreation
  const generatePlanText = useCallback(() => {
    if (!plan) return '';

    const lines: string[] = [];
    const raceDuration = plan.race?.durationHours ?? 0;
    const totalCarbs = schedule.reduce((sum, e) => sum + (e.carbsInEvent ?? 0), 0);
    const totalCaffeine = schedule.reduce((sum, event) => sum + (event.caffeineMg || 0), 0);

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
      const timeStr = event.timeMin < 0 
        ? `Pre-race (${Math.abs(event.timeMin)}min before)` 
        : formatDuration(event.timeMin / 60);
      
      lines.push(`[${timeStr}] ${event.phase}`);
      lines.push(`  Product: ${event.productName}`);
      lines.push(`  Action: ${event.action}`);
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
  }, [plan, schedule, targets, useCaffeine, athleteWeight, sportType, duration, intensity, temperature]);

  // Copy to clipboard handler - useCallback
  const copyPlanToClipboard = useCallback(() => {
    const planText = generatePlanText();
    navigator.clipboard.writeText(planText).then(() => {
      setCopySuccess(true);
      setTimeout(() => setCopySuccess(false), 2000);
    }).catch(err => {
      console.error('Failed to copy:', err);
    });
  }, [generatePlanText]);

  if (!plan?.nutritionSchedule) {
    return null;
  }

  if (schedule.length === 0) {
    return (
      <div className="results-card">
        <p className="empty-message">No schedule generated</p>
      </div>
    );
  }

  // Unique key for forcing re-render when plan changes significantly
  const planKey = `${plan.race?.durationHours}-${plan.race?.intensity}-${schedule.length}`;

  return (
    <div className="results-section" key={planKey}>
      <div className="results-card">
        {/* Sticky header with title and copy button */}
        <div className="plan-header-sticky">
          <h2 className="plan-title">{planTitle}</h2>
          <Button
            onClick={copyPlanToClipboard}
            variant="secondary"
            size="sm"
            title="Copy plan to clipboard"
          >
            {copySuccess ? '✓ Copied!' : '📋 Copy'}
          </Button>
        </div>

        {/* Nutrition summary with progress bars */}
        {plan && (
          <NutritionSummary
            plan={plan}
            targets={targets}
            useCaffeine={useCaffeine}
            loadingTargets={loadingTargets}
          />
        )}

        {/* Timeline view - replaces dense table */}
        <div className="plan-timeline">
          {preRaceEvents.length > 0 && (
            <TimelineSection
              title="Pre-Race Nutrition"
              events={preRaceEvents}
              showCaffeine={useCaffeine}
            />
          )}
          
          {raceEvents.length > 0 && (
            <TimelineSection
              title="Race Nutrition"
              events={raceEvents}
              showCaffeine={useCaffeine}
            />
          )}
        </div>
      </div>
    </div>
  );
};
