import React, { useEffect, useState } from 'react';
import { IntensityLevel } from '../types';
import { api } from '../api';
import type { IntensityMetadata } from '../types';

interface IntensitySelectorProps {
  intensity: IntensityLevel;
  onIntensityChange: (intensity: IntensityLevel) => void;
}

const INTENSITY_LABELS: Record<string, string> = {
  Easy: 'Easy',
  Moderate: 'Training',
  Hard: 'Race',
};

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
        <p className="loading">Loading...</p>
      </div>
    );
  }

  return (
    <div className="form-card">
      <div className="intensity-buttons">
        {metadata.map((meta) => {
          const isSelected = intensity === meta.level;
          const label = INTENSITY_LABELS[meta.level] || meta.level;

          return (
            <button
              key={meta.level}
              className={`intensity-btn ${isSelected ? 'active' : ''}`}
              onClick={() => onIntensityChange(meta.level)}
              data-tooltip={meta.effects.join(' · ')}
            >
              <span className="intensity-icon">{meta.icon}</span>
              <span>{label}</span>
            </button>
          );
        })}
      </div>
    </div>
  );
};
