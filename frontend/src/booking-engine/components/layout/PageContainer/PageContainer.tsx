export interface PageContainerProps {
  className?: string;
  children: React.ReactNode;
}

export const PageContainer = ({ className = '', children }: PageContainerProps) => {
  return (
    <div className={`max-w-4xl mx-auto px-4 py-8 ${className}`}>
      {children}
    </div>
  );
};

