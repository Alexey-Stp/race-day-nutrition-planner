import type { ProductInfo, ActivityInfo, RaceNutritionPlan, AthleteProfile, RaceProfile, ProductEditor } from './types';

// Empty by default: requests go to same-origin `/api`, which the Vite dev/preview
// proxy and the nginx container proxy forward to the backend.
const API_BASE_URL: string = import.meta.env.VITE_API_URL ?? '';

/** RFC 7807 problem details as returned by the API for every error response. */
interface ProblemDetails {
  type?: string;
  title?: string;
  status?: number;
  detail?: string;
  errors?: Record<string, string[]>;
}

const isProblemDetails = (value: unknown): value is ProblemDetails =>
  typeof value === 'object' && value !== null && ('title' in value || 'detail' in value || 'errors' in value);

const problemToMessage = (problem: ProblemDetails): string => {
  const parts: string[] = [];
  const headline = problem.detail ?? problem.title;
  if (headline) parts.push(headline);
  if (problem.errors) {
    for (const messages of Object.values(problem.errors)) {
      parts.push(...messages);
    }
  }
  return parts.join(' ');
};

async function errorFromResponse(response: Response): Promise<Error> {
  const statusSuffix = response.statusText ? ' ' + response.statusText : '';
  const fallback = `Request failed (${response.status}${statusSuffix})`;
  let text = '';
  try {
    text = await response.text();
  } catch {
    return new Error(fallback);
  }
  if (!text) return new Error(fallback);

  try {
    const body: unknown = JSON.parse(text);
    if (typeof body === 'string') return new Error(body || fallback);
    if (isProblemDetails(body)) return new Error(problemToMessage(body) || fallback);
  } catch {
    // Not JSON: fall through to plain text.
  }
  return new Error(text);
}

async function request<T>(path: string, init?: RequestInit): Promise<T> {
  const response = await fetch(`${API_BASE_URL}${path}`, init);
  if (!response.ok) {
    throw await errorFromResponse(response);
  }
  return response.json() as Promise<T>;
}

export const api = {
  getProducts(signal?: AbortSignal): Promise<ProductInfo[]> {
    return request<ProductInfo[]>('/api/products', { signal });
  },

  getActivities(signal?: AbortSignal): Promise<ActivityInfo[]> {
    return request<ActivityInfo[]>('/api/activities', { signal });
  },

  generatePlan(
    athlete: AthleteProfile,
    race: RaceProfile,
    products: (ProductEditor & { productType: string; caffeineMg?: number })[],
    caffeineEnabled: boolean = false,
    signal?: AbortSignal,
  ): Promise<RaceNutritionPlan> {
    const body = {
      athleteWeightKg: athlete.weightKg,
      sportType: race.sportType,
      durationHours: race.durationHours,
      // Enums bind by name (JsonStringEnumConverter on the API; integers are rejected).
      temperature: race.temperature,
      intensity: race.intensity,
      caffeineEnabled,
      // Triathlon leg inputs (omitted when absent). DistancePreset binds by name;
      // legs pass through as hours.
      legs: race.legs ?? undefined,
      distancePreset: race.preset ?? undefined,
      products: products.map(p => ({
        name: p.name,
        productType: p.productType,
        carbsG: p.carbsG,
        sodiumMg: p.sodiumMg,
        volumeMl: p.volumeMl ?? 0,
        caffeineMg: p.caffeineMg ?? null
      }))
    };

    return request<RaceNutritionPlan>('/api/plan/generate', {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify(body),
      signal,
    });
  },
};
