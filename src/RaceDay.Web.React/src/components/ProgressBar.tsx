import React, { useMemo } from 'react';
import { PROGRESS_THRESHOLDS } from '../constants';

interface ProgressBarProps {
  value: number;
  max: number;
  label: string;
  unit?: string;
}

export const ProgressBar: React.FC<ProgressBarProps> = ({ 
  value, 
  max, 
  label,
  unit = '' 
}) => {
  const percentage = useMemo(() => {
    return max > 0 ? (value / max) * 100 : 0;
  }, [value, max]);

  const status = useMemo(() => {
    if (percentage >= PROGRESS_THRESHOLDS.OPTIMAL_MIN && percentage <= PROGRESS_THRESHOLDS.OPTIMAL_MAX) {
      return 'optimal';
    }
    if (percentage > PROGRESS_THRESHOLDS.OPTIMAL_MAX) {
      return 'high';
    }
    return 'low';
  }, [percentage]);

  return (
    <div className="progress-item">
      <div className="progress-header">
        <span className="progress-label">{label}</span>
        <span className="progress-value">
          {value.toFixed(0)}{unit} / {max.toFixed(0)}{unit}
        </span>
      </div>
      <div className="progress-bar-container">
        <div className="progress-bar">
          <div
            className={`progress-fill progress-fill--${status}`}
            style={{ width: `${Math.min(percentage, 100)}%` }}
          />
        </div>
        <span className={`progress-percent progress-percent--${status}`}>
          {percentage.toFixed(0)}%
        </span>
      </div>
    </div>
  );
};
