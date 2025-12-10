import React, { useEffect, useState } from 'react';
import { TemperatureCondition } from '../types';
import { api } from '../api';
import type { TemperatureMetadata } from '../types';

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
        <div className="form-group inline-group">
          <label htmlFor="temperature-buttons">Temperature</label>
          <p className="loading">Loading temperature options...</p>
        </div>
      </div>
    );
  }

  return (
    <div className="form-card">
      <div className="form-group inline-group">
        <label htmlFor="temperature-buttons">Temperature</label>
        <div id="temperature-buttons" className="temperature-buttons">
          {metadata.map((meta) => {
            const isSelected = temperature === meta.condition;
            
            return (
              <button
                key={meta.condition}
                className={`temp-btn ${isSelected ? 'active' : ''}`}
                onClick={() => onTemperatureChange(meta.condition)}
                title={meta.effects.join(', ')}
              >
                <div>{meta.condition}</div>
                <div className="btn-range">{meta.range}</div>
              </button>
            );
          })}
        </div>
      </div>
    </div>
  );
};
