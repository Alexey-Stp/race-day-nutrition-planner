// Utility functions

// Format duration helper
export function formatDuration(hours: number): string {
  const wholeHours = Math.floor(hours);
  const minutes = Math.round((hours - wholeHours) * 60);

  if (wholeHours === 0) {
    return `${minutes} minutes`;
  } else if (minutes === 0) {
    return `${wholeHours} hour${wholeHours > 1 ? 's' : ''}`;
  } else {
    return `${wholeHours} hour${wholeHours > 1 ? 's' : ''} ${minutes} minutes`;
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
