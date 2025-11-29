export interface PageContainerProps {
  className?: string;
  children: React.ReactNode;
}

export const PageContainer = ({ className = '', children }: PageContainerProps) => {
  return (
    <div style={{
      width: '100%',
      maxWidth: '100%',
      padding: '32px 24px',
      boxSizing: 'border-box'
    }} className={className}>
      {children}
    </div>
  );
};

