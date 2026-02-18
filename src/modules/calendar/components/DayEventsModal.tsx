import React from "react";
import { useTranslation } from "react-i18next";
import { Dialog, DialogContent, DialogHeader, DialogTitle, DialogDescription } from "@/components/ui/dialog";
import { Button } from "@/components/ui/button";
import { Badge } from "@/components/ui/badge";
import { Separator } from "@/components/ui/separator";
import { CalendarEvent } from "../types";
import dayjs from "dayjs";
import { CalendarDays, Clock, MapPin } from "lucide-react";

type DayEventsModalProps = {
  open: boolean;
  date: Date | null;
  events: CalendarEvent[];
  onOpenChange: (open: boolean) => void;
  onAddEvent: (date: Date) => void;
  onEventClick: (event: CalendarEvent) => void;
  eventTypesLoaded?: boolean;
};

export function DayEventsModal({ open, date, events, onOpenChange, onAddEvent, onEventClick, eventTypesLoaded = true }: DayEventsModalProps) {
  const { t } = useTranslation('calendar');
  const sorted = React.useMemo(() => {
    return [...events].sort((a, b) => dayjs(a.start).valueOf() - dayjs(b.start).valueOf());
  }, [events]);

  const title = date ? dayjs(date).format("dddd, MMMM D, YYYY") : "";

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent className="w-[95vw] max-w-[720px] sm:max-w-xl p-0 overflow-hidden sm:rounded-2xl">
  <DialogHeader className="p-5 pb-4 border-b bg-gradient-to-r from-primary/5 to-primary/10 pr-14">
          <div className="flex flex-col gap-3 sm:flex-row sm:items-center sm:justify-between">
            <div className="flex items-center gap-3 min-w-0">
              <div className="p-2 rounded-lg bg-primary/10">
                <CalendarDays className="h-5 w-5 text-primary" />
              </div>
              <div className="flex-1 min-w-0">
                <DialogTitle className="truncate text-base sm:text-lg">{title}</DialogTitle>
                <DialogDescription className="mt-1 text-xs sm:text-sm">
                  {sorted.length > 0 ? `${sorted.length} event${sorted.length !== 1 ? 's' : ''} scheduled` : 'No events scheduled'}
                </DialogDescription>
              </div>
            </div>
            {date && sorted.length > 0 && (
              <div className="sm:mr-6">
                <Button 
                  variant="link" 
                  onClick={() => onAddEvent(date)} 
                  className="px-0 text-primary hover:underline"
                  disabled={!eventTypesLoaded}
                >
                  {eventTypesLoaded ? t('add_event') : t('loading')}
                </Button>
              </div>
            )}
          </div>
        </DialogHeader>

        <div className="p-4">
          {sorted.length === 0 ? (
            <div className="py-12 text-center text-muted-foreground">
              <p className="text-sm">No events scheduled</p>
              {date && (
                <div className="mt-4">
                  <button 
                    onClick={() => onAddEvent(date)} 
                    className="inline-flex items-center gap-1 text-primary hover:underline font-medium disabled:opacity-50 disabled:cursor-not-allowed"
                    disabled={!eventTypesLoaded}
                  >
                    <span>{eventTypesLoaded ? '+ Add event' : 'Loading event types...'}</span>
                  </button>
                </div>
              )}
            </div>
          ) : (
            <div className="space-y-3 max-h-[65vh] sm:max-h-[70vh] overflow-y-auto">
              {sorted.map((ev, idx) => {
                const isAllDay = dayjs(ev.end).diff(dayjs(ev.start), 'day') >= 1;
                return (
                  <div key={ev.id}>
                    <button onClick={() => onEventClick(ev)} className="w-full text-left group">
                      <div className="flex items-start gap-3 p-3 rounded-xl border border-border/60 hover:border-primary/40 hover:bg-primary/5 transition-colors">
                        <div className="p-2 rounded-md bg-primary text-white">
                          <Clock className="h-4 w-4" />
                        </div>
                        <div className="flex-1 min-w-0">
                          <div className="flex items-center justify-between gap-2">
                            <p className="font-medium text-foreground truncate group-hover:text-primary transition-colors">{ev.title}</p>
                            <Badge variant="secondary" className="shrink-0">{isAllDay ? 'All day' : `${dayjs(ev.start).format('HH:mm')} - ${dayjs(ev.end).format('HH:mm')}`}</Badge>
                          </div>
                          {ev.location && (
                            <div className="flex items-center gap-2 mt-1 text-xs text-muted-foreground truncate">
                              <MapPin className="h-3 w-3" />
                              <span className="truncate">{ev.location}</span>
                            </div>
                          )}
                        </div>
                      </div>
                    </button>
                    {idx < sorted.length - 1 && <Separator className="my-2" />}
                  </div>
                );
              })}
            </div>
          )}
        </div>
      </DialogContent>
    </Dialog>
  );
}
