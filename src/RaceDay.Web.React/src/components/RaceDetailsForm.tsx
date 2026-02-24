import React, { useEffect, useState, useCallback, useMemo } from 'react';
import { SportType, type ActivityInfo } from '../types';
import { api } from '../api';
import { formatDuration } from '../utils';
import { ACTIVITY_ICONS } from '../constants/icons';

interface RaceDetailsFormProps {
  sportType: SportType;
  duration: number;
  onSportTypeChange: (sport: SportType) => void;
  onDurationChange: (duration: number) => void;
}

export const RaceDetailsForm: React.FC<RaceDetailsFormProps> = ({
  sportType: _sportType,  // Passed from parent but managed internally via callbacks
  duration,
  onSportTypeChange,
  onDurationChange
}) => {
  const [activities, setActivities] = useState<ActivityInfo[]>([]);
  const [loading, setLoading] = useState(true);
  const [minDuration, setMinDuration] = useState(0.5);
  const [maxDuration, setMaxDuration] = useState(24);
  const [currentDisplayDuration, setCurrentDisplayDuration] = useState(duration);
  const [currentActivityId, setCurrentActivityId] = useState<string>('');

  const loadActivities = useCallback(async () => {
    try {
      const [activitiesData, defaultsData] = await Promise.all([
        api.getActivities(),
        api.getDefaults()
      ]);
      setActivities(activitiesData);
      
      // Set default activity from backend
      const defaultActivityId = defaultsData.defaultActivityId;
      const defaultActivity = activitiesData.find(a => a.id === defaultActivityId);
      if (defaultActivity) {
        setCurrentActivityId(defaultActivityId);
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

  const hourMarkers = useMemo(() => {
    if (maxDuration <= minDuration) return [];
    
    const startHour = Math.ceil(minDuration);
    const endHour = Math.floor(maxDuration);
    const range = maxDuration - minDuration;
    const hours = [];
    
    for (let h = startHour; h <= endHour; h++) {
      hours.push({
        hour: h,
        position: ((h - minDuration) / range) * 100
      });
    }
    
    return hours;
  }, [minDuration, maxDuration]);

  return (
    <div className="form-card">
      <div className="activity-buttons">
        {loading ? (
          <p className="loading">Loading...</p>
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
              data-tooltip={activity.description}
            >
              <span className="activity-icon">{ACTIVITY_ICONS[activity.id]}</span>
              <span>{activity.name}</span>
            </button>
          ))
        )}
      </div>

      <div className="form-group">
        <label htmlFor="duration">Duration</label>
        <div className="duration-center-display">{formatDuration(currentDisplayDuration)}</div>
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
            step="0.01667"
          />
          <div className="slider-markers">
            {hourMarkers.map(({ hour, position }) => (
              <span key={hour} className="slider-marker" style={{ left: `${position}%` }}>
                {hour}h
              </span>
            ))}
          </div>
          <div className="slider-labels">
            <span>{formatDuration(minDuration)}</span>
            <span>{formatDuration(maxDuration)}</span>
          </div>
        </div>
      </div>
    </div>
  );
};
