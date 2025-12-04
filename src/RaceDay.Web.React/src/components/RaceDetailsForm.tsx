import React, { useEffect, useState, useCallback } from 'react';
import { SportType, IntensityLevel, type ActivityInfo } from '../types';
import { api } from '../api';
import { formatDuration } from '../nutritionCalculator';

interface RaceDetailsFormProps {
  sportType: SportType;
  duration: number;
  temperature: number;
  intensity: IntensityLevel;
  onSportTypeChange: (sport: SportType) => void;
  onDurationChange: (duration: number) => void;
  onTemperatureChange: (temp: number) => void;
  onIntensityChange: (intensity: IntensityLevel) => void;
}

export const RaceDetailsForm: React.FC<RaceDetailsFormProps> = ({
  duration,
  temperature,
  intensity,
  onSportTypeChange,
  onDurationChange,
  onTemperatureChange,
  onIntensityChange
}) => {
  const [activities, setActivities] = useState<ActivityInfo[]>([]);
  const [selectedActivity, setSelectedActivity] = useState<ActivityInfo | null>(null);
  const [loading, setLoading] = useState(true);
  const [minDuration, setMinDuration] = useState(0.5);
  const [maxDuration, setMaxDuration] = useState(24);
  const [currentDisplayDuration, setCurrentDisplayDuration] = useState(duration);
  const [durationValidationError, setDurationValidationError] = useState<string | null>(null);

  const loadActivities = useCallback(async () => {
    try {
      const data = await api.getActivities();
      setActivities(data);
      
      // Set default activity to Olympic Triathlon
      const defaultActivity = data.find(a => a.id === 'olympic-triathlon');
      if (defaultActivity) {
        setSelectedActivity(defaultActivity);
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
      setSelectedActivity(null);
      setDurationValidationError(null);
      return;
    }

    const activity = activities.find(a => a.id === activityId);
    if (activity) {
      setSelectedActivity(activity);
      setMinDuration(activity.minDurationHours);
      setMaxDuration(activity.maxDurationHours);
      onDurationChange(activity.bestTimeHours);
      setCurrentDisplayDuration(activity.bestTimeHours);
      onSportTypeChange(activity.sportType);
      setDurationValidationError(null);
    }
  };

  const handleDurationInput = (e: React.ChangeEvent<HTMLInputElement>) => {
    const newDuration = parseFloat(e.target.value);
    setCurrentDisplayDuration(newDuration);
  };

  const handleDurationChange = () => {
    onDurationChange(currentDisplayDuration);
    
    // Validate duration
    if (selectedActivity) {
      if (currentDisplayDuration < selectedActivity.minDurationHours) {
        setDurationValidationError(`Duration must be at least ${formatDuration(selectedActivity.minDurationHours)}`);
      } else if (currentDisplayDuration > selectedActivity.maxDurationHours) {
        setDurationValidationError(`Duration must not exceed ${formatDuration(selectedActivity.maxDurationHours)}`);
      } else {
        setDurationValidationError(null);
      }
    }
  };

  const getActivityEmoji = (sportType: SportType): string => {
    switch (sportType) {
      case SportType.Run:
        return '🏃';
      case SportType.Bike:
        return '🚴';
      case SportType.Triathlon:
        return '🏊';
      default:
        return '🏁';
    }
  };

  return (
    <div className="form-card">
      <h2>🏁 Race Details</h2>
      
      <div className="form-group">
        <div className="label-with-tooltip">
          <label htmlFor="activity">Select Activity</label>
          <span className="tooltip-icon" title="Choose a predefined activity or customize your own">ℹ️</span>
        </div>
        {loading ? (
          <p className="loading">Loading activities...</p>
        ) : (
          <>
            <select id="activity" onChange={handleActivityChange} className="form-control" defaultValue="">
              <option value="">-- Choose an activity --</option>
              {activities.map(activity => (
                <option key={activity.id} value={activity.id}>
                  {getActivityEmoji(activity.sportType)} {activity.name}
                </option>
              ))}
            </select>
            
            {selectedActivity && (
              <>
                <small className="form-text">{selectedActivity.description}</small>
                <small className="form-text"><strong>Best time:</strong> {selectedActivity.bestTimeFormatted}</small>
              </>
            )}
          </>
        )}
      </div>

      <div className="form-group">
        <div className="label-with-tooltip">
          <label htmlFor="duration">Duration</label>
          <span className="tooltip-icon" title="Use the slider to set your race duration">ℹ️</span>
        </div>
        <div className="duration-display">
          <strong>{formatDuration(currentDisplayDuration)}</strong>
        </div>
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
        {durationValidationError && (
          <small className="form-text validation-error">⚠️ {durationValidationError}</small>
        )}
      </div>

      <div className="form-group">
        <div className="label-with-tooltip">
          <label htmlFor="temperature">Temperature</label>
          <span className="tooltip-icon" title="Affects fluid and sodium requirements. Higher temperatures increase needs.">ℹ️</span>
        </div>
        <div className="input-group">
          <input
            type="number"
            id="temperature"
            value={temperature}
            onChange={(e) => onTemperatureChange(parseFloat(e.target.value) || 0)}
            className="form-control"
            min="-10"
            max="40"
            step="1"
            placeholder="e.g. 20"
          />
          <span className="input-unit">°C</span>
        </div>
      </div>

      <div className="form-group">
        <div className="label-with-tooltip">
          <label htmlFor="intensity">Intensity Level</label>
          <span className="tooltip-icon" title="Easy: Recovery pace, 60-70% max HR | Moderate: Steady pace, 70-85% max HR | Hard: Threshold/race pace, 85%+ max HR">ℹ️</span>
        </div>
        <select
          id="intensity"
          value={intensity}
          onChange={(e) => onIntensityChange(e.target.value as IntensityLevel)}
          className="form-control"
        >
          <option value={IntensityLevel.Easy}>Easy - Recovery/endurance pace</option>
          <option value={IntensityLevel.Moderate}>Moderate - Steady/comfortable pace</option>
          <option value={IntensityLevel.Hard}>Hard - Threshold/race pace</option>
        </select>
        <small className="form-text">Higher intensity increases carbohydrate consumption.</small>
      </div>
    </div>
  );
};
