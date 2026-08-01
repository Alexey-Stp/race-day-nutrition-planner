// Core domain types matching C# models

export const SportType = {
  Run: "Run",
  Bike: "Bike",
  Triathlon: "Triathlon"
} as const;

export type SportType = typeof SportType[keyof typeof SportType];

export const IntensityLevel = {
  Easy: "Easy",
  Moderate: "Moderate",
  Hard: "Hard"
} as const;

export type IntensityLevel = typeof IntensityLevel[keyof typeof IntensityLevel];

export const TemperatureCondition = {
  Cold: "Cold",
  Moderate: "Moderate",
  Hot: "Hot"
} as const;

export type TemperatureCondition = typeof TemperatureCondition[keyof typeof TemperatureCondition];

export const DistancePreset = {
  Sprint: "Sprint",
  Olympic: "Olympic",
  HalfIron: "HalfIron",
  Ironman: "Ironman"
} as const;

export type DistancePreset = typeof DistancePreset[keyof typeof DistancePreset];

/** Explicit swim/bike/run leg durations in hours (triathlon only). */
export interface TriathlonLegs {
  swimH: number;
  bikeH: number;
  runH: number;
}

/** Default swim/bike/run leg fractions for a distance preset. */
export interface LegFractions {
  swim: number;
  bike: number;
  run: number;
}

/** Per-leg nutrition targets returned by the API (triathlon only). */
export interface SegmentTarget {
  phase: string;
  carbsG: number;
  sodiumMg: number;
  fluidMl: number;
  durationMinutes: number;
}

// Metadata types from backend API
export interface TemperatureMetadata {
  condition: TemperatureCondition;
  range: string;
  effects: string[];
}

export interface IntensityMetadata {
  level: IntensityLevel;
  icon: string;
  carbRange: string;
  heartRateZone: string;
  effects: string[];
}

export interface UIMetadata {
  temperatures: TemperatureMetadata[];
  intensities: IntensityMetadata[];
  defaultActivityId: string;
}

// Configuration metadata types
export interface PhaseInfo {
  phase: string;
  name: string;
  description: string;
}

export interface NutritionTargetConfig {
  name: string;
  unit: string;
  description: string;
  minValue: number;
  maxValue: number;
  baseValue: number;
}

export interface SportConfig {
  sportType: string;
  name: string;
  description: string;
  carbsPerKgPerHour: number;
  maxCarbsPerHour: number;
  slotIntervalMinutes: number;
  caffeineStartHour: number;
  caffeineIntervalHours: number;
  maxCaffeineMgPerKg: number;
}

export interface TemperatureAdjustment {
  temperatureCondition: string;
  range: string;
  fluidBonus: number;
  sodiumBonus: number;
  description: string;
}

export interface AthleteWeightConfig {
  thresholdKg: number;
  category: string;
  fluidBonus: number;
  sodiumBonus: number;
  description: string;
}

export interface ConfigurationMetadata {
  phases: PhaseInfo[];
  nutritionTargets: NutritionTargetConfig[];
  sports: SportConfig[];
  temperatureAdjustments: TemperatureAdjustment[];
  athleteWeightThresholds: AthleteWeightConfig[];
  descriptions: Record<string, string>;
}

export interface ProductInfo {
  id: string;
  name: string;
  brand: string;
  productType: string;
  carbsG: number;
  sodiumMg: number;
  volumeMl: number;
  caloriesKcal: number;
  caffeineMg?: number;
}

export interface ActivityInfo {
  id: string;
  name: string;
  description: string;
  sportType: SportType;
  minDurationHours: number;
  maxDurationHours: number;
  bestTimeHours: number;
  bestTimeFormatted: string;
  preset?: DistancePreset | null;
  legFractions?: LegFractions | null;
}

export interface ProductEditor {
  name: string;
  brand?: string;
  carbsG: number;
  sodiumMg: number;
  volumeMl: number;
}

export interface AthleteProfile {
  weightKg: number;
}

export interface RaceProfile {
  sportType: SportType;
  durationHours: number;
  temperature: TemperatureCondition;
  intensity: IntensityLevel;
  legs?: TriathlonLegs | null;
  preset?: DistancePreset | null;
}

export interface NutritionTargets {
  carbsGPerHour: number;
  fluidsMlPerHour: number;
  sodiumMgPerHour: number;
}

export interface ScheduleItem {
  timeMin: number;
  productName: string;
  amountPortions: number;
}

export interface ProductSummary {
  productName: string;
  totalPortions: number;
}

export interface NutritionEvent {
  timeMin: number;
  phase: string;
  phaseDescription: string;
  productName: string;
  amountPortions: number;
  action: string;
  totalCarbsSoFar: number;
  hasCaffeine: boolean;
  caffeineMg?: number;
  totalCaffeineSoFar?: number;
  carbsInEvent?: number;
  sipMl?: number;
}

export interface AdvancedPlanResponse {
  race: RaceProfile;
  athlete: AthleteProfile;
  nutritionSchedule: NutritionEvent[];
  shoppingSummary?: ShoppingSummary;
  warnings?: string[];
  errors?: string[];
  segmentTargets?: SegmentTarget[];
}

export interface ShoppingSummary {
  items: ShoppingItem[];
  totalProductCount: number;
  totalCarbs: number;
}

export interface ShoppingItem {
  productName: string;
  totalPortions: number;
  totalCarbs: number;
}

export interface RaceNutritionPlan extends AdvancedPlanResponse {
  // For backward compatibility - new response is advanced plan
}

export interface ScheduleDisplayItem {
  timeMin: number;
  productName: string;
  brand: string;
  type: string;
  caloriesKcal: number;
  carbsG: number;
  sodiumMg: number;
}
