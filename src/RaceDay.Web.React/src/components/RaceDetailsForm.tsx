import React, { useEffect, useState, useCallback } from 'react';
import { SportType, IntensityLevel, type ActivityInfo } from '../types';
import { api } from '../api';
import { formatDuration } from '../utils';

interface RaceDetailsFormProps {
  sportType: SportType;
  duration: number;
  intensity: IntensityLevel;
  onSportTypeChange: (sport: SportType) => void;
  onDurationChange: (duration: number) => void;
  onIntensityChange: (intensity: IntensityLevel) => void;
}

export const RaceDetailsForm: React.FC<RaceDetailsFormProps> = ({
  duration,
  intensity,
  onSportTypeChange,
  onDurationChange,
  onIntensityChange
}) => {
  const [activities, setActivities] = useState<ActivityInfo[]>([]);
  const [loading, setLoading] = useState(true);
  const [minDuration, setMinDuration] = useState(0.5);
  const [maxDuration, setMaxDuration] = useState(24);
  const [currentDisplayDuration, setCurrentDisplayDuration] = useState(duration);

  const loadActivities = useCallback(async () => {
    try {
      const data = await api.getActivities();
      setActivities(data);
      
      // Set default activity to Olympic Triathlon
      const defaultActivity = data.find(a => a.id === 'olympic-triathlon');
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

  const handleActivityChange = (e: React.ChangeEvent<HTMLSelectElement>) => {
    const activityId = e.target.value;
    if (!activityId) {
      return;
    }

    const activity = activities.find(a => a.id === activityId);
    if (activity) {
      setMinDuration(activity.minDurationHours);
      setMaxDuration(activity.maxDurationHours);
      onDurationChange(activity.bestTimeHours);
      setCurrentDisplayDuration(activity.bestTimeHours);
      onSportTypeChange(activity.sportType);
    }
  };

  const handleDurationInput = (e: React.ChangeEvent<HTMLInputElement>) => {
    const newDuration = Number.parseFloat(e.target.value);
    setCurrentDisplayDuration(newDuration);
  };

  const handleDurationChange = () => {
    onDurationChange(currentDisplayDuration);
  };

  return (
    <div className="form-card">
      <div className="form-group inline-group">
        <label htmlFor="activity">Activity</label>
        {loading ? (
          <p className="loading">Loading...</p>
        ) : (
          <select id="activity" onChange={handleActivityChange} className="form-control" defaultValue="">
            <option value="">Choose activity</option>
            {activities.map(activity => (
              <option key={activity.id} value={activity.id}>
                {activity.name}
              </option>
            ))}
          </select>
        )}
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
