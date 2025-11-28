export const Spinner = ({ className = '' }: { className?: string }) => {
  return (
    <div className={`inline-block animate-spin rounded-full h-6 w-6 border-b-2 border-blue-600 ${className}`} />
  );
};

