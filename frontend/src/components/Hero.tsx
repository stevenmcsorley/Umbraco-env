import type { ReactNode } from 'react';

export interface HeroProps {
  title: string;
  subtitle?: string;
  imageUrl?: string;
  className?: string;
  children?: ReactNode;
}

export const Hero = ({ title, subtitle, imageUrl, className = '', children }: HeroProps) => {
  return (
    <div className={`relative h-96 flex items-center justify-center ${className}`}>
      {imageUrl && (
        <div
          className="absolute inset-0 bg-cover bg-center"
          style={{ backgroundImage: `url(${imageUrl})` }}
        >
          <div className="absolute inset-0 bg-black bg-opacity-40" />
        </div>
      )}
      <div className="relative z-10 text-center text-white px-4">
        <h1 className="text-4xl md:text-6xl font-bold mb-4">{title}</h1>
        {subtitle && <p className="text-xl md:text-2xl">{subtitle}</p>}
        {children}
      </div>
    </div>
  );
};

