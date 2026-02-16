// Optimized QueryClient configuration for enterprise scalability
import { QueryClient, QueryClientConfig } from '@tanstack/react-query';

// Cache time constants (in milliseconds)
export const CACHE_TIMES = {
  // Static data that rarely changes (lookups, roles, skills)
  STATIC: 1000 * 60 * 30, // 30 minutes
  
  // Semi-static data (user preferences, settings)
  SEMI_STATIC: 1000 * 60 * 10, // 10 minutes
  
  // Dynamic data (contacts, offers, sales)
  DYNAMIC: 1000 * 60 * 2, // 2 minutes
  
  // Real-time data (notifications, dispatches)
  REALTIME: 1000 * 30, // 30 seconds
  
  // Never cache (form submissions, mutations)
  NONE: 0,
} as const;

// Stale time constants - when to show cached data while refetching
export const STALE_TIMES = {
  STATIC: 1000 * 60 * 15, // 15 minutes before considered stale
  SEMI_STATIC: 1000 * 60 * 5, // 5 minutes
  DYNAMIC: 1000 * 60, // 1 minute
  REALTIME: 1000 * 10, // 10 seconds
  IMMEDIATE: 0, // Always refetch
} as const;

// Retry configuration
const retryConfig = {
  maxRetries: 3,
  retryDelay: (attemptIndex: number) => Math.min(1000 * 2 ** attemptIndex, 30000),
};

// Create optimized QueryClient configuration
const queryClientConfig: QueryClientConfig = {
  defaultOptions: {
    queries: {
      // Default stale time - data is fresh for 1 minute
      staleTime: STALE_TIMES.DYNAMIC,
      
      // Default cache time - keep in cache for 5 minutes after becoming inactive
      gcTime: 1000 * 60 * 5,
      
      // Retry failed requests up to 3 times with exponential backoff
      retry: (failureCount, error: any) => {
        // Don't retry on 4xx errors (client errors)
        if (error?.status >= 400 && error?.status < 500) {
          return false;
        }
        return failureCount < retryConfig.maxRetries;
      },
      retryDelay: retryConfig.retryDelay,
      
      // Refetch behavior
      refetchOnWindowFocus: 'always', // Refetch when user returns to tab
      refetchOnReconnect: true, // Refetch when network reconnects
      refetchOnMount: true, // Refetch when component mounts if data is stale
      
      // Network mode
      networkMode: 'online', // Only run queries when online
      
      // Structural sharing for optimal re-renders
      structuralSharing: true,
    },
    mutations: {
      // Retry mutations once on failure
      retry: 1,
      retryDelay: 1000,
      
      // Network mode
      networkMode: 'online',
    },
  },
};

// Create the singleton QueryClient instance
export const queryClient = new QueryClient(queryClientConfig);

// Query key factories for consistent cache keys
export const queryKeys = {
  // Contacts
  contacts: {
    all: ['contacts'] as const,
    list: (params?: object) => ['contacts', 'list', params] as const,
    detail: (id: number) => ['contacts', 'detail', id] as const,
  },
  
  // Offers
  offers: {
    all: ['offers'] as const,
    list: (params?: object) => ['offers', 'list', params] as const,
    detail: (id: number) => ['offers', 'detail', id] as const,
  },
  
  // Sales
  sales: {
    all: ['sales'] as const,
    list: (params?: object) => ['sales', 'list', params] as const,
    detail: (id: number) => ['sales', 'detail', id] as const,
  },
  
  // Service Orders
  serviceOrders: {
    all: ['serviceOrders'] as const,
    list: (params?: object) => ['serviceOrders', 'list', params] as const,
    detail: (id: number) => ['serviceOrders', 'detail', id] as const,
  },
  
  // Dispatches
  dispatches: {
    all: ['dispatches'] as const,
    list: (params?: object) => ['dispatches', 'list', params] as const,
    detail: (id: number) => ['dispatches', 'detail', id] as const,
    calendar: (range: object) => ['dispatches', 'calendar', range] as const,
  },
  
  // Users & Auth
  users: {
    all: ['users'] as const,
    list: (params?: object) => ['users', 'list', params] as const,
    detail: (id: number) => ['users', 'detail', id] as const,
    current: ['users', 'current'] as const,
  },
  
  // Lookups (static data - long cache)
  lookups: {
    all: ['lookups'] as const,
    type: (type: string) => ['lookups', type] as const,
    currencies: ['lookups', 'currencies'] as const,
    priorities: ['lookups', 'priorities'] as const,
    taskStatuses: ['lookups', 'taskStatuses'] as const,
    eventTypes: ['lookups', 'eventTypes'] as const,
  },
  
  // Articles
  articles: {
    all: ['articles'] as const,
    list: (params?: object) => ['articles', 'list', params] as const,
    detail: (id: number) => ['articles', 'detail', id] as const,
  },
  
  // Notifications
  notifications: {
    all: ['notifications'] as const,
    unread: ['notifications', 'unread'] as const,
    list: (params?: object) => ['notifications', 'list', params] as const,
  },
  
  // AI Chat
  aiChat: {
    all: ['aiChat'] as const,
    conversations: ['aiChat', 'conversations'] as const,
    messages: (conversationId: string) => ['aiChat', 'messages', conversationId] as const,
  },
} as const;

// Type for entities with 'all' key
type EntityWithAll = {
  [K in keyof typeof queryKeys]: typeof queryKeys[K] extends { all: readonly string[] } ? K : never
}[keyof typeof queryKeys];

// Helper to invalidate related queries efficiently
export const invalidateRelatedQueries = async (
  client: QueryClient,
  entity: EntityWithAll
) => {
  const entityQueries = queryKeys[entity];
  if ('all' in entityQueries) {
    await client.invalidateQueries({ queryKey: entityQueries.all });
  }
};

// Prefetch helper for navigation
export const prefetchQuery = async <T>(
  queryKey: readonly unknown[],
  queryFn: () => Promise<T>,
  staleTime: number = STALE_TIMES.DYNAMIC
) => {
  return queryClient.prefetchQuery({
    queryKey,
    queryFn,
    staleTime,
  });
};

export default queryClient;
