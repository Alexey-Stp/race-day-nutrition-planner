import type {
  AthleteProfile,
  RaceProfile,
  ProductEditor,
  RaceNutritionPlan,
  NutritionTargets,
  ScheduleItem
} from './types';

// Nutrition Constants (matching C# NutritionConstants)
const CARBS_PER_HOUR_EASY = 50;
const CARBS_PER_HOUR_MODERATE = 70;
const CARBS_PER_HOUR_HARD = 90;
const LONG_RACE_CARBS_BONUS = 10;
const LONG_RACE_THRESHOLD_HOURS = 5;

const BASE_FLUIDS_ML = 500;
const HOT_TEMP_THRESHOLD = 25;
const COLD_TEMP_THRESHOLD = 5;
const HOT_FLUIDS_BONUS = 200;
const COLD_FLUIDS_PENALTY = 100;
const HEAVY_ATHLETE_THRESHOLD = 80;
const LIGHT_ATHLETE_THRESHOLD = 60;
const HEAVY_ATHLETE_FLUIDS_BONUS = 50;
const LIGHT_ATHLETE_FLUIDS_PENALTY = 50;
const MIN_FLUIDS_ML = 300;
const MAX_FLUIDS_ML = 900;

const BASE_SODIUM_MG = 400;
const HOT_SODIUM_BONUS = 200;
const HEAVY_ATHLETE_SODIUM_BONUS = 100;
const MIN_SODIUM_MG = 300;
const MAX_SODIUM_MG = 1000;

const DEFAULT_INTERVAL_MINUTES = 20;

// Calculate nutrition targets
export function calculateTargets(race: RaceProfile, athlete: AthleteProfile): NutritionTargets {
  // Calculate carbs
  let carbsPerHour = CARBS_PER_HOUR_EASY;
  if (race.intensity === "Moderate") {
    carbsPerHour = CARBS_PER_HOUR_MODERATE;
  } else if (race.intensity === "Hard") {
    carbsPerHour = CARBS_PER_HOUR_HARD;
  }

  // Long race bonus for moderate/hard intensity
  if (race.durationHours >= LONG_RACE_THRESHOLD_HOURS && race.intensity !== "Easy") {
    carbsPerHour += LONG_RACE_CARBS_BONUS;
  }

  // Calculate fluids
  let fluidsPerHour = BASE_FLUIDS_ML;
  if (race.temperatureC >= HOT_TEMP_THRESHOLD) {
    fluidsPerHour += HOT_FLUIDS_BONUS;
  } else if (race.temperatureC <= COLD_TEMP_THRESHOLD) {
    fluidsPerHour -= COLD_FLUIDS_PENALTY;
  }

  if (athlete.weightKg > HEAVY_ATHLETE_THRESHOLD) {
    fluidsPerHour += HEAVY_ATHLETE_FLUIDS_BONUS;
  } else if (athlete.weightKg < LIGHT_ATHLETE_THRESHOLD) {
    fluidsPerHour -= LIGHT_ATHLETE_FLUIDS_PENALTY;
  }

  fluidsPerHour = Math.max(MIN_FLUIDS_ML, Math.min(MAX_FLUIDS_ML, fluidsPerHour));

  // Calculate sodium
  let sodiumPerHour = BASE_SODIUM_MG;
  if (race.temperatureC >= HOT_TEMP_THRESHOLD) {
    sodiumPerHour += HOT_SODIUM_BONUS;
  }

  if (athlete.weightKg > HEAVY_ATHLETE_THRESHOLD) {
    sodiumPerHour += HEAVY_ATHLETE_SODIUM_BONUS;
  }

  sodiumPerHour = Math.max(MIN_SODIUM_MG, Math.min(MAX_SODIUM_MG, sodiumPerHour));

  return {
    carbsGPerHour: carbsPerHour,
    fluidsMlPerHour: fluidsPerHour,
    sodiumMgPerHour: sodiumPerHour
  };
}

// Product type for calculation
interface Product {
  name: string;
  type: string;
  carbsG: number;
  sodiumMg: number;
  volumeMl?: number;
}

// Generate nutrition plan
export function generatePlan(
  race: RaceProfile,
  athlete: AthleteProfile,
  products: ProductEditor[]
): RaceNutritionPlan {
  // Convert ProductEditor to Product
  const productList: Product[] = products.map(p => ({
    name: p.name,
    type: p.volumeMl && p.volumeMl > 0 ? 'drink' : 'gel',
    carbsG: p.carbsG,
    sodiumMg: p.sodiumMg,
    volumeMl: p.volumeMl
  }));

  // Validate products
  const hasGel = productList.some(p => p.type === 'gel');
  const hasDrink = productList.some(p => p.type === 'drink');

  if (!hasGel) {
    throw new Error('At least one gel product is required');
  }
  if (!hasDrink) {
    throw new Error('At least one drink product is required');
  }

  const targets = calculateTargets(race, athlete);
  const totalMinutes = Math.round(race.durationHours * 60);
  const intervals = Math.floor(totalMinutes / DEFAULT_INTERVAL_MINUTES);

  const schedule: ScheduleItem[] = [];
  let totalCarbs = 0;
  let totalFluids = 0;
  let totalSodium = 0;

  const gels = productList.filter(p => p.type === 'gel');
  const drinks = productList.filter(p => p.type === 'drink');

  // Distribute products across intervals
  for (let i = 1; i <= intervals; i++) {
    const timeMin = i * DEFAULT_INTERVAL_MINUTES;
    
    // Alternate between gel and drink, or use both at some intervals
    if (i % 2 === 0 && gels.length > 0) {
      const gel = gels[i % gels.length];
      schedule.push({
        timeMin,
        productName: gel.name,
        amountPortions: 1
      });
      totalCarbs += gel.carbsG;
      totalSodium += gel.sodiumMg;
    }

    if (drinks.length > 0) {
      const drink = drinks[i % drinks.length];
      const portions = 0.5; // Half a bottle
      schedule.push({
        timeMin,
        productName: drink.name,
        amountPortions: portions
      });
      totalCarbs += drink.carbsG * portions;
      totalSodium += drink.sodiumMg * portions;
      totalFluids += (drink.volumeMl || 0) * portions;
    }
  }

  return {
    targets,
    totalCarbsG: totalCarbs,
    totalFluidsMl: totalFluids,
    totalSodiumMg: totalSodium,
    schedule
  };
}

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
