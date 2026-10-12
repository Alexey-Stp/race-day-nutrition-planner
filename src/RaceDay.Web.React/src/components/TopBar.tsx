import React from 'react';

interface TopBarProps {
  version: string;
  raceMode: boolean;
  onToggleRaceMode: () => void;
  planReady: boolean;
}

export const TopBar: React.FC<TopBarProps> = ({ version, raceMode, onToggleRaceMode, planReady }) => {
  return (
    <header className="topbar">
      <div className="topbar-brand">
        <span className="topbar-mark" aria-hidden>▲</span>
        <h1>Race Day Nutrition</h1>
        <span className="topbar-meta">v{version}</span>
      </div>
      <div className="topbar-actions">
        <button
          type="button"
          className={`btn-race-mode ${raceMode ? 'is-on' : ''}`}
          onClick={onToggleRaceMode}
          disabled={!planReady}
          aria-pressed={raceMode}
        >
          <span className="dot" aria-hidden />
          {raceMode ? 'Exit race mode' : 'Race mode'}
        </button>
      </div>
    </header>
  );
};
