import React from "react";
import { useEffect, useMemo, useState } from "react";
import { Calendar as BigCalendar, Views, View, dayjsLocalizer } from "react-big-calendar";
import dayjs from "dayjs";
import "dayjs/locale/fr";
import { add, startOfToday } from "date-fns";
import { formatStatValue } from "@/lib/formatters";
import { useTranslation } from "react-i18next";
import { Button } from "@/components/ui/button";
import { Card, CardContent } from "@/components/ui/card";
import { Separator } from "@/components/ui/separator";
import { YearGrid } from "./YearGrid";
import { EventDialog } from "./EventDialog";
import { EventViewDialog } from "./EventViewDialog";
import { EventTypeManager, EventType } from "./EventTypeManager";
import type { CalendarEvent } from "../types";
import { useLookups } from "@/shared/contexts/LookupsContext";
import "react-big-calendar/lib/css/react-big-calendar.css";
import "../styles.css";
import { cn } from "@/lib/utils";
import { 
  ChevronLeft, 
  ChevronRight, 
  Calendar as CalIcon, 
  Plus, 
  Users, 
  Clock, 
  Calendar as CalendarIcon,
  Target,
  List as ListIcon
} from "lucide-react";
import { Popover, PopoverContent, PopoverTrigger } from "@/components/ui/popover";
import { useLocation, useNavigate } from 'react-router-dom';
import { Calendar as DatePicker } from "@/components/ui/calendar";

import { useCalendar } from "../hooks/useCalendar";
import { DayEventsModal } from "./DayEventsModal";
import { EventListView } from "./EventListView";
import { Tabs, TabsList, TabsTrigger, TabsContent } from "@/components/ui/tabs";
import { SearchAndFilterBar, type FilterGroup } from "@/shared/components/SearchAndFilterBar";

// Localizer must be created lazily (not at module top-level) to avoid
// Temporal Dead Zone errors when Vite code-splits this chunk.
let _localizer: ReturnType<typeof dayjsLocalizer> | null = null;
function getLocalizer() {
  if (!_localizer) _localizer = dayjsLocalizer(dayjs);
  return _localizer;
}

export function CalendarPage() {
  const localizer = getLocalizer();
  const { t, i18n } = useTranslation();
  const { eventTypes: lookupEventTypes, updateEventTypes } = useLookups();
  const { date, setDate, view, setView, events, setEvents, currentPeriodEvents, onNavigate, create, update, remove } = useCalendar();

  // Convert lookup event types to calendar event types
  const eventTypes: EventType[] = lookupEventTypes.map(lookup => ({
    id: lookup.id,
    name: lookup.name,
    color: lookup.color || '#6b7280',
    isDefault: lookup.isDefault || false
  }));
  const [dialogOpen, setDialogOpen] = useState(false);
  const [viewDialogOpen, setViewDialogOpen] = useState(false);
  const [typeManagerOpen, setTypeManagerOpen] = useState(false);
  const [selectedEvent, setSelectedEvent] = useState<CalendarEvent | null>(null);
  const [slotSelection, setSlotSelection] = useState<{
    start?: Date;
    end?: Date;
  }>({});
  const [dayModalOpen, setDayModalOpen] = useState(false);
  const [dayModalDate, setDayModalDate] = useState<Date | null>(null);
  const [mode, setMode] = useState<'calendar' | 'list'>('calendar');
  const [searchTerm, setSearchTerm] = useState('');
  const [activeFilters, setActiveFilters] = useState<{ type?: string }>({ type: 'all' });

  const location = useLocation();
  const navigate = useNavigate();

  useEffect(() => {
    document.title = `${t("calendar") || "Calendar"} — FlowSolution`;
  }, [t]);

  useEffect(() => {
    dayjs.locale(i18n.language.startsWith("fr") ? "fr" : "en");
  }, [i18n.language]);

  // currentPeriodEvents comes from hook

  const statistics = useMemo(() => {
    const periodEvents = currentPeriodEvents;
    
    return [
      {
        label: "Total Events",
        value: periodEvents.length,
        icon: CalendarIcon,
        color: "chart-1"
      },
      {
        label: "Appointments",
        value: periodEvents.filter(e => e.type === 'appointment').length,
        icon: Clock,
        color: "chart-2"
      },
      {
        label: "Client Meetings",
        value: periodEvents.filter(e => e.type === 'customer_meeting').length,
        icon: Users,
        color: "chart-3"
      },
      {
        label: "Project Deadlines",
        value: periodEvents.filter(e => e.type === 'project_due').length,
        icon: Target,
        color: "chart-4"
      }
    ];
  }, [currentPeriodEvents]);

  // onNavigate is provided by hook

  const views = useMemo(() => ({ month: true }), []);

  const handleCreate = async (data: {
    title: string;
    start: Date;
    end: Date;
    type: string;
    description?: string;
    location?: string;
  }) => {
    // Validate that the event type exists in the lookup table
    const validEventType = eventTypes.find(et => et.id === data.type);
    if (!validEventType) {
      console.error('Invalid event type selected:', data.type);
      return;
    }
    
    try {
      await create(data);
      setDialogOpen(false);
      setSelectedEvent(null);
    } catch (error) {
      console.error('Failed to create event:', error);
    }
  };

  const handleUpdate = async (eventId: string, data: Partial<CalendarEvent>) => {
    // Validate event type if it's being updated
    if (data.type) {
      const validEventType = eventTypes.find(et => et.id === data.type);
      if (!validEventType) {
        console.error('Invalid event type selected:', data.type);
        return;
      }
    }
    
    try {
      await update(eventId, data);
      setDialogOpen(false);
      setViewDialogOpen(false);
      setSelectedEvent(null);
    } catch (error) {
      console.error('Failed to update event:', error);
    }
  };

  const handleDelete = (eventId: string) => {
    remove(eventId);
    setViewDialogOpen(false);
    setDialogOpen(false);
  };

  const handleEventView = (event: CalendarEvent) => {
    setSelectedEvent(event);
    setViewDialogOpen(true);
  };

  // Open event from query param ?eventId=...
  useEffect(() => {
    try {
      const params = new URLSearchParams(location.search);
      const eid = params.get('eventId');
      if (eid) {
        // find event in current events
        const match = events.find(ev => String(ev.id) === String(eid));
        if (match) {
          setSelectedEvent(match);
          setViewDialogOpen(true);
          // remove eventId from url to avoid reopening on navigation
          params.delete('eventId');
          const newSearch = params.toString();
          const newPath = `${location.pathname}${newSearch ? `?${newSearch}` : ''}`;
          navigate(newPath, { replace: true });
        }
      }
    } catch {
      // ignore malformed URLSearchParams
    }
  // run when location.search or events change
  }, [location.search, location.pathname, events, navigate]);

  const handleEventEdit = () => {
    setViewDialogOpen(false);
    setDialogOpen(true);
  };

  const getEventColor = (event: CalendarEvent) => {
    const eventType = eventTypes.find(type => type.id === event.type);
    return eventType?.color || '#6b7280';
  };

  // Build filter groups like other modules (Type selector)
  const filterGroups: FilterGroup[] = useMemo(() => {
    const counts: Record<string, number> = {};
    for (const ev of events) {
      const key = String(ev.type);
      counts[key] = (counts[key] || 0) + 1;
    }
    return [
      {
        key: 'type',
        label: 'Type',
        options: eventTypes.map(et => ({ value: String(et.id), label: et.name, count: counts[String(et.id)] || 0 }))
      }
    ];
  }, [events, eventTypes]);

  const onFilterChange = (key: string, value: string | string[]) => {
    if (key === 'type') setActiveFilters(prev => ({ ...prev, type: value as string }));
  };
  const onClearFilters = () => setActiveFilters({ type: 'all' });

  // Apply search + filters for both views
  const filteredEvents = useMemo(() => {
    const term = searchTerm.trim().toLowerCase();
    return events.filter(ev => {
      const matchesSearch = !term || ev.title.toLowerCase().includes(term) || (ev.location || '').toLowerCase().includes(term);
      const matchesType = !activeFilters.type || activeFilters.type === 'all' || String(ev.type) === String(activeFilters.type);
      return matchesSearch && matchesType;
    });
  }, [events, searchTerm, activeFilters]);
  const Header = () => (
    <div className="flex items-center justify-between p-4 border-b border-border bg-card/50 backdrop-blur">
      <div className="flex items-center gap-3">
        <div className="p-2 rounded-lg bg-primary/10">
          <CalendarIcon className="h-6 w-6 text-primary" />
        </div>
        <div>
          <h1 className="text-xl font-semibold text-foreground">{t('calendar.title', 'Calendar & Events')}</h1>
          <p className="text-[11px] text-muted-foreground">{t('calendar.subtitle', 'Manage appointments, meetings, and project deadlines')}</p>
        </div>
      </div>
      <div className="hidden sm:flex items-center gap-2">
        {/* keep action buttons here to preserve behavior */}
        <Button variant="outline" size="icon" onClick={() => onNavigate("TODAY")} className="text-xs px-2">
          Today
        </Button>
        <Button variant="outline" size="icon" onClick={() => onNavigate("PREV")} aria-label="Previous">
          <ChevronLeft className="h-4 w-4" />
        </Button>
        <Button variant="outline" size="icon" onClick={() => onNavigate("NEXT")} aria-label="Next">
          <ChevronRight className="h-4 w-4" />
        </Button>
      </div>
    </div>
  );


  return (
    <div className="flex flex-col">
      {/* Header */}
      <Header />

      {/* Statistics Cards */}
      <div className="p-3 sm:p-4 border-b border-border">
        <div className="grid grid-cols-2 lg:grid-cols-4 gap-3 sm:gap-4">
        {statistics.map((stat, index) => (
          <Card key={index} className="shadow-card hover-lift gradient-card group cursor-pointer transition-all hover:shadow-lg border-0">
            <CardContent className="p-3">
              <div className="flex items-center justify-between">
                <div className="flex items-center gap-3 flex-1 min-w-0">
                  <div className={`p-2 rounded-lg transition-all flex-shrink-0 bg-${stat.color}/10 group-hover:bg-${stat.color}/20`}>
                    <stat.icon className={`h-4 w-4 transition-all text-${stat.color}`} />
                  </div>
                  <div className="flex-1 min-w-0">
                    <p className="text-xs text-muted-foreground font-medium truncate">{stat.label}</p>
                  </div>
                </div>
                <div className="text-right">
                  <p className="text-sm font-bold text-foreground">{formatStatValue(stat.value)}</p>
                </div>
              </div>
            </CardContent>
          </Card>
        ))}
        </div>
      </div>

      {/* Calendar Controls and View */}
  <Card className="border-0 shadow-medium">
        <div className="flex flex-col gap-3 md:flex-row md:items-center md:justify-between p-4">
          <div className="flex items-center gap-2">
            <Button variant="outline" size="icon" onClick={() => onNavigate("TODAY")} className="text-xs px-2">
              Today
            </Button>
            <Button variant="outline" size="icon" onClick={() => onNavigate("PREV")} aria-label="Previous">
              <ChevronLeft className="h-4 w-4" />
            </Button>
            <Button variant="outline" size="icon" onClick={() => onNavigate("NEXT")} aria-label="Next">
              <ChevronRight className="h-4 w-4" />
            </Button>
            
            <Popover>
              <PopoverTrigger asChild>
                <Button variant="outline" className="ml-2">
                  <CalIcon className="h-4 w-4 mr-2" />
                  {dayjs(date).format("MMM D, YYYY")}
                </Button>
              </PopoverTrigger>
              <PopoverContent className="w-auto p-2" align="start">
                <DatePicker 
                  mode="single" 
                  selected={date} 
                  onSelect={d => d && setDate(d)} 
                  className={cn("p-3 pointer-events-auto")} 
                />
              </PopoverContent>
            </Popover>
          </div>

          <div className="flex flex-wrap items-center gap-2">
            <Button 
              onClick={() => {
                if (eventTypes.length === 0) {
                  console.warn('Event types not loaded yet');
                  return;
                }
                setSelectedEvent(null);
                setSlotSelection({
                  start: date,
                  end: add(date, { hours: 1 })
                });
                setDialogOpen(true);
              }}
              className="gradient-primary text-white shadow-medium hover-lift"
              disabled={eventTypes.length === 0}
            >
              <Plus className="h-4 w-4 mr-2" />
              {eventTypes.length === 0 ? t('loading') : t('new_event')}
            </Button>
            {/* View switcher buttons (match other pages) */}
            <div className="flex items-center gap-2 ml-2">
              <Button 
                variant={mode === 'calendar' ? 'default' : 'outline'}
                size="sm"
                onClick={() => setMode('calendar')}
                className={`flex-1 sm:flex-none ${mode === 'calendar' ? 'bg-primary text-white hover:bg-primary/90' : ''}`}
                aria-pressed={mode === 'calendar'}
              >
                <CalendarIcon className={`h-4 w-4 ${mode === 'calendar' ? 'text-white' : ''}`} />
              </Button>
              <Button 
                variant={mode === 'list' ? 'default' : 'outline'}
                size="sm"
                onClick={() => setMode('list')}
                className={`flex-1 sm:flex-none ${mode === 'list' ? 'bg-primary text-white hover:bg-primary/90' : ''}`}
                aria-pressed={mode === 'list'}
              >
                <ListIcon className={`h-4 w-4 ${mode === 'list' ? 'text-white' : ''}`} />
              </Button>
            </div>
          </div>
        </div>
        <Separator />
          <CardContent className={cn("p-0")}>
          {/* Shared search and filters under the actions */}
          <div className="p-3 sm:p-4 pb-3">
            <SearchAndFilterBar
              searchTerm={searchTerm}
              onSearchChange={setSearchTerm}
              placeholder="Search events..."
              filterGroups={filterGroups}
              activeFilters={activeFilters as any}
              onFilterChange={onFilterChange}
              onClearFilters={onClearFilters}
              fullWidth
            />
          </div>
          <Tabs value={mode} className="w-full">
            <TabsContent value="calendar" className="m-0">
          {view === "year" ? (
            <YearGrid 
              date={date} 
              onMonthClick={m => {
                const d = add(startOfToday(), { months: m });
                setDate(d);
                setView(Views.MONTH);
              }} 
            />
          ) : (
            <div className="h-[70vh] relative">
              <BigCalendar 
                localizer={localizer} 
                date={date} 
                onNavigate={d => setDate(d)} 
                view={view} 
                onView={v => setView(v)} 
                events={filteredEvents.map(event => ({
                  ...event,
                  style: {
                    backgroundColor: getEventColor(event),
                    borderColor: getEventColor(event),
                  }
                }))}
                startAccessor="start" 
                endAccessor="end" 
                style={{ height: "100%" }} 
                views={views} 
                popup={false}
                selectable
                tooltipAccessor={(event: CalendarEvent) => `${event.title}${event.location ? ` - ${event.location}` : ''}`}
                onSelectSlot={(slotInfo: any) => {
                  // Open Day Events Modal instead of immediate add
                  setDayModalDate(slotInfo.start);
                  setDayModalOpen(true);
                }}
                onSelectEvent={(event: CalendarEvent) => {
                  handleEventView(event);
                }}
                onShowMore={(_events: CalendarEvent[], date: Date) => {
                  setDayModalDate(date);
                  setDayModalOpen(true);
                }}
                eventPropGetter={(event: CalendarEvent) => ({
                  style: {
                    backgroundColor: getEventColor(event),
                    borderColor: getEventColor(event),
                    color: 'white',
                    borderRadius: '4px',
                    fontSize: '10px',
                    padding: '0 4px',
                    cursor: 'pointer'
                  }
                })}
                showMultiDayTimes={false}
                dayLayoutAlgorithm="no-overlap"
                max={2}
                components={{
                  // Render a compact single event entry and let BigCalendar handle the
                  // native '+ more' logic triggered by limited row height.
                  event: ({ event }: any) => (
                    <div className="w-full h-full text-white text-[10px] px-1 cursor-pointer hover:opacity-90 transition-opacity leading-[18px]">
                      <span className="truncate font-medium text-[10px]">{event.title}</span>
                    </div>
                  )
                }}
              />

              {/* Mobile FAB */}
              <Button 
                className="md:hidden fixed bottom-6 right-6 rounded-full h-12 w-12 shadow-strong" 
                onClick={() => {
                  setSelectedEvent(null);
                  setSlotSelection({
                    start: date,
                    end: add(date, { hours: 1 })
                  });
                  setDialogOpen(true);
                }} 
                aria-label="Add Event"
              >
                <Plus className="h-5 w-5" />
              </Button>
            </div>
          )}
            </TabsContent>
            <TabsContent value="list" className="m-0">
              <EventListView
                startDate={startOfToday()}
                days={30}
                events={filteredEvents}
                onEventClick={(ev) => {
                  setSelectedEvent(ev);
                  setViewDialogOpen(true);
                }}
              />
            </TabsContent>
          </Tabs>
        </CardContent>
      </Card>

      <EventViewDialog
        open={viewDialogOpen}
        onOpenChange={setViewDialogOpen}
        event={selectedEvent}
        onEdit={handleEventEdit}
        onDelete={handleDelete}
      />

      <EventDialog 
        open={dialogOpen && eventTypes.length > 0} 
        onOpenChange={setDialogOpen} 
        initialStart={slotSelection.start} 
        initialEnd={slotSelection.end}
        event={selectedEvent}
        eventTypes={eventTypes}
        onSave={handleCreate}
        onUpdate={handleUpdate}
        onDelete={handleDelete}
        onManageTypes={() => setTypeManagerOpen(true)}
      />

      {/* Day Events Modal */}
      <DayEventsModal
        open={dayModalOpen}
        date={dayModalDate}
        events={useMemo(() => {
          if (!dayModalDate) return [];
          return events.filter(ev => dayjs(ev.start).isSame(dayModalDate, 'day'));
        }, [dayModalDate, events])}
        onOpenChange={setDayModalOpen}
        onAddEvent={(d) => {
          if (eventTypes.length === 0) {
            console.warn('Event types not loaded yet');
            return;
          }
          setSelectedEvent(null);
          setSlotSelection({ start: d, end: add(d, { hours: 1 }) });
          setDayModalOpen(false);
          setDialogOpen(true);
        }}
        onEventClick={(ev) => {
          setSelectedEvent(ev);
          setDayModalOpen(false);
          setViewDialogOpen(true);
        }}
        eventTypesLoaded={eventTypes.length > 0}
      />

      <EventTypeManager
        open={typeManagerOpen}
        onOpenChange={setTypeManagerOpen}
        eventTypes={eventTypes}
        onEventTypesChange={(newTypes) => {
          const lookupTypes = newTypes.map(type => ({
            id: type.id,
            name: type.name,
            color: type.color,
            isDefault: type.isDefault,
            isActive: true
          }));
          updateEventTypes(lookupTypes);
        }}
      />
    </div>
  );
}
