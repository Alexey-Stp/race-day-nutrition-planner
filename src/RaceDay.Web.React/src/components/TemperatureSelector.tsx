import React, { useEffect, useState } from 'react';
import { TemperatureCondition } from '../types';
import { api } from '../api';
import type { TemperatureMetadata } from '../types';

const TEMP_ICONS: Record<string, string> = {
  Cold: '❄️',
  Moderate: '🌤️',
  Hot: '🌡️',
};

interface TemperatureSelectorProps {
  temperature: TemperatureCondition;
  onTemperatureChange: (temp: TemperatureCondition) => void;
}

export const TemperatureSelector: React.FC<TemperatureSelectorProps> = ({
  temperature,
  onTemperatureChange
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
    </div>
  );
};
