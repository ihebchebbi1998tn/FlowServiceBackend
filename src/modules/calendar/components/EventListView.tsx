import React, { useMemo } from 'react';
import dayjs from 'dayjs';
import { CalendarEvent } from '../types';
import { Card } from '@/components/ui/card';
import { cn } from '@/lib/utils';

export interface EventListViewProps {
  startDate: Date;
  days?: number; // default 30
  events: CalendarEvent[];
  onEventClick?: (event: CalendarEvent) => void;
  className?: string;
}

export const EventListView: React.FC<EventListViewProps> = ({
  startDate,
  days = 30,
  events,
  onEventClick,
  className
}) => {
  const daysArray = useMemo(() => {
    return Array.from({ length: days }, (_, i) => dayjs(startDate).add(i, 'day'));
  }, [startDate, days]);

  const byDay = useMemo(() => {
    const map = new Map<string, CalendarEvent[]>();
    for (const d of daysArray) {
      map.set(d.format('YYYY-MM-DD'), []);
    }
    for (const ev of events) {
      const key = dayjs(ev.start).format('YYYY-MM-DD');
      if (map.has(key)) {
        map.get(key)!.push(ev);
      }
    }
    // sort each day's events by start time
    for (const [k, list] of map) {
      list.sort((a, b) => dayjs(a.start).valueOf() - dayjs(b.start).valueOf());
    }
    return map;
  }, [events, daysArray]);

  return (
    <div className={cn('space-y-3 p-3', className)}>
      {daysArray.map(d => {
        const key = d.format('YYYY-MM-DD');
        const list = byDay.get(key) || [];
        return (
          <Card key={key} className="border-0 shadow-card">
            <div className="px-4 py-2 flex items-center justify-between sticky top-0 bg-card/95 backdrop-blur supports-[backdrop-filter]:bg-card/60 rounded-t-md">
              <div className="font-semibold text-foreground">{d.format('dddd, MMM D')}</div>
              <div className="text-xs text-muted-foreground">{list.length} {list.length === 1 ? 'event' : 'events'}</div>
            </div>
            <div className="divide-y divide-border">
              {list.length === 0 ? (
                <div className="px-4 py-3 text-sm text-muted-foreground">No events</div>
              ) : (
                list.map(ev => (
                  <button
                    key={String(ev.id)}
                    onClick={() => onEventClick?.(ev)}
                    className="w-full text-left px-4 py-3 hover:bg-muted/40 transition-colors flex items-center gap-3"
                  >
                    <span
                      className="h-2.5 w-2.5 rounded-full flex-shrink-0"
                      style={{ backgroundColor: (ev as any).style?.backgroundColor || 'hsl(var(--primary))' }}
                    />
                    <div className="min-w-0 flex-1">
                      <div className="font-medium truncate text-foreground">{ev.title}</div>
                      <div className="text-xs text-muted-foreground truncate">
                        {dayjs(ev.start).format('HH:mm')} - {dayjs(ev.end).format('HH:mm')}
                        {ev.location ? ` • ${ev.location}` : ''}
                      </div>
                    </div>
                  </button>
                ))
              )}
            </div>
          </Card>
        );
      })}
    </div>
  );
};

export default EventListView;
