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

export const TemperatureDescriptions: Record<TemperatureCondition, { range: string; effects: string[] }> = {
  Cold: {
    range: "≤ 5°C",
    effects: [
      "Reduced fluid needs",
      "Less sodium required",
      "Risk of overconsumption",
      "Lower sweating rate"
    ]
  },
  Moderate: {
    range: "5-25°C",
    effects: [
      "Baseline nutrition targets",
      "Standard fluid intake",
      "Optimal conditions",
      "Stable digestion"
    ]
  },
  Hot: {
    range: "≥ 25°C",
    effects: [
      "Increased fluid needs",
      "Higher sodium requirements",
      "Risk of dehydration",
      "Faster carb absorption"
    ]
  }
};

export const IntensityDescriptions: Record<IntensityLevel, { icon: string; carbRange: string; heartRateZone: string; effects: string[] }> = {
  Easy: {
    icon: "🟢",
    carbRange: "30-60 g/hr",
    heartRateZone: "Zone 1-2 (60-75% max HR)",
    effects: [
      "Conversational pace",
      "Lower carb needs",
      "Minimal fuel requirements",
      "Comfortable breathing"
    ]
  },
  Moderate: {
    icon: "🟡",
    carbRange: "60-90 g/hr",
    heartRateZone: "Zone 3 (75-85% max HR)",
    effects: [
      "Steady effort",
      "Standard nutrition targets",
      "Regular intake intervals",
      "Manageable intensity"
    ]
  },
  Hard: {
    icon: "🔴",
    carbRange: "90-120 g/hr",
    heartRateZone: "Zone 4-5 (85-100% max HR)",
    effects: [
      "High effort/competitive",
      "Maximum carb intake",
      "Frequent fuel needs",
      "Elevated heart rate"
    ]
  }
};

export interface ProductInfo {
  id: string;
  name: string;
  brand: string;
  productType: string;
  carbsG: number;
  sodiumMg: number;
  volumeMl: number;
  caloriesKcal: number;
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
  productName: string;
  amountPortions: number;
  action: string;
  totalCarbsSoFar: number;
  hasCaffeine: boolean;
}

export interface AdvancedPlanResponse {
  race: RaceProfile;
  athlete: AthleteProfile;
  nutritionSchedule: NutritionEvent[];
  shoppingSummary?: ShoppingSummary;
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
