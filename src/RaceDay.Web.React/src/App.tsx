import { useState } from 'react';
import { SportType, IntensityLevel, type ProductEditor, type ProductInfo, type RaceNutritionPlan, type ScheduleDisplayItem } from './types';
import { generatePlan } from './nutritionCalculator';
import { AthleteProfileForm } from './components/AthleteProfileForm';
import { RaceDetailsForm } from './components/RaceDetailsForm';
import { ProductsEditor } from './components/ProductsEditor';
import { ProductSelector } from './components/ProductSelector';
import { PlanResults } from './components/PlanResults';
import './App.css';

// Constants
const CALORIES_PER_KG_ESTIMATE = 30; // Rough daily calorie estimate per kg of body weight

function App() {
  const [athleteWeight, setAthleteWeight] = useState(75);
  const [sportType, setSportType] = useState<SportType>(SportType.Triathlon);
  const [duration, setDuration] = useState(2);
  const [temperature, setTemperature] = useState(20);
  const [intensity, setIntensity] = useState<IntensityLevel>(IntensityLevel.Moderate);

  const [gels, setGels] = useState<ProductEditor[]>([
    { name: 'Maurten Gel 100', carbsG: 25, sodiumMg: 100, volumeMl: 0, brand: 'Maurten' },
    { name: 'Gu Energy Gel', carbsG: 22, sodiumMg: 50, volumeMl: 0, brand: 'GU' }
  ]);

  const [drinks, setDrinks] = useState<ProductEditor[]>([
    { name: 'Maurten Drink 500ml', carbsG: 30, sodiumMg: 300, volumeMl: 500, brand: 'Maurten' },
    { name: 'Isotonic Drink 500ml', carbsG: 25, sodiumMg: 200, volumeMl: 500, brand: 'Generic' }
  ]);

  const [plan, setPlan] = useState<RaceNutritionPlan | null>(null);
  const [schedule, setSchedule] = useState<ScheduleDisplayItem[]>([]);
  const [scheduleTotalCalories, setScheduleTotalCalories] = useState(0);
  const [scheduleTotalCarbs, setScheduleTotalCarbs] = useState(0);
  const [scheduleTotalSodium, setScheduleTotalSodium] = useState(0);

  const calculatePlan = () => {
    const athlete = { weightKg: athleteWeight };
    const race = {
      sportType,
      durationHours: duration,
      temperatureC: temperature,
      intensity
    };

    const allProducts: ProductEditor[] = [
      ...gels.filter(p => p.name.trim() !== ''),
      ...drinks.filter(p => p.name.trim() !== '')
    ];

    if (allProducts.length === 0) {
      return;
    }

    try {
      const newPlan = generatePlan(race, athlete, allProducts);
      setPlan(newPlan);
      buildSchedule(newPlan);
    } catch (error) {
      console.error('Error generating plan:', error);
      // Could show error message to user
    }
  };

  const buildSchedule = (plan: RaceNutritionPlan) => {
    const newSchedule: ScheduleDisplayItem[] = [];
    let totalCalories = 0;
    let totalCarbs = 0;
    let totalSodium = 0;

    // Create product map
    const allProductsMap = new Map<string, { type: string; carbsG: number; sodiumMg: number; brand: string }>();
    
    gels.filter(p => p.name.trim() !== '').forEach(gel => {
      allProductsMap.set(gel.name, {
        type: 'gel',
        carbsG: gel.carbsG,
        sodiumMg: gel.sodiumMg,
        brand: gel.brand || ''
      });
    });

    drinks.filter(p => p.name.trim() !== '').forEach(drink => {
      allProductsMap.set(drink.name, {
        type: 'drink',
        carbsG: drink.carbsG,
        sodiumMg: drink.sodiumMg,
        brand: drink.brand || ''
      });
    });

    // Get unique time points
    const uniqueTimes = Array.from(new Set(plan.schedule.map(s => s.timeMin))).sort((a, b) => a - b);

    uniqueTimes.forEach(timeMin => {
      const itemsAtTime = plan.schedule.filter(s => s.timeMin === timeMin);

      itemsAtTime.forEach(item => {
        const productInfo = allProductsMap.get(item.productName);
        if (productInfo) {
          const calories = productInfo.carbsG * 4; // 4 kcal per gram of carbs

          newSchedule.push({
            timeMin,
            productName: item.productName,
            brand: productInfo.brand,
            type: productInfo.type,
            caloriesKcal: calories,
            carbsG: productInfo.carbsG,
            sodiumMg: productInfo.sodiumMg
          });

          totalCalories += calories;
          totalCarbs += productInfo.carbsG;
          totalSodium += productInfo.sodiumMg;
        }
      });
    });

    setSchedule(newSchedule);
    setScheduleTotalCalories(totalCalories);
    setScheduleTotalCarbs(totalCarbs);
    setScheduleTotalSodium(totalSodium);
  };

  const addProductToEditor = (product: ProductInfo) => {
    const editor: ProductEditor = {
      name: product.name,
      carbsG: product.carbsG,
      sodiumMg: product.sodiumMg,
      volumeMl: product.volumeMl,
      brand: product.brand
    };

    if (product.productType === 'gel') {
      setGels([...gels, editor]);
    } else if (product.productType === 'drink') {
      setDrinks([...drinks, editor]);
    }
  };

  return (
    <div className="planner-container">
      <div className="header">
        <h1>🏁 Race Day Nutrition Planner</h1>
        <p className="subtitle">Calculate your personalized nutrition strategy for race day</p>
      </div>

      <div className="content">
        <div className="form-section">
          <AthleteProfileForm
            athleteWeight={athleteWeight}
            onAthleteWeightChange={setAthleteWeight}
          />
          <RaceDetailsForm
            sportType={sportType}
            duration={duration}
            temperature={temperature}
            intensity={intensity}
            onSportTypeChange={setSportType}
            onDurationChange={setDuration}
            onTemperatureChange={setTemperature}
            onIntensityChange={setIntensity}
          />
          
          <button onClick={calculatePlan} className="btn btn-primary btn-lg btn-calculate">
            📊 Calculate Nutrition Plan
          </button>
        </div>

        {plan && (
          <div className="product-schedule-section">
            <h2>Your Product Schedule</h2>
            <p className="schedule-info">
              Weight: {athleteWeight} kg | Duration: {duration} hours | 
              Estimated daily calories: {(athleteWeight * CALORIES_PER_KG_ESTIMATE).toFixed(0)} kcal (used for calculation)
            </p>
            
            <div className="schedule-table-wrapper">
              <table className="schedule-table">
                <thead>
                  <tr>
                    <th>Time (min)</th>
                    <th>Product</th>
                    <th>Brand</th>
                    <th>Type</th>
                    <th>Calories (kcal)</th>
                    <th>Carbs (g)</th>
                    <th>Sodium (mg)</th>
                  </tr>
                </thead>
                <tbody>
                  {schedule.map((item, index) => (
                    <tr key={index}>
                      <td className="time-col">{item.timeMin}</td>
                      <td className="product-col">{item.productName}</td>
                      <td className="brand-col">{item.brand}</td>
                      <td className="type-col">{item.type}</td>
                      <td className="number-col">{item.caloriesKcal.toFixed(0)}</td>
                      <td className="number-col">{item.carbsG.toFixed(1)}</td>
                      <td className="number-col">{item.sodiumMg.toFixed(0)}</td>
                    </tr>
                  ))}
                </tbody>
                <tfoot>
                  <tr>
                    <td colSpan={4} className="total-label">TOTAL</td>
                    <td className="number-col total"><strong>{scheduleTotalCalories.toFixed(0)}</strong></td>
                    <td className="number-col total"><strong>{scheduleTotalCarbs.toFixed(1)}</strong></td>
                    <td className="number-col total"><strong>{scheduleTotalSodium.toFixed(0)}</strong></td>
                  </tr>
                </tfoot>
              </table>
            </div>

            <ProductsEditor
              gels={gels}
              drinks={drinks}
              onGelsChange={setGels}
              onDrinksChange={setDrinks}
            />
          </div>
        )}

        <div className="product-browser-section">
          <h2>Browse Products</h2>
          <ProductSelector onProductAdded={addProductToEditor} />
        </div>

        <PlanResults plan={plan} />
      </div>
    </div>
  );
}

export default App;
