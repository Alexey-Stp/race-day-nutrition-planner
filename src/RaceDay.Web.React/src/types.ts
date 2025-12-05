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

export interface RaceNutritionPlan {
  targets: NutritionTargets;
  totalCarbsG: number;
  totalFluidsMl: number;
  totalSodiumMg: number;
  schedule: ScheduleItem[];
  productSummaries: ProductSummary[];
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
