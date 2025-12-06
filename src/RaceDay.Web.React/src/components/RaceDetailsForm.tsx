import React, { useEffect, useState, useCallback } from 'react';
import { SportType, type ActivityInfo } from '../types';
import { api } from '../api';
import { formatDuration } from '../utils';

interface RaceDetailsFormProps {
  sportType: SportType;
  duration: number;
  onSportTypeChange: (sport: SportType) => void;
  onDurationChange: (duration: number) => void;
}

export const RaceDetailsForm: React.FC<RaceDetailsFormProps> = ({
  duration,
  onSportTypeChange,
  onDurationChange
}) => {
  const [activities, setActivities] = useState<ActivityInfo[]>([]);
  const [loading, setLoading] = useState(true);
  const [minDuration, setMinDuration] = useState(0.5);
  const [maxDuration, setMaxDuration] = useState(24);
  const [currentDisplayDuration, setCurrentDisplayDuration] = useState(duration);
  const [currentActivityId, setCurrentActivityId] = useState<string>('run');

  const loadActivities = useCallback(async () => {
    try {
      const data = await api.getActivities();
      setActivities(data);
      
      // Set default activity to Run
      const defaultActivity = data.find(a => a.id === 'run');
      if (defaultActivity) {
        onSportTypeChange(defaultActivity.sportType);
        setMinDuration(defaultActivity.minDurationHours);
        setMaxDuration(defaultActivity.maxDurationHours);
        onDurationChange(defaultActivity.bestTimeHours);
        setCurrentDisplayDuration(defaultActivity.bestTimeHours);
      }
    } catch (error) {
      console.error('Error loading activities:', error);
    } finally {
      setLoading(false);
    }
  }, [onSportTypeChange, onDurationChange]);

  useEffect(() => {
    loadActivities();
  }, [loadActivities]);

  useEffect(() => {
    setCurrentDisplayDuration(duration);
  }, [duration]);

  const handleDurationInput = (e: React.ChangeEvent<HTMLInputElement>) => {
    const newDuration = Number.parseFloat(e.target.value);
    setCurrentDisplayDuration(newDuration);
  };

  const handleDurationChange = () => {
    onDurationChange(currentDisplayDuration);
  };

  return (
    <div className="form-card">
      <h2>Sport Type</h2>
      <div className="form-group">
        <div className="activity-buttons">
          {loading ? (
            <p className="loading">Loading activities...</p>
          ) : (
            activities.map(activity => (
              <button
                key={activity.id}
                className={`activity-btn ${currentActivityId === activity.id ? 'active' : ''}`}
                onClick={() => {
                  setCurrentActivityId(activity.id);
                  setMinDuration(activity.minDurationHours);
                  setMaxDuration(activity.maxDurationHours);
                  onDurationChange(activity.bestTimeHours);
                  setCurrentDisplayDuration(activity.bestTimeHours);
                  onSportTypeChange(activity.sportType);
                }}
              >
                {activity.name}
              </button>
            ))
          )}
        </div>
      </div>

      <div className="form-group inline-group">
        <label htmlFor="duration">Duration</label>
        <div className="duration-display">{formatDuration(currentDisplayDuration)}</div>
      </div>
      <div className="form-group">
        <div className="slider-group">
          <input
            type="range"
            id="duration"
            value={currentDisplayDuration}
            onInput={handleDurationInput}
            onChange={handleDurationChange}
            className="form-slider"
            min={minDuration}
            max={maxDuration}
            step="0.25"
          />
          <div className="slider-labels">
            <span>{formatDuration(minDuration)}</span>
            <span>{formatDuration(maxDuration)}</span>
          </div>
        </div>
      </div>
    </div>
  );
};
