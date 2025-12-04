import { useState } from 'react';
import { SportType, IntensityLevel, type ProductEditor, type ProductInfo, type RaceNutritionPlan, type ScheduleDisplayItem } from './types';
import { api } from './api';
import { AthleteProfileForm } from './components/AthleteProfileForm';
import { RaceDetailsForm } from './components/RaceDetailsForm';
import { ProductsEditor } from './components/ProductsEditor';
import { ProductSelector } from './components/ProductSelector';
import { PlanResults } from './components/PlanResults';
import './App.css';

function App() {
  const [athleteWeight, setAthleteWeight] = useState(75);
  const [sportType, setSportType] = useState<SportType>(SportType.Triathlon);
  const [duration, setDuration] = useState(2);
  const [temperature, setTemperature] = useState(20);
  const [intensity, setIntensity] = useState<IntensityLevel>(IntensityLevel.Moderate);

  const [gels, setGels] = useState<ProductEditor[]>([]);
  const [drinks, setDrinks] = useState<ProductEditor[]>([]);

  const [plan, setPlan] = useState<RaceNutritionPlan | null>(null);
  const [schedule, setSchedule] = useState<ScheduleDisplayItem[]>([]);
  const [scheduleTotalCalories, setScheduleTotalCalories] = useState(0);
  const [scheduleTotalCarbs, setScheduleTotalCarbs] = useState(0);
  const [scheduleTotalSodium, setScheduleTotalSodium] = useState(0);

  const calculatePlan = async () => {
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
      const newPlan = await api.generatePlan(athlete, race, allProducts);
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
        <h1>Race Day Nutrition Planner</h1>
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
          <>
            <PlanResults plan={plan} />
            
            <div className="product-schedule-section">
              <h2>Nutrition Schedule</h2>
              <p className="schedule-info">
                Weight: {athleteWeight} kg | Duration: {duration} hours
              </p>
              
              <div className="schedule-table-wrapper">
                <table className="schedule-table">
                  <thead>
                    <tr>
                      <th>Time</th>
                      <th>Product</th>
                      <th>Brand</th>
                      <th>Type</th>
                      <th>Cal</th>
                      <th>Carbs</th>
                      <th>Sodium</th>
                    </tr>
                  </thead>
                  <tbody>
                    {schedule.map((item, index) => (
                      <tr key={`${item.timeMin}-${item.productName}-${index}`}>
                        <td className="time-col">{item.timeMin}m</td>
                        <td className="product-col">{item.productName}</td>
                        <td className="brand-col">{item.brand}</td>
                        <td className="type-col">{item.type}</td>
                        <td className="number-col">{item.caloriesKcal.toFixed(0)}</td>
                        <td className="number-col">{item.carbsG.toFixed(1)}g</td>
                        <td className="number-col">{item.sodiumMg.toFixed(0)}mg</td>
                      </tr>
                    ))}
                  </tbody>
                  <tfoot>
                    <tr>
                      <td colSpan={4} className="total-label">TOTAL</td>
                      <td className="number-col total"><strong>{scheduleTotalCalories.toFixed(0)}</strong></td>
                      <td className="number-col total"><strong>{scheduleTotalCarbs.toFixed(1)}g</strong></td>
                      <td className="number-col total"><strong>{scheduleTotalSodium.toFixed(0)}mg</strong></td>
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
          </>
        )}

        {!plan && (
          <div className="product-browser-section">
            <h2>Browse Products</h2>
            <ProductSelector onProductAdded={addProductToEditor} />
          </div>
        )}
      </div>
    </div>
  );
}

export default App;
