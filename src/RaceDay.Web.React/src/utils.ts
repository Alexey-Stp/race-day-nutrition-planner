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

// Format race phase (0=Swim, 1=Bike, 2=Run)
export function formatPhase(phase: string | number): string {
  const phaseMap: Record<string | number, string> = {
    '0': 'Swim',
    '1': 'Bike',
    '2': 'Run',
    'Swim': 'Swim',
    'Bike': 'Bike',
    'Run': 'Run'
  };

  return phaseMap[phase] || String(phase);
}

// Format sip action for display
export function formatAction(event: { action: string; sipMl?: number; amountPortions: number }): string {
  if (event.action === 'Sip' && event.sipMl) {
    return `Sip: ${Math.round(event.sipMl)} ml`;
  }
  if (event.action === 'Sip') {
    return 'Sip';
  }
  return `${event.action} (${event.amountPortions} portion${event.amountPortions !== 1 ? 's' : ''})`;
}
