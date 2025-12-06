import React, { useEffect, useState } from 'react';
import { IntensityLevel } from '../types';
import { api } from '../api';
import type { IntensityMetadata } from '../types';

interface IntensitySelectorProps {
  intensity: IntensityLevel;
  onIntensityChange: (intensity: IntensityLevel) => void;
}

export const IntensitySelector: React.FC<IntensitySelectorProps> = ({
  intensity,
  onIntensityChange
}) => {
  const [metadata, setMetadata] = useState<IntensityMetadata[]>([]);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    const loadMetadata = async () => {
      try {
        const data = await api.getIntensityMetadata();
        setMetadata(data);
      } catch (error) {
        console.error('Error loading intensity metadata:', error);
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
          <label htmlFor="intensity-buttons">Intensity</label>
          <p className="loading">Loading intensity options...</p>
        </div>
      </div>
    );
  }

  return (
    <div className="form-card">
      <div className="form-group inline-group">
        <label htmlFor="intensity-buttons">Intensity</label>
        <div id="intensity-buttons" className="intensity-buttons">
          {metadata.map((meta) => {
            const isSelected = intensity === meta.level;
            
            return (
              <button
                key={meta.level}
                className={`intensity-btn ${isSelected ? 'active' : ''}`}
                onClick={() => onIntensityChange(meta.level)}
                title={meta.effects.join(', ')}
              >
                <div className="intensity-icon">{meta.icon}</div>
                <div>{meta.level}</div>
                <div className="btn-carbs">{meta.carbRange}</div>
                <div className="btn-hr">{meta.heartRateZone}</div>
              </button>
            );
          })}
        </div>
      </div>
    </div>
  );
};
