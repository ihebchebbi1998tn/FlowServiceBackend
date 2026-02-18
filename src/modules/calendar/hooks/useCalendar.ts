import { useEffect, useMemo, useState } from "react";
import { Views, View } from "react-big-calendar";
import { add, startOfToday, startOfWeek, endOfWeek, startOfMonth, endOfMonth } from "date-fns";
import type { CalendarEvent } from "../types";
import { CalendarService } from "../services/calendar.service";

export function useCalendar() {
  const [date, setDate] = useState<Date>(startOfToday());
  const [view, setView] = useState<View>(Views.MONTH);
  const [events, setEvents] = useState<CalendarEvent[]>([]);
  const [loading, setLoading] = useState(false);

  // Load events from API on mount
  useEffect(() => {
    const loadEvents = async () => {
      setLoading(true);
      try {
        const calendarEvents = await CalendarService.list();
        setEvents(calendarEvents);
      } catch (error) {
        console.error('Failed to load calendar events:', error);
      } finally {
        setLoading(false);
      }
    };
    loadEvents();
  }, []);

  const currentPeriodEvents = useMemo(() => {
    let start: Date, end: Date;
    switch (view) {
      case Views.WEEK:
        start = startOfWeek(date); end = endOfWeek(date); break;
      case Views.MONTH:
        start = startOfMonth(date); end = endOfMonth(date); break;
      default:
        start = startOfToday(); end = add(startOfToday(), { days: 7 });
    }
    return events.filter(event => event.start >= start && event.start <= end);
  }, [events, date, view]);

  function onNavigate(action: "TODAY" | "PREV" | "NEXT") {
    if (action === "TODAY") return setDate(new Date());
    if (action === "PREV") return setDate(add(date, { [view === Views.MONTH || view === "year" ? "months" : "days"]: -1 * (view === Views.WEEK ? 7 : 1) } as any));
    if (action === "NEXT") return setDate(add(date, { [view === Views.MONTH || view === "year" ? "months" : "days"]: view === Views.WEEK ? 7 : 1 } as any));
  }

  async function create(data: { title: string; start: Date; end: Date; type: string; description?: string; location?: string; }) {
    setLoading(true);
    try {
      const newEvent = await CalendarService.create({
        title: data.title,
        start: data.start,
        end: data.end,
        type: data.type,
        description: data.description,
        location: data.location,
        allDay: false,
        status: 'scheduled',
        priority: 'medium',
        isPrivate: false,
        createdBy: '00000000-0000-0000-0000-000000000000' // Default GUID
      });
      setEvents(prev => [...prev, newEvent]);
    } catch (error) {
      console.error('Failed to create event:', error);
      throw error;
    } finally {
      setLoading(false);
    }
  }

  async function update(eventId: string, data: Partial<CalendarEvent>) {
    setLoading(true);
    try {
      const updatedEvent = await CalendarService.update(eventId, data);
      setEvents(prev => prev.map(e => e.id === eventId ? updatedEvent : e));
    } catch (error) {
      console.error('Failed to update event:', error);
      throw error;
    } finally {
      setLoading(false);
    }
  }

  async function remove(eventId: string) {
    setLoading(true);
    try {
      await CalendarService.remove(eventId);
      setEvents(prev => prev.filter(e => e.id !== eventId));
    } catch (error) {
      console.error('Failed to delete event:', error);
      throw error;
    } finally {
      setLoading(false);
    }
  }

  return { date, setDate, view, setView, events, setEvents, currentPeriodEvents, onNavigate, create, update, remove, loading } as const;
}
