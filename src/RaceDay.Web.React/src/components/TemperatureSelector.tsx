import React, { useEffect, useState } from 'react';
import { TemperatureCondition } from '../types';
import { api } from '../api';
import type { TemperatureMetadata } from '../types';
import { TEMP_ICONS } from '../constants/icons';

interface TemperatureSelectorProps {
  temperature: TemperatureCondition;
  onTemperatureChange: (temp: TemperatureCondition) => void;
  useCaffeine: boolean;
  onCaffeineToggle: (value: boolean) => void;
}

export const TemperatureSelector: React.FC<TemperatureSelectorProps> = ({
  temperature,
  onTemperatureChange,
  useCaffeine,
  onCaffeineToggle,
}) => {
  const [metadata, setMetadata] = useState<TemperatureMetadata[]>([]);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    const loadMetadata = async () => {
      try {
        const data = await api.getTemperatureMetadata();
        setMetadata(data);
      } catch (error) {
        console.error('Error loading temperature metadata:', error);
      } finally {
        setLoading(false);
      }
    };
    loadMetadata();
  }, []);

  if (loading) {
    return (
      <div className="form-card">
        <p className="loading">Loading...</p>
      </div>
    );
  }

  return (
    <div className="form-card">
      <div className="temperature-buttons">
        {metadata.map((meta) => {
          const isSelected = temperature === meta.condition;

          return (
            <button
              key={meta.condition}
              className={`temp-btn ${isSelected ? 'active' : ''}`}
              onClick={() => onTemperatureChange(meta.condition)}
              data-tooltip={meta.effects.join(' · ')}
            >
              <span className="temp-icon">{TEMP_ICONS[meta.condition]}</span>
              <span>{meta.range}</span>
            </button>
          );
        })}
      </div>
      <label className="caffeine-compact-row">
        <span className="caffeine-compact-label">☕ Caffeine</span>
        <span className="ios-switch">
          <input
            type="checkbox"
            checked={useCaffeine}
            onChange={(e) => onCaffeineToggle(e.target.checked)}
          />
          <span className="ios-switch-track" />
        </span>
      </label>
    </div>
  );
};
