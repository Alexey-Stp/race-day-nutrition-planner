// Design System Constants

// Spacing scale (in pixels) - follows 4px base unit
export const SPACING = {
  XXS: 4,
  XS: 8,
  SM: 12,
  MD: 16,
  LG: 24,
  XL: 32,
  XXL: 48,
} as const;

// Loading state
export const MIN_LOADING_MS = 5000;

export const LOADING_MESSAGES = [
  'Thinking...',
  'Analyzing your race profile...',
  'Calculating nutrition targets...',
  'Building your fuel schedule...',
  'Optimizing timing and portions...',
  'Finalizing your plan...',
] as const;

// Caffeine limits
export const MAX_CAFFEINE_MG = 300;

// Tap target sizes (Apple HIG)
export const MIN_TAP_TARGET = 44; // pixels

// Athlete metrics
export const ATHLETE_WEIGHT = {
  MIN: 40,
  MAX: 150,
  STEP: 0.5,
  DEFAULT: 75,
} as const;

// Duration constraints
export const DURATION = {
  MIN: 0.5,
  MAX: 24,
  STEP: 0.01667, // 1 minute in hours
} as const;

// Progress thresholds
export const PROGRESS_THRESHOLDS = {
  OPTIMAL_MIN: 95,
  OPTIMAL_MAX: 105,
} as const;
