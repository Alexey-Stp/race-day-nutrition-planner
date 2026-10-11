import React, { useMemo } from 'react';
import type { RaceNutritionPlan } from '../types';
import { formatDuration, withEventIds } from '../utils';
import { MAX_CAFFEINE_MG } from '../constants';
import { getShoppingSummary } from '../utils/shoppingListExtensions';

interface ResultsPanelProps {
  className?: string;
  plan: RaceNutritionPlan | null;
  /** True when form inputs differ from those the current plan was generated with. */
  inputsChanged: boolean;
  onRaceMode: () => void;
}

export const ResultsPanel: React.FC<ResultsPanelProps> = ({
  className = '',
  plan,
  inputsChanged,
  onRaceMode,
}) => {
  // Render the plan exactly as generated; caffeine was decided server-side.
  const schedule = useMemo(() => withEventIds(plan?.nutritionSchedule ?? []), [plan]);

  const { totalCarbs, totalCaffeine } = useMemo(() => {
    const c = schedule.reduce((s, e) => s + (e.carbsInEvent ?? 0), 0);
    const caf = schedule.reduce((s, e) => s + (e.caffeineMg ?? 0), 0);
    return { totalCarbs: c, totalCaffeine: caf };
  }, [schedule]);

  const shopping = useMemo(() => getShoppingSummary(plan), [plan]);

  const isTriathlon = plan?.race.sportType === 'Triathlon';

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
  const errors = plan?.errors ?? [];

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

  const { race, athlete, targets, caffeineEnabled } = plan;

  return (
    <section className={`panel-results ${className}`}>
      <div className="panel-results-inner">
        {inputsChanged && (
          <output className="warnings">
            <div className="warning">Inputs changed — regenerate to update the plan</div>
          </output>
        )}

        <header className="results-head">
          <div>
            <div className="stamp">{race.sportType} · {formatDuration(race.durationHours)} · {athlete.weightKg}kg · {race.intensity} · {race.temperature}</div>
            <h2>Your race day plan</h2>
          </div>
          <div className="results-head-actions">
            <button type="button" className="btn-race-mode is-on" onClick={onRaceMode}>
              <span className="dot" aria-hidden /> Race mode
            </button>
          </div>
        </header>

        <div className="targets">
          <Target label="Carbs/hr" value={`${targets.carbsGPerHour.toFixed(0)}g`} />
          <Target label="Total carbs" value={`${targets.totalCarbsG.toFixed(0)}g`} sub={`Plan ${totalCarbs.toFixed(0)}g`} />
          <Target label="Fluids/hr" value={`${targets.fluidsMlPerHour.toFixed(0)}ml`} />
          <Target label="Sodium/hr" value={`${targets.sodiumMgPerHour.toFixed(0)}mg`} />
        </div>

        <div className="bars">
          <Bar label="Carbs" value={totalCarbs} max={targets.totalCarbsG} unit="g" />
          {caffeineEnabled && (
            <Bar label="Caffeine" value={totalCaffeine} max={targets.caffeineMg > 0 ? targets.caffeineMg : MAX_CAFFEINE_MG} unit="mg" />
          )}
        </div>

        {errors.length > 0 && (
          <div className="warnings" role="alert">
            {errors.map((err) => (
              <div key={err} className="error">{err}</div>
            ))}
          </div>
        )}

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
                {group.items.map((e) => {
                  const when = e.timeMin < 0
                    ? `T−${Math.abs(e.timeMin)}m`
                    : `T+${formatDuration(e.timeMin / 60)}`;
                  return (
                    <li key={e.id} className="feed-item">
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
