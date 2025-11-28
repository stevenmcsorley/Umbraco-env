import { useState, useEffect } from 'react';
import { AvailabilityService } from '../services/api/AvailabilityService';
import type { AvailabilityRequest, AvailabilityResponse } from '../types/domain.types';

export const useAvailability = (request: AvailabilityRequest | null) => {
  const [data, setData] = useState<AvailabilityResponse | null>(null);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    if (!request) {
      setData(null);
      return;
    }

    setLoading(true);
    setError(null);

    AvailabilityService.getRange(request)
      .then(setData)
      .catch((err) => setError(err.message))
      .finally(() => setLoading(false));
  }, [request?.productId, request?.from, request?.to]);

  return { data, loading, error };
};

