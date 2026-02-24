import { useState, useEffect, useMemo, useCallback } from 'react';
import { type RaceNutritionPlan } from './types';
import { api } from './api';
import { AthleteProfileForm } from './components/AthleteProfileForm';
import { RaceDetailsForm } from './components/RaceDetailsForm';
import { TemperatureSelector } from './components/TemperatureSelector';
import { IntensitySelector } from './components/IntensitySelector';
import { AdvancedProductSelector } from './components/AdvancedProductSelector';
import { PlanResults } from './components/PlanResults';
import { ShoppingList } from './components/ShoppingList';
import { usePlannerForm } from './hooks/usePlannerForm';
import { 
  LOADING_MESSAGES, 
  MIN_LOADING_MS,
  getSportEmojiList 
} from './constants';

// Import all CSS modules
import './styles/tokens.css';
import './styles/base.css';
import './styles/buttons.css';
import './styles/components.css';
import './styles/timeline.css';
import './styles/progress.css';
import './App.css';

const APP_VERSION = import.meta.env.VITE_APP_VERSION ?? 'dev';
const MSG_INTERVAL_MS = Math.floor(MIN_LOADING_MS / LOADING_MESSAGES.length);

function App() {
  // Use custom hook for form state management
  const {
    athleteWeight,
    sportType,
    duration,
    temperature,
    intensity,
    useCaffeine,
    selectedProducts,
    setAthleteWeight,
    setSportType,
    setDuration,
    setTemperature,
    setIntensity,
    setUseCaffeine,
    setSelectedProducts,
    isFormValid,
  } = usePlannerForm();

  // Plan generation state
  const [plan, setPlan] = useState<RaceNutritionPlan | null>(null);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [messageIdx, setMessageIdx] = useState(0);

  // Cycle through loading messages
  useEffect(() => {
    if (!loading) return;
    setMessageIdx(0);
    const interval = setInterval(
      () => setMessageIdx(i => (i + 1) % LOADING_MESSAGES.length),
      MSG_INTERVAL_MS,
    );
    return () => clearInterval(interval);
  }, [loading]);

  // Get loading emoji based on sport type - memoized
  const emojiList = useMemo(() => getSportEmojiList(sportType), [sportType]);
  const loadingEmoji = useMemo(
    () => emojiList[messageIdx % emojiList.length],
    [emojiList, messageIdx]
  );

  // Generate plan handler - useCallback to prevent recreation
  const generatePlan = useCallback(async () => {
    if (selectedProducts.length === 0) {
      setError('Please select at least one product');
      return;
    }

    setLoading(true);
    setError(null);

    try {
      const athlete = { weightKg: athleteWeight };
      const race = {
        sportType,
        durationHours: duration,
        temperature,
        intensity
      };

      // Transform products to API format
      const products = selectedProducts.map(p => ({
        name: p.name,
        productType: p.productType,
        carbsG: p.carbsG,
        sodiumMg: p.sodiumMg,
        volumeMl: p.volumeMl ?? 0,
        caffeineMg: p.caffeineMg ?? undefined
      }));

      // Generate plan with minimum loading time for UX
      const [newPlan] = await Promise.all([
        api.generatePlan(athlete, race, products, useCaffeine),
        new Promise<void>(resolve => setTimeout(resolve, MIN_LOADING_MS)),
      ]);
      
      setPlan(newPlan);
      setError(null);
    } catch (err) {
      const errorMsg = err instanceof Error ? err.message : 'Failed to generate plan';
      setError(errorMsg);
      console.error('Error generating plan:', err);
    } finally {
      setLoading(false);
    }
  }, [selectedProducts, athleteWeight, sportType, duration, temperature, intensity, useCaffeine]);

  return (
    <div className="planner-container">
      {/* Header */}
      <div className="header">
        <span className="version-badge">{APP_VERSION}</span>
        <h1>Race Day Nutrition Planner</h1>
      </div>

      {/* Loading overlay */}
      {loading && (
        <div className="loading-overlay">
          <div className="loading-card">
            <div className="loading-track-area">
              <span className="loading-athlete">{loadingEmoji}</span>
              <div className="loading-track" />
            </div>
            <p key={messageIdx} className="loading-message">
              {LOADING_MESSAGES[messageIdx]}
            </p>
          </div>
        </div>
      )}

      <div className="content">
        {/* Left Section - Input Form */}
        <div className="form-section">
          <AthleteProfileForm
            athleteWeight={athleteWeight}
            onAthleteWeightChange={setAthleteWeight}
          />

          <div className="selector-group">
            <RaceDetailsForm
              sportType={sportType}
              duration={duration}
              onSportTypeChange={setSportType}
              onDurationChange={setDuration}
            />

            <IntensitySelector
              intensity={intensity}
              onIntensityChange={setIntensity}
            />

            <TemperatureSelector
              temperature={temperature}
              onTemperatureChange={setTemperature}
              useCaffeine={useCaffeine}
              onCaffeineToggle={setUseCaffeine}
            />
          </div>

          <AdvancedProductSelector onProductsSelected={setSelectedProducts} />

          {error && <div className="error-message">{error}</div>}

          {/* Sticky generate button */}
          <div className="sticky-action-row">
            <button
              onClick={generatePlan}
              className="btn btn-primary btn-lg btn-calculate"
              disabled={loading || !isFormValid()}
            >
              {loading ? 'Generating...' : 'Generate Plan'}
            </button>
          </div>
        </div>

        {/* Right Section - Results */}
        <div className="results-container">
          {plan ? (
            <>
              <PlanResults
                plan={plan}
                useCaffeine={useCaffeine}
                athleteWeight={athleteWeight}
                sportType={sportType}
                duration={duration}
                temperature={temperature}
                intensity={intensity}
              />
              <ShoppingList plan={plan} />
            </>
          ) : (
            <div className="form-card empty-results">
              <p className="empty-message">Generate a plan to see results here</p>
            </div>
          )}
        </div>
      </div>
    </div>
  );
}

export default App;
