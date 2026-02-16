import axios from 'axios';
import { API_CONFIG } from '@/config/api.config';
import { getCurrentTenant } from '@/utils/tenant';

const API_BASE_URL = API_CONFIG.baseURL;

// Backend DTOs matching FlowServiceBackend/DTOs/CalendarDTOs.cs
export interface CalendarEventDto {
  id: string;
  title: string;
  description?: string;
  start: string; // ISO string from backend
  end: string; // ISO string from backend
  allDay: boolean;
  type: string;
  status: string;
  priority: string;
  category?: string;
  color?: string;
  location?: string;
  attendees?: string;
  relatedType?: string;
  relatedId?: string;
  contactId?: number;
  reminders?: string;
  recurring?: string;
  isPrivate: boolean;
  createdAt: string;
  updatedAt: string;
  createdBy: string;
  modifiedBy?: string;
  contactName?: string;
  typeName?: string;
  eventAttendees: EventAttendeeDto[];
  eventReminders: EventReminderDto[];
}

export interface CreateCalendarEventDto {
  title: string;
  description?: string;
  start: string; // ISO string
  end: string; // ISO string
  allDay?: boolean;
  type: string;
  status?: string;
  priority?: string;
  category?: string;
  color?: string;
  location?: string;
  attendees?: string;
  relatedType?: string;
  relatedId?: string;
  contactId?: number;
  reminders?: string;
  recurring?: string;
  isPrivate?: boolean;
  createdBy: string;
}

export interface UpdateCalendarEventDto {
  title?: string;
  description?: string;
  start?: string;
  end?: string;
  allDay?: boolean;
  type?: string;
  status?: string;
  priority?: string;
  category?: string;
  color?: string;
  location?: string;
  attendees?: string;
  relatedType?: string;
  relatedId?: string;
  contactId?: number;
  reminders?: string;
  recurring?: string;
  isPrivate?: boolean;
  modifiedBy?: string;
}

export interface EventTypeDto {
  id: string;
  name: string;
  description?: string;
  color: string;
  isDefault: boolean;
  isActive: boolean;
  createdAt: string;
}

export interface CreateEventTypeDto {
  id: string;
  name: string;
  description?: string;
  color: string;
  isDefault?: boolean;
  isActive?: boolean;
}

export interface EventAttendeeDto {
  id: string;
  eventId: string;
  userId?: string;
  email?: string;
  name?: string;
  status: string;
  response?: string;
  respondedAt?: string;
  createdAt: string;
}

export interface CreateEventAttendeeDto {
  eventId: string;
  userId?: string;
  email?: string;
  name?: string;
  status?: string;
  response?: string;
}

export interface EventReminderDto {
  id: string;
  eventId: string;
  type: string;
  minutesBefore: number;
  isActive: boolean;
  sentAt?: string;
  createdAt: string;
}

export interface CreateEventReminderDto {
  eventId: string;
  type: string;
  minutesBefore: number;
  isActive?: boolean;
}

// Create axios instance with auth token
const createAuthHeaders = () => {
  const token = localStorage.getItem('access_token');
  return token ? { Authorization: `Bearer ${token}` } : {};
};

const api = axios.create({
  baseURL: API_BASE_URL,
  headers: {
    'Content-Type': 'application/json',
  },
});

// Add auth header and tenant header to all requests
api.interceptors.request.use((config) => {
  const authHeaders = createAuthHeaders();
  if (authHeaders.Authorization) {
    config.headers.Authorization = authHeaders.Authorization;
  }
  const tenant = getCurrentTenant();
  if (tenant) {
    config.headers['X-Tenant'] = tenant;
  }
  return config;
});

// Calendar Events API
export class CalendarApi {
  static async getAllEvents(): Promise<CalendarEventDto[]> {
    const response = await api.get('/Calendar/events');
    return response.data;
  }

  static async getEventById(id: string): Promise<CalendarEventDto> {
    const response = await api.get(`/Calendar/events/${id}`);
    return response.data;
  }

  static async getEventsByDateRange(start: Date, end: Date): Promise<CalendarEventDto[]> {
    const params = {
      start: start.toISOString(),
      end: end.toISOString()
    };
    const response = await api.get('/Calendar/events/date-range', { params });
    return response.data;
  }

  static async getEventsByContact(contactId: number): Promise<CalendarEventDto[]> {
    const response = await api.get(`/Calendar/events/contact/${contactId}`);
    return response.data;
  }

  static async createEvent(createDto: CreateCalendarEventDto): Promise<CalendarEventDto> {
    const response = await api.post('/Calendar/events', createDto);
    return response.data;
  }

  static async updateEvent(id: string, updateDto: UpdateCalendarEventDto): Promise<CalendarEventDto> {
    const response = await api.put(`/Calendar/events/${id}`, updateDto);
    return response.data;
  }

  static async deleteEvent(id: string): Promise<void> {
    await api.delete(`/Calendar/events/${id}`);
  }

  // Event Types API
  static async getAllEventTypes(): Promise<EventTypeDto[]> {
    const response = await api.get('/Calendar/event-types');
    return response.data;
  }

  static async getEventTypeById(id: string): Promise<EventTypeDto> {
    const response = await api.get(`/Calendar/event-types/${id}`);
    return response.data;
  }

  static async createEventType(createDto: CreateEventTypeDto): Promise<EventTypeDto> {
    const response = await api.post('/Calendar/event-types', createDto);
    return response.data;
  }

  static async deleteEventType(id: string): Promise<void> {
    await api.delete(`/Calendar/event-types/${id}`);
  }

  // Event Attendees API
  static async getEventAttendees(eventId: string): Promise<EventAttendeeDto[]> {
    const response = await api.get(`/Calendar/events/${eventId}/attendees`);
    return response.data;
  }

  static async createEventAttendee(createDto: CreateEventAttendeeDto): Promise<EventAttendeeDto> {
    const response = await api.post('/Calendar/events/attendees', createDto);
    return response.data;
  }

  static async deleteEventAttendee(id: string): Promise<void> {
    await api.delete(`/Calendar/events/attendees/${id}`);
  }

  // Event Reminders API
  static async getEventReminders(eventId: string): Promise<EventReminderDto[]> {
    const response = await api.get(`/Calendar/events/${eventId}/reminders`);
    return response.data;
  }

  static async createEventReminder(createDto: CreateEventReminderDto): Promise<EventReminderDto> {
    const response = await api.post('/Calendar/events/reminders', createDto);
    return response.data;
  }

  static async deleteEventReminder(id: string): Promise<void> {
    await api.delete(`/Calendar/events/reminders/${id}`);
  }
}