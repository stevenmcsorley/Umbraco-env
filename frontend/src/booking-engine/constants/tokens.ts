export interface DesignTokens {
  colors: {
    brandPrimary: string;
    brandAccent: string;
    text: string;
    background: string;
  };
  spacing: {
    xs: string;
    sm: string;
    md: string;
    lg: string;
    xl: string;
  };
  radius: {
    sm: string;
    md: string;
    lg: string;
  };
  shadows: {
    card: string;
    modal: string;
  };
  typography: {
    fontFamily: string;
    sizes: {
      xs: string;
      sm: string;
      md: string;
      lg: string;
      xl: string;
      xxl: string;
    };
  };
}

export const defaultTokens: DesignTokens = {
  colors: {
    brandPrimary: '#2563eb',
    brandAccent: '#f59e0b',
    text: '#1f2937',
    background: '#ffffff'
  },
  spacing: {
    xs: '0.25rem',
    sm: '0.5rem',
    md: '1rem',
    lg: '1.5rem',
    xl: '2rem'
  },
  radius: {
    sm: '0.25rem',
    md: '0.5rem',
    lg: '0.75rem'
  },
  shadows: {
    card: '0 1px 3px 0 rgba(0, 0, 0, 0.1)',
    modal: '0 10px 15px -3px rgba(0, 0, 0, 0.1)'
  },
  typography: {
    fontFamily: '-apple-system, BlinkMacSystemFont, "Segoe UI", Roboto, sans-serif',
    sizes: {
      xs: '0.75rem',
      sm: '0.875rem',
      md: '1rem',
      lg: '1.125rem',
      xl: '1.25rem',
      xxl: '1.5rem'
    }
  }
};

