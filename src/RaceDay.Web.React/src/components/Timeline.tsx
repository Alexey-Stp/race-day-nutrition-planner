import React from 'react';
import type { NutritionEvent } from '../types';
import { formatDuration, formatPhase, formatAction } from '../utils';

interface TimelineEventCardProps {
  event: NutritionEvent;
  index: number;
  showCaffeine: boolean;
}

export const TimelineEventCard: React.FC<TimelineEventCardProps> = ({
  event,
  index,
  showCaffeine
}) => {
  const isPreRace = event.timeMin < 0;
  const isSip = event.action === 'Sip';
  
  return (
    <div className={`timeline-event ${isSip ? 'timeline-event--sip' : ''} ${isPreRace ? 'timeline-event--prerace' : ''}`}>
      {/* Time badge */}
      <div className="timeline-event__time">
        {isPreRace 
          ? `${Math.abs(event.timeMin)}m before`
          : formatDuration(event.timeMin / 60)
        }
      </div>

      {/* Event content */}
      <div className="timeline-event__content">
        <div className="timeline-event__header">
          <span className="timeline-event__phase">{formatPhase(event.phase)}</span>
          {event.hasCaffeine && showCaffeine && (
            <span className="timeline-event__badge timeline-event__badge--caffeine">
              ☕ {event.caffeineMg}mg
            </span>
          )}
        </div>

        <div className="timeline-event__product">{event.productName}</div>
        <div className="timeline-event__action">{formatAction(event)}</div>

        {/* Nutrition info */}
        <div className="timeline-event__nutrition">
          {event.carbsInEvent && event.carbsInEvent > 0 ? (
            <>
              <span className="timeline-event__carbs">
                <strong>{event.carbsInEvent.toFixed(1)}g</strong> carbs
              </span>
              <span className="timeline-event__total">
                {event.totalCarbsSoFar.toFixed(0)}g total
              </span>
            </>
          ) : (
            <span className="timeline-event__total">
              {event.totalCarbsSoFar.toFixed(0)}g total
            </span>
          )}
        </div>

        {/* Progress indicator - visual accumulation */}
        <div className="timeline-event__progress">
          <div 
            className="timeline-event__progress-bar"
            style={{ 
              width: `${Math.min((event.totalCarbsSoFar / 1000) * 100, 100)}%` 
            }}
          />
        </div>
      </div>
    </div>
  );
};

interface TimelineSectionProps {
  title: string;
  events: NutritionEvent[];
  showCaffeine: boolean;
}

export const TimelineSection: React.FC<TimelineSectionProps> = ({
  title,
  events,
  showCaffeine
}) => {
  if (events.length === 0) return null;

  return (
    <div className="timeline-section">
      <h3 className="timeline-section__title">{title}</h3>
      <div className="timeline-section__events">
        {events.map((event, index) => (
          <TimelineEventCard
            key={`${index}-${event.timeMin}-${event.productName}`}
            event={event}
            index={index}
            showCaffeine={showCaffeine}
          />
        ))}
      </div>
    </div>
  );
};
