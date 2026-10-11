// Utility functions

// Format duration as compact "Xh Ym" (e.g., "4h 05m", "2h 30m", "45m")
export function formatDuration(hours: number): string {
  const wholeHours = Math.floor(hours);
  const minutes = Math.round((hours - wholeHours) * 60);

  if (wholeHours === 0) {
    return `${minutes}m`;
  } else if (minutes === 0) {
    return `${wholeHours}h`;
  } else {
    return `${wholeHours}h ${minutes.toString().padStart(2, '0')}m`;
  }
}

// Stable per-event identifier: `${timeMin}-${productName}-${occurrence}` where
// occurrence disambiguates repeated product/time pairs. Survives re-renders and
// re-filtering, unlike an array index.
export function withEventIds<T extends { timeMin: number; productName: string }>(events: readonly T[]): (T & { id: string })[] {
  const seen = new Map<string, number>();
  return events.map((e) => {
    const base = `${e.timeMin}-${e.productName}`;
    const n = seen.get(base) ?? 0;
    seen.set(base, n + 1);
    return { ...e, id: `${base}-${n}` };
  });
}
