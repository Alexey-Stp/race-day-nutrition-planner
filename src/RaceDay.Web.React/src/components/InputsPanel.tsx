import React, { useEffect, useState } from 'react';
import type { ProductInfo, SportType, IntensityLevel, TemperatureCondition } from '../types';
import { SportType as SportTypeEnum, IntensityLevel as IntensityEnum, TemperatureCondition as TempEnum } from '../types';
import { api } from '../api';
import { ATHLETE_WEIGHT, DURATION } from '../constants';
import { formatDuration } from '../utils';
import { PRODUCT_GROUP_LABELS, PRODUCT_GROUP_ORDER } from '../constants/icons';

type FormReturn = ReturnType<typeof import('../hooks/usePlannerForm').usePlannerForm>;

interface InputsPanelProps {
  className?: string;
  form: FormReturn;
  onGenerate: () => void;
  generating: boolean;
  canGenerate: boolean;
  error: string | null;
}

export const InputsPanel: React.FC<InputsPanelProps> = ({
  className = '',
  form,
  onGenerate,
  generating,
  canGenerate,
  error,
}) => {
  const {
    athleteWeight, setAthleteWeight,
    sportType, setSportType,
    duration, setDuration,
    temperature, setTemperature,
    intensity, setIntensity,
    useCaffeine, setUseCaffeine,
    selectedProducts, setSelectedProducts,
  } = form;

  const [products, setProducts] = useState<ProductInfo[]>([]);
  const [brands, setBrands] = useState<string[]>([]);
  const [brand, setBrand] = useState<string>('');
  const [loadingProducts, setLoadingProducts] = useState(true);

  useEffect(() => {
    let alive = true;
    (async () => {
      try {
        const data = await api.getProducts();
        if (!alive) return;
        setProducts(data);
        const uniq = Array.from(new Set(data.map((p) => p.brand).filter(Boolean))).sort((a, b) => a.localeCompare(b));
        setBrands(uniq);
        if (uniq[0]) {
          setBrand(uniq[0]);
          const def = data.filter((p) => p.brand === uniq[0] && p.productType !== 'recovery');
          setSelectedProducts(def);
        }
      } catch (e) {
        console.error(e);
      } finally {
        if (alive) setLoadingProducts(false);
      }
    })();
    return () => { alive = false; };
  // eslint-disable-next-line react-hooks/exhaustive-deps
  }, []);

  const selectedIds = new Set(selectedProducts.map((p) => p.id));

  const toggleProduct = (p: ProductInfo) => {
    if (selectedIds.has(p.id)) {
      setSelectedProducts(selectedProducts.filter((x) => x.id !== p.id));
    } else {
      setSelectedProducts([...selectedProducts, p]);
    }
  };

  const changeBrand = (b: string) => {
    setBrand(b);
    const def = products.filter((p) => (!b || p.brand === b) && p.productType !== 'recovery');
    setSelectedProducts(def);
  };

  const visible = products.filter((p) => !brand || p.brand === brand);
  const grouped = PRODUCT_GROUP_ORDER.map((g) => ({
    type: g as string,
    label: PRODUCT_GROUP_LABELS[g],
    items: visible.filter((p) => p.productType === g),
  })).filter((g) => g.items.length > 0);

  return (
    <aside className={`panel-inputs ${className}`}>
      <div className="panel-inputs-inner">
        <Section label="Athlete">
          <div className="field">
            <label htmlFor="weight">Weight</label>
            <div className="num-stepper">
              <button type="button" onClick={() => setAthleteWeight(Math.max(ATHLETE_WEIGHT.MIN, +(athleteWeight - 0.5).toFixed(1)))}>−</button>
              <input
                id="weight"
                type="number"
                min={ATHLETE_WEIGHT.MIN}
                max={ATHLETE_WEIGHT.MAX}
                step={ATHLETE_WEIGHT.STEP}
                value={athleteWeight}
                onChange={(e) => setAthleteWeight(Number(e.target.value))}
              />
              <span className="unit">kg</span>
              <button type="button" onClick={() => setAthleteWeight(Math.min(ATHLETE_WEIGHT.MAX, +(athleteWeight + 0.5).toFixed(1)))}>+</button>
            </div>
          </div>
        </Section>

        <Section label="Sport">
          <div className="pill-row">
            {(['Run', 'Bike', 'Triathlon'] as SportType[]).map((s) => (
              <button
                key={s}
                type="button"
                className={`pill ${sportType === s ? 'is-active' : ''}`}
                onClick={() => setSportType(SportTypeEnum[s])}
              >
                {s}
              </button>
            ))}
          </div>
        </Section>

        <Section label={`Duration · ${formatDuration(duration)}`}>
          <input
            className="slider"
            type="range"
            min={DURATION.MIN}
            max={8}
            step={0.25}
            value={duration}
            onChange={(e) => setDuration(Number(e.target.value))}
          />
          <div className="slider-ticks">
            <span>30m</span><span>2h</span><span>4h</span><span>6h</span><span>8h</span>
          </div>
        </Section>

        <Section label="Intensity">
          <div className="pill-row">
            {(['Easy', 'Moderate', 'Hard'] as IntensityLevel[]).map((lvl) => (
              <button
                key={lvl}
                type="button"
                className={`pill ${intensity === lvl ? 'is-active' : ''}`}
                onClick={() => setIntensity(IntensityEnum[lvl])}
              >
                {lvl}
              </button>
            ))}
          </div>
        </Section>

        <Section label="Temperature">
          <div className="pill-row">
            {(['Cold', 'Moderate', 'Hot'] as TemperatureCondition[]).map((t) => (
              <button
                key={t}
                type="button"
                className={`pill ${temperature === t ? 'is-active' : ''}`}
                onClick={() => setTemperature(TempEnum[t])}
              >
                {t}
              </button>
            ))}
          </div>
        </Section>

        <Section label="Caffeine">
          <label className="switch">
            <input type="checkbox" checked={useCaffeine} onChange={(e) => setUseCaffeine(e.target.checked)} />
            <span className="switch-ui" />
            <span className="switch-label">{useCaffeine ? 'Enabled' : 'Off'}</span>
          </label>
        </Section>

        <Section label="Brand & products">
          {loadingProducts ? (
            <p className="muted">Loading products…</p>
          ) : (
            <>
              <select className="select" value={brand} onChange={(e) => changeBrand(e.target.value)}>
                <option value="">All brands</option>
                {brands.map((b) => <option key={b} value={b}>{b}</option>)}
              </select>

              <div className="product-groups">
                {grouped.map((g) => (
                  <div key={g.type} className="product-group">
                    <div className="product-group-head">
                      <span>{g.label}</span>
                      <span className="muted">
                        {g.items.filter((p) => selectedIds.has(p.id)).length}/{g.items.length}
                      </span>
                    </div>
                    <div className="product-list">
                      {g.items.map((p) => (
                        <label key={p.id} className={`product-row ${selectedIds.has(p.id) ? 'is-on' : ''}`}>
                          <input
                            type="checkbox"
                            checked={selectedIds.has(p.id)}
                            onChange={() => toggleProduct(p)}
                          />
                          <span className="product-name">{p.name}</span>
                          <span className="product-meta">
                            {p.carbsG.toFixed(0)}g
                            {p.caffeineMg ? ` · ${p.caffeineMg}mg caf` : ''}
                          </span>
                        </label>
                      ))}
                    </div>
                  </div>
                ))}
              </div>
            </>
          )}
        </Section>

        {error && <div className="error">{error}</div>}

        <button
          type="button"
          className="btn-generate"
          disabled={!canGenerate}
          onClick={onGenerate}
        >
          {generating ? 'Generating…' : 'Generate plan'}
        </button>
      </div>
    </aside>
  );
};

const Section: React.FC<{ label: string; children: React.ReactNode }> = ({ label, children }) => (
  <div className="section">
    <div className="section-label">{label}</div>
    {children}
  </div>
);
