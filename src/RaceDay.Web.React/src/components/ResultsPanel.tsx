import React, { useEffect, useMemo, useState } from 'react';
import type { RaceNutritionPlan, SportType, TemperatureCondition, IntensityLevel } from '../types';
import { api } from '../api';
import { formatDuration } from '../utils';
import { MAX_CAFFEINE_MG } from '../constants';
import { getShoppingSummary } from '../utils/shoppingListExtensions';

interface Targets {
  carbsGPerHour: number;
  fluidsMlPerHour: number;
  sodiumMgPerHour: number;
  totalCarbsG: number;
  totalFluidsML: number;
  totalSodiumMg: number;
}

interface ResultsPanelProps {
  className?: string;
  plan: RaceNutritionPlan | null;
  useCaffeine: boolean;
  athleteWeight: number;
  sportType: SportType;
  duration: number;
  temperature: TemperatureCondition;
  intensity: IntensityLevel;
  onRaceMode: () => void;
}

export const ResultsPanel: React.FC<ResultsPanelProps> = ({
  className = '',
  plan,
  useCaffeine,
  athleteWeight,
  sportType,
  duration,
  temperature,
  intensity,
  onRaceMode,
}) => {
  const [targets, setTargets] = useState<Targets | null>(null);

  useEffect(() => {
    let active = true;
    const fetch = plan?.race && plan?.athlete
      ? api.calculateNutritionTargets(
          plan.athlete.weightKg,
          plan.race.sportType,
          plan.race.durationHours,
          plan.race.temperature,
          plan.race.intensity,
        )
      : Promise.resolve(null);
    fetch.then((res) => { if (active) setTargets(res); }).catch(console.error);
    return () => { active = false; };
  }, [plan]);

  const schedule = useMemo(() => {
    if (!plan?.nutritionSchedule) return [];
    return useCaffeine ? plan.nutritionSchedule : plan.nutritionSchedule.filter((e) => !e.hasCaffeine);
  }, [plan, useCaffeine]);

  const { totalCarbs, totalCaffeine } = useMemo(() => {
    const c = schedule.reduce((s, e) => s + (e.carbsInEvent ?? 0), 0);
    const caf = schedule.reduce((s, e) => s + (e.caffeineMg ?? 0), 0);
    return { totalCarbs: c, totalCaffeine: caf };
  }, [schedule]);

  const shopping = useMemo(() => getShoppingSummary(plan), [plan]);

  const isTriathlon = sportType === 'Triathlon';

  // Group the schedule by leg for triathlon; otherwise a single flat list.
  const legGroups = useMemo(() => {
    if (!isTriathlon) return [{ phase: '', items: schedule }];
    const order = ['Swim', 'Bike', 'Run'];
    return order
      .map((phase) => ({ phase, items: schedule.filter((e) => e.phase === phase) }))
      .filter((g) => g.items.length > 0);
  }, [schedule, isTriathlon]);

  const segmentTargets = plan?.segmentTargets ?? [];
  const warnings = plan?.warnings ?? [];

  if (!plan) {
    return (
      <section className={`panel-results ${className}`}>
        <div className="panel-results-inner">
          <div className="empty-state">
            <div className="empty-mark">◎</div>
            <h2>No plan yet</h2>
            <p>Set up your race on the left, then generate a plan.</p>
          </div>
        </div>
      </section>
    );
  }

  return (
    <section className={`panel-results ${className}`}>
      <div className="panel-results-inner">
        <header className="results-head">
          <div>
            <div className="stamp">{sportType} · {formatDuration(duration)} · {athleteWeight}kg · {intensity} · {temperature}</div>
            <h2>Your race day plan</h2>
          </div>
          <div className="results-head-actions">
            <button type="button" className="btn-race-mode is-on" onClick={onRaceMode}>
              <span className="dot" aria-hidden /> Race mode
            </button>
          </div>
        </header>

        <div className="targets">
          <Target label="Carbs/hr" value={targets ? `${targets.carbsGPerHour.toFixed(0)}g` : '—'} />
          <Target label="Total carbs" value={targets ? `${targets.totalCarbsG.toFixed(0)}g` : '—'} sub={`Plan ${totalCarbs.toFixed(0)}g`} />
          <Target label="Fluids/hr" value={targets ? `${targets.fluidsMlPerHour.toFixed(0)}ml` : '—'} />
          <Target label="Sodium/hr" value={targets ? `${targets.sodiumMgPerHour.toFixed(0)}mg` : '—'} />
        </div>

        <div className="bars">
          <Bar label="Carbs" value={totalCarbs} max={targets?.totalCarbsG ?? 1} unit="g" />
          {useCaffeine && <Bar label="Caffeine" value={totalCaffeine} max={MAX_CAFFEINE_MG} unit="mg" />}
        </div>

        {warnings.length > 0 && (
          <output className="warnings">
            {warnings.map((w) => (
              <div key={w} className="warning">{w}</div>
            ))}
          </output>
        )}

        {isTriathlon && segmentTargets.length > 0 && (
          <section className="leg-targets">
            <h3>Per-leg targets</h3>
            <div className="leg-target-grid">
              {segmentTargets.map((s) => (
                <div key={s.phase} className="leg-target">
                  <div className="leg-target-head">{s.phase}</div>
                  <div className="leg-target-body">
                    <span>{formatDuration(s.durationMinutes / 60)}</span>
                    <span>{s.carbsG.toFixed(0)}g carbs</span>
                    <span>{s.fluidMl.toFixed(0)}ml</span>
                  </div>
                </div>
              ))}
            </div>
          </section>
        )}

        <section className="feed-list">
          <h3>Schedule</h3>
          {legGroups.map((group) => (
            <div key={group.phase || 'all'} className="feed-group">
              {group.phase && <div className="feed-group-head">{group.phase}</div>}
              <ol>
                {group.items.map((e, i) => {
                  const when = e.timeMin < 0
                    ? `T−${Math.abs(e.timeMin)}m`
                    : `T+${formatDuration(e.timeMin / 60)}`;
                  return (
                    <li key={`${group.phase}-${i}`} className="feed-item">
                      <span className="feed-when">{when}</span>
                      <span className="feed-dot" aria-hidden />
                      <div className="feed-body">
                        <div className="feed-title">{e.productName}</div>
                        <div className="feed-meta">
                          <span>{e.action}</span>
                          {e.carbsInEvent ? <span>{e.carbsInEvent.toFixed(0)}g carbs</span> : null}
                          {e.caffeineMg ? <span>{e.caffeineMg}mg caf</span> : null}
                        </div>
                      </div>
                    </li>
                  );
                })}
              </ol>
            </div>
          ))}
        </section>

        {shopping && shopping.items.length > 0 && (
          <section className="shop">
            <h3>Shopping list</h3>
            <div className="shop-grid">
              {shopping.items.map((it) => (
                <div key={it.productName} className="shop-item">
                  <div className="shop-name">{it.productName}</div>
                  <div className="shop-meta">{it.totalPortions.toFixed(1)} × · {it.totalCarbs.toFixed(0)}g</div>
                </div>
              ))}
            </div>
          </section>
        )}
      </div>
    </section>
  );
};

const Target: React.FC<{ label: string; value: string; sub?: string }> = ({ label, value, sub }) => (
  <div className="target">
    <div className="target-label">{label}</div>
    <div className="target-value">{value}</div>
    {sub && <div className="target-sub">{sub}</div>}
  </div>
);

const Bar: React.FC<{ label: string; value: number; max: number; unit: string }> = ({ label, value, max, unit }) => {
  const pct = Math.min(100, Math.max(0, (value / Math.max(1, max)) * 100));
  return (
    <div className="bar">
      <div className="bar-head"><span>{label}</span><span>{value.toFixed(0)}/{max.toFixed(0)} {unit}</span></div>
      <div className="bar-track"><div className="bar-fill" style={{ width: `${pct}%` }} /></div>
    </div>
  );
};
