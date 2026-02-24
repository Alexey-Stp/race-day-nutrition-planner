import React from 'react';

interface SectionProps {
  children: React.ReactNode;
  spacing?: 'sm' | 'md' | 'lg';
  className?: string;
}

export const Section: React.FC<SectionProps> = ({
  children,
  spacing = 'md',
  className = ''
}) => {
  const spacingClass = {
    'sm': 'gap-8',
    'md': 'gap-12',
    'lg': 'gap-16'
  }[spacing];

  return (
    <section className={`section ${spacingClass} ${className}`}>
      {children}
    </section>
  );
};

interface SectionHeaderProps {
  title: string;
  subtitle?: string;
  badge?: string | number;
  action?: React.ReactNode;
}

export const SectionHeader: React.FC<SectionHeaderProps> = ({
  title,
  subtitle,
  badge,
  action
}) => {
  return (
    <div className="section-header">
      <div className="section-header-content">
        <h2 className="section-title">{title}</h2>
        {badge !== undefined && (
          <span className="section-badge">{badge}</span>
        )}
      </div>
      {subtitle && <p className="section-subtitle">{subtitle}</p>}
      {action && <div className="section-action">{action}</div>}
    </div>
  );
};
