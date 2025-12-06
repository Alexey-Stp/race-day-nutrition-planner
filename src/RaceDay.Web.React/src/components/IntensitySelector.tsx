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
                <span className="intensity-icon">{description.icon}</span>
                <span className="intensity-label">{option}</span>
              </button>
            );
          })}
        </div>
      </div>
      {/* Show selected intensity details */}
      <div className="intensity-details">
        <div className="carb-range">
          <strong>Carbs:</strong> {IntensityDescriptions[intensity].carbRange}
        </div>
        <div className="heart-rate-zone">
          <strong>Heart Rate:</strong> {IntensityDescriptions[intensity].heartRateZone}
        </div>
        <div className="effects-list">
          {IntensityDescriptions[intensity].effects.map((effect) => (
            <div key={effect} className="effect-item">
              <span className="bullet">•</span>
              <span>{effect}</span>
            </div>
          ))}
        </div>
      </div>
    </div>
  );
};
