export interface PageSectionProps {
  className?: string;
  children: React.ReactNode;
}

export const PageSection = ({ className = '', children }: PageSectionProps) => {
  return (
    <section className={`mb-8 ${className}`}>
      {children}
    </section>
  );
};

