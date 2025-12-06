import React from 'react';
import { TemperatureCondition, TemperatureDescriptions } from '../types';

interface TemperatureSelectorProps {
  temperature: TemperatureCondition;
  onTemperatureChange: (temp: TemperatureCondition) => void;
}

export const TemperatureSelector: React.FC<TemperatureSelectorProps> = ({
  temperature,
  onTemperatureChange
}) => {
  const temperatureOptions = [
    TemperatureCondition.Cold,
    TemperatureCondition.Moderate,
    TemperatureCondition.Hot
  ] as const;

  return (
    <div className="form-card">
      <div className="form-group inline-group">
        <label htmlFor="temperature-buttons">Temperature</label>
        <div id="temperature-buttons" className="temperature-buttons">
          {temperatureOptions.map((option) => {
            const description = TemperatureDescriptions[option];
            const isSelected = temperature === option;
            
            return (
              <button
                key={option}
                className={`temp-btn ${isSelected ? 'active' : ''}`}
                onClick={() => onTemperatureChange(option)}
                title={description.effects.join(', ')}
              >
                <div>{option}</div>
                <div className="btn-range">{description.range}</div>
              </button>
            );
          })}
        </div>
      </div>
    </div>
  );
};
