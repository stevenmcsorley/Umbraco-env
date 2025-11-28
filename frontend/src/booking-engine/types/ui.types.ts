export interface ButtonProps {
  onClick?: () => void;
  disabled?: boolean;
  variant?: 'primary' | 'secondary' | 'outline';
  className?: string;
  children: React.ReactNode;
  type?: 'button' | 'submit' | 'reset';
  'data-testid'?: string;
}

export interface InputProps {
  type?: string;
  value: string;
  onChange: (e: React.ChangeEvent<HTMLInputElement>) => void;
  placeholder?: string;
  className?: string;
  required?: boolean;
  'data-testid'?: string;
}

export interface CardProps {
  className?: string;
  children: React.ReactNode;
}

export interface SelectProps {
  value: string;
  onChange: (e: React.ChangeEvent<HTMLSelectElement>) => void;
  options: Array<{ value: string; label: string }>;
  className?: string;
  'data-testid'?: string;
}

