import React from 'react';
import { IntensityLevel, IntensityDescriptions } from '../types';

interface IntensitySelectorProps {
  intensity: IntensityLevel;
  onIntensityChange: (intensity: IntensityLevel) => void;
}

export const IntensitySelector: React.FC<IntensitySelectorProps> = ({
  intensity,
  onIntensityChange
}) => {
  const intensityOptions = [
    IntensityLevel.Easy,
    IntensityLevel.Moderate,
    IntensityLevel.Hard
  ] as const;

  return (
    <div className="form-card">
      <div className="form-group inline-group">
        <label htmlFor="intensity-buttons">Intensity</label>
        <div id="intensity-buttons" className="intensity-buttons">
          {intensityOptions.map((option) => {
            const description = IntensityDescriptions[option];
            const isSelected = intensity === option;
            
            return (
              <button
                key={option}
                className={`intensity-btn ${isSelected ? 'active' : ''}`}
                onClick={() => onIntensityChange(option)}
                title={description.effects.join(', ')}
              >
                <div className="intensity-icon">{description.icon}</div>
                <div>{option}</div>
                <div className="btn-carbs">{description.carbRange}</div>
                <div className="btn-hr">{description.heartRateZone}</div>
              </button>
            );
          })}
        </div>
      </div>
    </div>
  );
};
