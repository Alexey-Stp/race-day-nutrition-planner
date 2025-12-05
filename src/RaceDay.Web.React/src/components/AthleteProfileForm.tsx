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
      <div className="form-group inline-group">
        <label htmlFor="weight">Weight</label>
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
          />
          <span className="input-unit">kg</span>
        </div>
      </div>
    </div>
  );
};
