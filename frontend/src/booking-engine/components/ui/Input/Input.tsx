import type { InputProps } from './Input.types';

export const Input = ({
  type = 'text',
  value,
  onChange,
  placeholder,
  className = '',
  required = false,
  'data-testid': testId
}: InputProps) => {
  return (
    <input
      type={type}
      value={value}
      onChange={onChange}
      placeholder={placeholder}
      required={required}
      className={`w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-blue-500 ${className}`}
      data-testid={testId}
    />
  );
};

