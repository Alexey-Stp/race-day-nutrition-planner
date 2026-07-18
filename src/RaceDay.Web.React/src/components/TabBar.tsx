import React from 'react';

export type MobilePane = 'setup' | 'plan' | 'race';

interface TabBarProps {
  active: MobilePane;
  onChange: (p: MobilePane) => void;
  planReady: boolean;
}

const TABS: { id: MobilePane; label: string; needsPlan?: boolean }[] = [
  { id: 'setup', label: 'Setup' },
  { id: 'plan', label: 'Plan', needsPlan: true },
  { id: 'race', label: 'Race', needsPlan: true },
];

export const TabBar: React.FC<TabBarProps> = ({ active, onChange, planReady }) => {
  return (
    <nav className="tabbar" aria-label="Sections">
      {TABS.map((t) => {
        const disabled = !!t.needsPlan && !planReady;
        return (
          <button
            key={t.id}
            type="button"
            className="tabbar-btn"
            aria-pressed={active === t.id}
            disabled={disabled}
            onClick={() => onChange(t.id)}
          >
            {t.label}
          </button>
        );
      })}
    </nav>
  );
};
