import { useCallback, useEffect, useMemo, useState } from 'react';
import type { RaceNutritionPlan } from './types';
import { api } from './api';
import { usePlannerForm } from './hooks/usePlannerForm';
import { LOADING_MESSAGES, MIN_LOADING_MS } from './constants';

import { TopBar } from './components/TopBar';
import { TabBar, type MobilePane } from './components/TabBar';
import { InputsPanel } from './components/InputsPanel';
import { ResultsPanel } from './components/ResultsPanel';
import { RacePane } from './components/RacePane';
import { LoadingOverlay } from './components/LoadingOverlay';

import './styles/tokens.css';
import './styles/base.css';
import './styles/layout.css';
import './styles/inputs.css';
import './styles/results.css';
import './styles/race.css';
import './styles/loading.css';

const APP_VERSION = import.meta.env.VITE_APP_VERSION ?? 'dev';
const MSG_INTERVAL_MS = Math.floor(MIN_LOADING_MS / LOADING_MESSAGES.length);

function App() {
  const form = usePlannerForm();
  const {
    athleteWeight,
    sportType,
    duration,
    temperature,
    intensity,
    distancePreset,
    legs,
    useCaffeine,
    selectedProducts,
    isFormValid,
  } = form;

  const [plan, setPlan] = useState<RaceNutritionPlan | null>(null);
  // Snapshot of the inputs the current plan was generated from.
  const [planInputsKey, setPlanInputsKey] = useState<string | null>(null);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [messageIdx, setMessageIdx] = useState(0);
  const [raceMode, setRaceMode] = useState(false);
  const [mobilePane, setMobilePane] = useState<MobilePane>('setup');

  useEffect(() => {
    if (!loading) return;
    const interval = setInterval(
      () => setMessageIdx((i) => (i + 1) % LOADING_MESSAGES.length),
      MSG_INTERVAL_MS,
    );
    return () => clearInterval(interval);
  }, [loading]);

  const inputsKey = useMemo(
    () => JSON.stringify({
      athleteWeight,
      sportType,
      duration,
      temperature,
      intensity,
      distancePreset,
      legs,
      useCaffeine,
      products: selectedProducts.map((p) => p.id).sort((a, b) => a.localeCompare(b)),
    }),
    [athleteWeight, sportType, duration, temperature, intensity, distancePreset, legs, useCaffeine, selectedProducts],
  );

  const generatePlan = useCallback(async () => {
    if (selectedProducts.length === 0) {
      setError('Please select at least one product');
      return;
    }
    setMessageIdx(0);
    setLoading(true);
    setError(null);
    const generatedFrom = inputsKey;
    try {
      const athlete = { weightKg: athleteWeight };
      const isTri = sportType === 'Triathlon';
      const race = {
        sportType,
        durationHours: duration,
        temperature,
        intensity,
        // Leg inputs only apply to triathlon; omit otherwise so the API sees no legs.
        legs: isTri ? legs : null,
        preset: isTri ? distancePreset : null,
      };
      const products = selectedProducts.map((p) => ({
        name: p.name,
        productType: p.productType,
        carbsG: p.carbsG,
        sodiumMg: p.sodiumMg,
        volumeMl: p.volumeMl ?? 0,
        caffeineMg: p.caffeineMg ?? undefined,
      }));
      const [newPlan] = await Promise.all([
        api.generatePlan(athlete, race, products, useCaffeine),
        new Promise<void>((resolve) => setTimeout(resolve, MIN_LOADING_MS)),
      ]);
      setPlan(newPlan);
      setPlanInputsKey(generatedFrom);
      setMobilePane('plan');
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to generate plan');
      console.error('Error generating plan:', err);
    } finally {
      setLoading(false);
    }
  }, [selectedProducts, athleteWeight, sportType, duration, temperature, intensity, distancePreset, legs, useCaffeine, inputsKey]);

  const planReady = useMemo(() => !!plan?.nutritionSchedule?.length, [plan]);
  const inputsChanged = plan !== null && planInputsKey !== inputsKey;

  return (
    <div className="app-shell" data-race-mode={raceMode ? 'on' : 'off'}>
      <TopBar
        version={APP_VERSION}
        raceMode={raceMode}
        onToggleRaceMode={() => setRaceMode((v) => !v)}
        planReady={planReady}
      />

      <div className="workbench">
        <InputsPanel
          className={[
            mobilePane === 'setup' ? 'show' : '',
            raceMode ? 'hide-desktop' : '',
          ].join(' ').trim()}
          form={form}
          onGenerate={generatePlan}
          generating={loading}
          canGenerate={!loading && isFormValid()}
          error={error}
        />

        <ResultsPanel
          className={[
            mobilePane === 'plan' ? 'show' : '',
            raceMode ? 'hide-desktop' : '',
          ].join(' ').trim()}
          plan={plan}
          inputsChanged={inputsChanged}
          onRaceMode={() => setRaceMode(true)}
        />

        <RacePane
          className={[
            mobilePane === 'race' ? 'show' : '',
            raceMode ? 'show-desktop' : '',
          ].join(' ').trim()}
          plan={plan}
          active={raceMode || mobilePane === 'race'}
          onExit={() => setRaceMode(false)}
        />
      </div>

      <TabBar
        active={mobilePane}
        onChange={setMobilePane}
        planReady={planReady}
      />

      {loading && <LoadingOverlay messageIdx={messageIdx} sportType={sportType} />}
    </div>
  );
}

export default App;
