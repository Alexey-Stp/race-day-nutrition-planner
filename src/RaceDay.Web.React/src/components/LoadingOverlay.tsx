import React from 'react';
import type { SportType } from '../types';
import { LOADING_MESSAGES } from '../constants';
import { getSportEmojiList } from '../constants/icons';

interface LoadingOverlayProps {
  messageIdx: number;
  sportType: SportType;
}

export const LoadingOverlay: React.FC<LoadingOverlayProps> = ({ messageIdx, sportType }) => {
  const emojiList = getSportEmojiList(sportType);
  const emoji = emojiList[messageIdx % emojiList.length];
  return (
    <div className="loading-overlay" role="status" aria-live="polite">
      <div className="loading-card">
        <div className="loading-track">
          <span className="loading-runner">{emoji}</span>
        </div>
        <p key={messageIdx} className="loading-msg">{LOADING_MESSAGES[messageIdx]}</p>
      </div>
    </div>
  );
};
