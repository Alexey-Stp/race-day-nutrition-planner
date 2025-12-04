import React from 'react';

interface AthleteProfileFormProps {
  athleteWeight: number;
  onAthleteWeightChange: (weight: number) => void;
}

export const AthleteProfileForm: React.FC<AthleteProfileFormProps> = ({
  athleteWeight,
  onAthleteWeightChange
}) => {
  return (
    <div className="form-card">
      <h2>👤 Athlete Profile</h2>
      <div className="form-group">
        <label htmlFor="weight">Body Weight</label>
        <div className="input-group">
          <input
            type="number"
            id="weight"
            value={athleteWeight}
            onChange={(e) => onAthleteWeightChange(Number.parseFloat(e.target.value) || 0)}
            className="form-control"
            min="40"
            max="150"
            step="0.5"
            placeholder="e.g. 75"
          />
          <span className="input-unit">kg</span>
        </div>
        <small className="form-text">Athlete's body weight affects fluid and sodium recommendations.</small>
      </div>
    </div>
  );
};
