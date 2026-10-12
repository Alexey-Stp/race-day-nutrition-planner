// Icon mappings for different entities

import { SportType, IntensityLevel, TemperatureCondition } from '../types';

export const SPORT_EMOJI: Record<string, string[]> = {
  [SportType.Run]: ['🏃'],
  [SportType.Bike]: ['🚴'],
  [SportType.Triathlon]: ['🏊', '🚴', '🏃'],
} as const;

export const ACTIVITY_ICONS: Record<string, string> = {
  run: '🏃',
  bike: '🚴',
  triathlon: '🏊',
} as const;

export const TEMP_ICONS: Record<string, string> = {
  [TemperatureCondition.Cold]: '❄️',
  [TemperatureCondition.Moderate]: '🌤️',
  [TemperatureCondition.Hot]: '🌡️',
} as const;

export const INTENSITY_LABELS: Record<string, string> = {
  [IntensityLevel.Easy]: 'Easy',
  [IntensityLevel.Moderate]: 'Training',
  [IntensityLevel.Hard]: 'Race',
} as const;

export const PRODUCT_GROUP_LABELS: Record<string, string> = {
  drink: '🥤 Drinks',
  gel: '🟦 Gels',
  bar: '🍫 Bars',
  chew: '🍬 Chews',
  recovery: '💊 Recovery (Post-Race)',
} as const;

export const PRODUCT_GROUP_ORDER = ['drink', 'gel', 'bar', 'chew', 'recovery'] as const;

// Helper functions
export const getSportEmoji = (sportType: SportType): string => {
  const emojiList = SPORT_EMOJI[sportType] ?? ['🏃'];
  return emojiList[0];
};

export const getSportEmojiList = (sportType: SportType): string[] => {
  return SPORT_EMOJI[sportType] ?? ['🏃'];
};

export const getTemperatureIcon = (temp: TemperatureCondition): string => {
  return TEMP_ICONS[temp] ?? '🌤️';
};

export const getTemperatureDisplay = (temp: string): string => {
  switch (temp) {
    case 'Cold': return '❄️ Cold';
    case 'Moderate': return '🌤️ Moderate';
    case 'Hot': return '🌡️ Hot';
    default: return temp;
  }
};

export const getSportTypeDisplay = (type: string): string => {
  switch (type) {
    case 'Run': return '🏃';
    case 'Bike': return '🚴';
    case 'Triathlon': return '🏊‍♂️🚴🏃';
    default: return type;
  }
};
