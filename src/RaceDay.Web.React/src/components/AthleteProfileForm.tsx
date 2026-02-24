import React from 'react';
import { ATHLETE_WEIGHT } from '../constants';

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
            min={ATHLETE_WEIGHT.MIN}
            max={ATHLETE_WEIGHT.MAX}
            step={ATHLETE_WEIGHT.STEP}
          />
          <span className="input-unit">kg</span>
        </div>
      </div>
    </div>
  );
};
