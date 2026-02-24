import { useState, useCallback } from 'react';
import { SportType, IntensityLevel, TemperatureCondition, type ProductInfo } from '../types';
import { ATHLETE_WEIGHT } from '../constants';

/**
 * Custom hook to manage form state for the nutrition planner
 * Consolidates related state and provides type-safe setters
 */
export function usePlannerForm() {
  // Athlete profile
  const [athleteWeight, setAthleteWeight] = useState<number>(ATHLETE_WEIGHT.DEFAULT);

  // Race details
  const [sportType, setSportType] = useState<SportType>(SportType.Run);
  const [duration, setDuration] = useState(1.5);
  const [temperature, setTemperature] = useState<TemperatureCondition>(TemperatureCondition.Moderate);
  const [intensity, setIntensity] = useState<IntensityLevel>(IntensityLevel.Moderate);

  // Nutrition preferences
  const [useCaffeine, setUseCaffeine] = useState(true);
  const [selectedProducts, setSelectedProducts] = useState<ProductInfo[]>([]);

  // Form validation - memoized
  const isFormValid = useCallback(() => {
    return (
      athleteWeight > 0 &&
      sportType &&
      duration > 0 &&
      temperature &&
      intensity &&
      selectedProducts.length > 0
    );
  }, [athleteWeight, sportType, duration, temperature, intensity, selectedProducts.length]);

  return {
    // State
    athleteWeight,
    sportType,
    duration,
    temperature,
    intensity,
    useCaffeine,
    selectedProducts,
    // Setters
    setAthleteWeight,
    setSportType,
    setDuration,
    setTemperature,
    setIntensity,
    setUseCaffeine,
    setSelectedProducts,
    // Validation
    isFormValid,
  };
}
