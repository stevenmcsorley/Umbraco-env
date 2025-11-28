export interface ButtonProps {
  onClick?: () => void;
  disabled?: boolean;
  variant?: 'primary' | 'secondary' | 'outline';
  className?: string;
  children: React.ReactNode;
  type?: 'button' | 'submit' | 'reset';
  'data-testid'?: string;
}

