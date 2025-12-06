import { useState } from 'react';
import { SportType, IntensityLevel, TemperatureCondition, type RaceNutritionPlan } from './types';
import { api } from './api';
import { AthleteProfileForm } from './components/AthleteProfileForm';
import { RaceDetailsForm } from './components/RaceDetailsForm';
import { TemperatureSelector } from './components/TemperatureSelector';
import { IntensitySelector } from './components/IntensitySelector';
import { BrandSelector } from './components/BrandSelector';
import { PlanResults } from './components/PlanResults';
import { ShoppingList } from './components/ShoppingList';
import './App.css';

function App() {
  const [athleteWeight, setAthleteWeight] = useState(75);
  const [sportType, setSportType] = useState<SportType>(SportType.Run);
  const [duration, setDuration] = useState(1.5);
  const [temperature, setTemperature] = useState<TemperatureCondition>(TemperatureCondition.Moderate);
  const [intensity, setIntensity] = useState<IntensityLevel>(IntensityLevel.Moderate);

  const [selectedProducts, setSelectedProducts] = useState<any[]>([]);
  const [plan, setPlan] = useState<RaceNutritionPlan | null>(null);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  // Check if all required fields are filled
  const isFormValid = () => {
    return (
      athleteWeight > 0 &&
      sportType &&
      duration > 0 &&
      temperature &&
      intensity &&
      selectedProducts.length > 0
    );
  };

  const generatePlan = async () => {
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

      const products = selectedProducts.map(p => ({
        name: p.name,
        productType: p.productType,
        carbsG: p.carbsG,
        sodiumMg: p.sodiumMg,
        volumeMl: p.volumeMl || 0
      }));

      const newPlan = await api.generatePlan(athlete, race, products);
      setPlan(newPlan);
      setError(null);
    } catch (err) {
      const errorMsg = err instanceof Error ? err.message : 'Failed to generate plan';
      setError(errorMsg);
      console.error('Error generating plan:', err);
    } finally {
      setLoading(false);
    }
  };

  return (
    <div className="planner-container">
      <div className="header">
        <h1>Race Day Nutrition Planner</h1>
        <p className="subtitle">Personalized nutrition strategy</p>
      </div>

      <div className="content">
        {/* Left Section - Input Form */}
        <div className="form-section">
          <AthleteProfileForm
            athleteWeight={athleteWeight}
            onAthleteWeightChange={setAthleteWeight}
          />

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
          />

          <BrandSelector onBrandsSelected={setSelectedProducts} />

          {error && <div className="error-message" style={{ marginTop: '10px' }}>{error}</div>}

          <button 
            onClick={generatePlan} 
            className="btn btn-primary btn-lg btn-calculate"
            disabled={loading || !isFormValid()}
          >
            {loading ? 'Generating...' : 'Generate Plan'}
          </button>
        </div>

        {/* Right Section - Results */}
        <div className="results-container">
          {plan ? (
            <>
              <PlanResults plan={plan} />
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
