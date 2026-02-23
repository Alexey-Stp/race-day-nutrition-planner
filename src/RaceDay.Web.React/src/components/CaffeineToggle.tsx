import React from 'react';

interface CaffeineToggleProps {
  useCaffeine: boolean;
  onToggle: (value: boolean) => void;
}

export const CaffeineToggle: React.FC<CaffeineToggleProps> = ({
  useCaffeine,
  onToggle
}) => {
  return (
    <div className="form-card">
      <h3 className="preferences-label">Preferences</h3>
      <div className="preference-section">
        <label className="preference-label">Caffeine</label>
        <p className="preference-description">Include caffeine products in recommendations</p>
        <div className="caffeine-toggle-buttons">
          <button
            className={`caffeine-btn ${!useCaffeine ? 'active' : ''}`}
            onClick={() => onToggle(false)}
          >
            Off
          </button>
          <button
            className={`caffeine-btn ${useCaffeine ? 'active' : ''}`}
            onClick={() => onToggle(true)}
          >
            On
          </button>
        </div>
      </div>
    </div>
  );
};
