import React, { useEffect, useMemo, useState } from 'react';
import type { RaceNutritionPlan } from '../types';
import { formatDuration, withEventIds } from '../utils';

interface RacePaneProps {
  className?: string;
  plan: RaceNutritionPlan | null;
  /** Whether the race pane is currently shown (desktop race mode or mobile Race tab). */
  active: boolean;
  onExit: () => void;
}

const TICK_MS = 1000;

export const RacePane: React.FC<RacePaneProps> = ({ className = '', plan, active, onExit }) => {
  const [startAt, setStartAt] = useState<number | null>(null);
  const [now, setNow] = useState(0);
  const [done, setDone] = useState<ReadonlySet<string>>(() => new Set());

  // A new plan invalidates progress: reset during render (React's recommended
  // pattern for deriving state from props) rather than in an effect.
  const [trackedPlan, setTrackedPlan] = useState(plan);
  if (trackedPlan !== plan) {
    setTrackedPlan(plan);
    setStartAt(null);
    setDone(new Set());
  }

  // Only tick while the pane is visible and the race clock is running.
  const running = active && startAt !== null;
  useEffect(() => {
    if (!running) return;
    const id = setInterval(() => setNow(Date.now()), TICK_MS);
    return () => clearInterval(id);
  }, [running]);

  // Render the plan exactly as generated (pre-race items are not tracked here).
  const schedule = useMemo(
    () => withEventIds(plan?.nutritionSchedule ?? []).filter((e) => e.timeMin >= 0),
    [plan],
  );

  const elapsedMin = startAt ? Math.max(0, now - startAt) / 60000 : 0;

  const nextEvent = useMemo(
    () => schedule.find((e) => !done.has(e.id) && e.timeMin >= elapsedMin) ?? null,
    [schedule, elapsedMin, done],
  );

  const countdownSec = nextEvent ? Math.max(0, Math.round((nextEvent.timeMin - elapsedMin) * 60)) : 0;
  const mm = Math.floor(countdownSec / 60).toString().padStart(2, '0');
  const ss = (countdownSec % 60).toString().padStart(2, '0');

  const { completedCarbs, completedCaf, completedCount } = useMemo(() => {
    const taken = schedule.filter((e) => done.has(e.id));
    return {
      completedCarbs: taken.reduce((s, e) => s + (e.carbsInEvent ?? 0), 0),
      completedCaf: taken.reduce((s, e) => s + (e.caffeineMg ?? 0), 0),
      completedCount: taken.length,
    };
  }, [schedule, done]);

  const elapsedLabel = startAt ? formatDuration(elapsedMin / 60) : '—';

  const toggle = (id: string) => {
    const n = new Set(done);
    if (n.has(id)) n.delete(id); else n.add(id);
    setDone(n);
  };

  const start = () => {
    const t = Date.now();
    setNow(t);
    setStartAt(t);
  };

  return (
    <section className={`race-pane ${className}`}>
      <div className="race">
        <header className="race-head">
          <div>
            <div className="race-kicker">Race mode</div>
            <div className="race-clock">{elapsedLabel}</div>
          </div>
          <div className="race-head-actions">
            {startAt ? (
              <button className="btn-ghost" type="button" onClick={() => { setStartAt(null); setDone(new Set()); }}>Reset</button>
            ) : (
              <button className="btn-generate" type="button" onClick={start}>Start</button>
            )}
            <button className="btn-ghost" type="button" onClick={onExit}>Exit</button>
          </div>
        </header>

        {nextEvent ? (
          <div className="race-next">
            <div className="race-next-when">
              Next in <strong>{mm}:{ss}</strong>
            </div>
            <div className="race-next-title">{nextEvent.productName}</div>
            <div className="race-next-meta">
              <span>{nextEvent.action}</span>
              {nextEvent.carbsInEvent ? <span>{nextEvent.carbsInEvent.toFixed(0)}g carbs</span> : null}
              {nextEvent.caffeineMg ? <span>{nextEvent.caffeineMg}mg caf</span> : null}
            </div>
            <button
              type="button"
              className="btn-generate"
              onClick={() => toggle(nextEvent.id)}
            >
              Mark taken
            </button>
          </div>
        ) : (
          <div className="race-next race-next-empty">
            {startAt ? 'All feeds complete. Well done.' : 'Press Start when the gun goes.'}
          </div>
        )}

        <div className="race-totals">
          <div><span>Carbs in</span><strong>{completedCarbs.toFixed(0)}g</strong></div>
          {plan?.caffeineEnabled && <div><span>Caffeine in</span><strong>{completedCaf.toFixed(0)}mg</strong></div>}
          <div><span>Taken</span><strong>{completedCount}/{schedule.length}</strong></div>
        </div>

        <ol className="race-list">
          {schedule.map((e) => {
            const passed = elapsedMin >= e.timeMin;
            return (
              <li key={e.id} className={`race-row ${done.has(e.id) ? 'is-done' : ''} ${passed ? 'is-passed' : ''}`}>
                <label>
                  <input type="checkbox" checked={done.has(e.id)} onChange={() => toggle(e.id)} />
                  <span className="race-row-time">T+{formatDuration(e.timeMin / 60)}</span>
                  <span className="race-row-title">{e.productName}</span>
                  <span className="race-row-meta">
                    {e.carbsInEvent ? `${e.carbsInEvent.toFixed(0)}g` : ''}
                    {e.caffeineMg ? ` · ${e.caffeineMg}mg caf` : ''}
                  </span>
                </label>
              </li>
            );
          })}
        </ol>
      </div>
    </section>
  );
};
