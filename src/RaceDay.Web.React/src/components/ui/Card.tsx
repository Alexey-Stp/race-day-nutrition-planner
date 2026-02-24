import React from 'react';

interface CardProps {
  children: React.ReactNode;
  className?: string;
  padding?: 'none' | 'sm' | 'md' | 'lg';
  elevation?: 'none' | 'sm' | 'md';
}

export const Card: React.FC<CardProps> = ({ 
  children, 
  className = '', 
  padding = 'md',
  elevation = 'none'
}) => {
  const paddingClass = {
    'none': 'p-0',
    'sm': 'p-12',
    'md': 'p-16',
    'lg': 'p-24'
  }[padding];

  const elevationClass = {
    'none': '',
    'sm': 'shadow-sm',
    'md': 'shadow-md'
  }[elevation];

  return (
    <div className={`card ${paddingClass} ${elevationClass} ${className}`}>
      {children}
    </div>
  );
};

interface CardHeaderProps {
  title: string;
  subtitle?: string;
  action?: React.ReactNode;
  className?: string;
}

export const CardHeader: React.FC<CardHeaderProps> = ({
  title,
  subtitle,
  action,
  className = ''
}) => {
  return (
    <div className={`card-header ${className}`}>
      <div className="card-header-content">
        <h2 className="card-title">{title}</h2>
        {subtitle && <p className="card-subtitle">{subtitle}</p>}
      </div>
      {action && <div className="card-header-action">{action}</div>}
    </div>
  );
};

interface CardSectionProps {
  children: React.ReactNode;
  className?: string;
  spacing?: 'sm' | 'md' | 'lg';
}

export const CardSection: React.FC<CardSectionProps> = ({
  children,
  className = '',
  spacing = 'md'
}) => {
  const spacingClass = {
    'sm': 'gap-8',
    'md': 'gap-12',
    'lg': 'gap-16'
  }[spacing];

  return (
    <div className={`card-section ${spacingClass} ${className}`}>
      {children}
    </div>
  );
};
