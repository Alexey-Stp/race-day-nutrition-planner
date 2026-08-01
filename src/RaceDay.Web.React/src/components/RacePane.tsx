import React, { useEffect, useMemo, useState } from 'react';
import type { RaceNutritionPlan } from '../types';
import { formatDuration } from '../utils';

interface RacePaneProps {
  className?: string;
  plan: RaceNutritionPlan | null;
  useCaffeine: boolean;
  onExit: () => void;
}

const eventId = (e: { timeMin: number; productName: string }) => `${e.timeMin}:${e.productName}`;

export const RacePane: React.FC<RacePaneProps> = ({ className = '', plan, useCaffeine, onExit }) => {
  const [startAt, setStartAt] = useState<number | null>(null);
  const [now, setNow] = useState(Date.now());
  const [done, setDone] = useState<Set<string>>(new Set());

  useEffect(() => {
    setStartAt(null);
    setDone(new Set());
  }, [plan]);

  useEffect(() => {
    if (!startAt) return;
    const id = setInterval(() => setNow(Date.now()), 1000);
    return () => clearInterval(id);
  }, [startAt]);

  const schedule = useMemo(() => {
    if (!plan?.nutritionSchedule) return [];
    const filtered = useCaffeine ? plan.nutritionSchedule : plan.nutritionSchedule.filter((e) => !e.hasCaffeine);
    return filtered.filter((e) => e.timeMin >= 0);
  }, [plan, useCaffeine]);

  const elapsedMin = startAt ? (now - startAt) / 60000 : 0;

  const nextEvent = useMemo(() => {
    return schedule.find((e) => !done.has(eventId(e)) && e.timeMin >= elapsedMin) ?? null;
  }, [schedule, elapsedMin, done]);
  const nextId = nextEvent ? eventId(nextEvent) : null;

  const countdownSec = nextEvent ? Math.max(0, Math.round((nextEvent.timeMin - elapsedMin) * 60)) : 0;
  const mm = Math.floor(countdownSec / 60).toString().padStart(2, '0');
  const ss = (countdownSec % 60).toString().padStart(2, '0');

  const completedCarbs = useMemo(
    () => schedule.filter((e) => done.has(eventId(e))).reduce((s, e) => s + (e.carbsInEvent ?? 0), 0),
    [schedule, done],
  );
  const completedCaf = useMemo(
    () => schedule.filter((e) => done.has(eventId(e))).reduce((s, e) => s + (e.caffeineMg ?? 0), 0),
    [schedule, done],
  );

  const elapsedLabel = startAt ? formatDuration(elapsedMin / 60) : '—';

  const toggle = (id: string) => {
    const n = new Set(done);
    if (n.has(id)) n.delete(id); else n.add(id);
    setDone(n);
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
              <button className="btn-generate" type="button" onClick={() => setStartAt(Date.now())}>Start</button>
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
              disabled={!nextId}
              onClick={() => nextId && toggle(nextId)}
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
          {useCaffeine && <div><span>Caffeine in</span><strong>{completedCaf.toFixed(0)}mg</strong></div>}
          <div><span>Taken</span><strong>{done.size}/{schedule.length}</strong></div>
        </div>

        <ol className="race-list">
          {schedule.map((e) => {
            const id = eventId(e);
            const passed = elapsedMin >= e.timeMin;
            return (
              <li key={id} className={`race-row ${done.has(id) ? 'is-done' : ''} ${passed ? 'is-passed' : ''}`}>
                <label>
                  <input type="checkbox" checked={done.has(id)} onChange={() => toggle(id)} />
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
