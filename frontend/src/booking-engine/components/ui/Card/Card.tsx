import type { CardProps } from './Card.types';

export const Card = ({ className = '', children }: CardProps) => {
  return (
    <div className={`border border-gray-200 rounded-lg p-4 shadow-md ${className}`}>
      {children}
    </div>
  );
};

