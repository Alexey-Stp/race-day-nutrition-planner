import React from 'react';

interface ButtonProps extends React.ButtonHTMLAttributes<HTMLButtonElement> {
  variant?: 'primary' | 'secondary' | 'outline' | 'ghost';
  size?: 'sm' | 'md' | 'lg';
  fullWidth?: boolean;
  children: React.ReactNode;
}

export const Button: React.FC<ButtonProps> = ({
  variant = 'primary',
  size = 'md',
  fullWidth = false,
  children,
  className = '',
  ...props
}) => {
  const variantClass = {
    'primary': 'btn-primary',
    'secondary': 'btn-secondary',
    'outline': 'btn-outline',
    'ghost': 'btn-ghost'
  }[variant];

  const sizeClass = {
    'sm': 'btn-sm',
    'md': 'btn-md',
    'lg': 'btn-lg'
  }[size];

  const widthClass = fullWidth ? 'w-full' : '';

  return (
    <button
      className={`btn ${variantClass} ${sizeClass} ${widthClass} ${className}`}
      {...props}
    >
      {children}
    </button>
  );
};

interface IconButtonProps extends React.ButtonHTMLAttributes<HTMLButtonElement> {
  icon: React.ReactNode;
  label?: string;
}

export const IconButton: React.FC<IconButtonProps> = ({
  icon,
  label,
  className = '',
  ...props
}) => {
  return (
    <button
      className={`btn-icon ${className}`}
      aria-label={label}
      {...props}
    >
      {icon}
    </button>
  );
};
