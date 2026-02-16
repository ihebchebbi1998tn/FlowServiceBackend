import React, { useState } from "react";
import { Dialog, DialogContent, DialogHeader, DialogTitle, DialogFooter, DialogDescription } from "@/components/ui/dialog";
import { Button } from "@/components/ui/button";
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from "@/components/ui/select";
import { Label } from "@/components/ui/label";
import { Input } from "@/components/ui/input";
import { Textarea } from "@/components/ui/textarea";
import { Switch } from "@/components/ui/switch";
import { Calendar, Clock, MapPin, FileText, Send, Plus, X, Loader2 } from "lucide-react";
import { Badge } from "@/components/ui/badge";
import { useConnectedAccounts } from "@/modules/email-calendar/hooks/useConnectedAccounts";
import type { ConnectedAccount } from "@/modules/email-calendar/types";
import { toast } from "sonner";

interface PushToCalendarDialogProps {
  open: boolean;
  onOpenChange: (open: boolean) => void;
  // Pre-fill from existing event
  initialData?: {
    title?: string;
    description?: string;
    location?: string;
    start?: Date;
    end?: Date;
  };
}

export function PushToCalendarDialog({
  open,
  onOpenChange,
  initialData,
}: PushToCalendarDialogProps) {
  const { accounts, createCalendarEvent } = useConnectedAccounts();

  // Filter accounts with calendar sync enabled
  const calendarAccounts = accounts.filter(
    (a) => a.calendarSettings.isSyncEnabled && a.syncStatus !== "failed"
  );

  const [selectedAccountId, setSelectedAccountId] = useState<string>("");
  const [title, setTitle] = useState(initialData?.title || "");
  const [description, setDescription] = useState(initialData?.description || "");
  const [location, setLocation] = useState(initialData?.location || "");
  const [isAllDay, setIsAllDay] = useState(false);
  const [start, setStart] = useState(() => {
    if (initialData?.start) {
      const s = new Date(initialData.start);
      s.setMinutes(s.getMinutes() - s.getTimezoneOffset());
      return s.toISOString().slice(0, 16);
    }
    return "";
  });
  const [end, setEnd] = useState(() => {
    if (initialData?.end) {
      const e = new Date(initialData.end);
      e.setMinutes(e.getMinutes() - e.getTimezoneOffset());
      return e.toISOString().slice(0, 16);
    }
    return "";
  });
  const [attendeeInput, setAttendeeInput] = useState("");
  const [attendees, setAttendees] = useState<string[]>([]);
  const [pushing, setPushing] = useState(false);

  // Reset form when dialog opens with initial data
  React.useEffect(() => {
    if (open) {
      setTitle(initialData?.title || "");
      setDescription(initialData?.description || "");
      setLocation(initialData?.location || "");
      setIsAllDay(false);
      setAttendees([]);
      setAttendeeInput("");
      setSelectedAccountId(calendarAccounts[0]?.id || "");

      if (initialData?.start) {
        const s = new Date(initialData.start);
        s.setMinutes(s.getMinutes() - s.getTimezoneOffset());
        setStart(s.toISOString().slice(0, 16));
      } else {
        setStart("");
      }
      if (initialData?.end) {
        const e = new Date(initialData.end);
        e.setMinutes(e.getMinutes() - e.getTimezoneOffset());
        setEnd(e.toISOString().slice(0, 16));
      } else {
        setEnd("");
      }
    }
  }, [open, initialData]);

  const addAttendee = () => {
    const email = attendeeInput.trim();
    if (email && /^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(email) && !attendees.includes(email)) {
      setAttendees((prev) => [...prev, email]);
      setAttendeeInput("");
    }
  };

  const removeAttendee = (email: string) => {
    setAttendees((prev) => prev.filter((a) => a !== email));
  };

  const handlePush = async () => {
    if (!selectedAccountId || !title || !start || !end) {
      toast.error("Please fill in all required fields");
      return;
    }

    setPushing(true);
    try {
      const result = await createCalendarEvent(selectedAccountId, {
        title,
        description: description || undefined,
        location: location || undefined,
        startTime: new Date(start).toISOString(),
        endTime: new Date(end).toISOString(),
        isAllDay,
        attendees: attendees.length > 0 ? attendees : undefined,
      });

      if (result.success) {
        const account = calendarAccounts.find((a) => a.id === selectedAccountId);
        const providerName = account?.provider === "google" ? "Google Calendar" : "Outlook Calendar";
        toast.success(`Event pushed to ${providerName} successfully!`);
        onOpenChange(false);
      } else {
        toast.error(result.error || "Failed to create event on calendar");
      }
    } catch (err) {
      toast.error("Failed to push event to calendar");
    } finally {
      setPushing(false);
    }
  };

  const getProviderIcon = (account: ConnectedAccount) => {
    return account.provider === "google" ? "🔵" : "🟦";
  };

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent className="max-w-2xl max-h-[90vh] overflow-y-auto">
        <DialogHeader>
          <DialogTitle className="text-xl font-semibold flex items-center gap-2">
            <Send className="h-5 w-5 text-primary" />
            Push to External Calendar
          </DialogTitle>
          <DialogDescription>
            Create this event directly on your connected Google Calendar or Outlook Calendar
          </DialogDescription>
        </DialogHeader>

        {calendarAccounts.length === 0 ? (
          <div className="py-8 text-center space-y-3">
            <Calendar className="h-12 w-12 text-muted-foreground mx-auto" />
            <p className="text-muted-foreground">
              No connected calendar accounts found.
            </p>
            <p className="text-sm text-muted-foreground">
              Go to Settings → Integrations to connect your Google or Microsoft account with calendar sync enabled.
            </p>
          </div>
        ) : (
          <div className="space-y-4">
            {/* Account Selection */}
            <div className="space-y-2">
              <Label className="flex items-center gap-2 font-medium">
                <Calendar className="h-4 w-4" />
                Calendar Account
              </Label>
              <Select value={selectedAccountId} onValueChange={setSelectedAccountId}>
                <SelectTrigger>
                  <SelectValue placeholder="Select a calendar account" />
                </SelectTrigger>
                <SelectContent>
                  {calendarAccounts.map((account) => (
                    <SelectItem key={account.id} value={account.id}>
                      <div className="flex items-center gap-2">
                        <span>{getProviderIcon(account)}</span>
                        <span>{account.handle}</span>
                        <Badge variant="outline" className="text-[10px] ml-1">
                          {account.provider === "google" ? "Google" : "Outlook"}
                        </Badge>
                      </div>
                    </SelectItem>
                  ))}
                </SelectContent>
              </Select>
            </div>

            {/* Event Title */}
            <div className="space-y-2">
              <Label htmlFor="push-title" className="flex items-center gap-2 font-medium">
                <FileText className="h-4 w-4" />
                Event Title <span className="text-destructive">*</span>
              </Label>
              <Input
                id="push-title"
                value={title}
                onChange={(e) => setTitle(e.target.value)}
                placeholder="Enter event title..."
              />
            </div>

            {/* All Day Toggle */}
            <div className="flex items-center justify-between">
              <Label className="flex items-center gap-2 font-medium">
                <Clock className="h-4 w-4" />
                All Day Event
              </Label>
              <Switch checked={isAllDay} onCheckedChange={setIsAllDay} />
            </div>

            {/* Date and Time */}
            <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
              <div className="space-y-2">
                <Label htmlFor="push-start" className="flex items-center gap-2">
                  <Clock className="h-4 w-4" />
                  Start <span className="text-destructive">*</span>
                </Label>
                <Input
                  id="push-start"
                  type={isAllDay ? "date" : "datetime-local"}
                  value={isAllDay ? start.slice(0, 10) : start}
                  onChange={(e) => setStart(e.target.value)}
                />
              </div>
              <div className="space-y-2">
                <Label htmlFor="push-end" className="flex items-center gap-2">
                  <Clock className="h-4 w-4" />
                  End <span className="text-destructive">*</span>
                </Label>
                <Input
                  id="push-end"
                  type={isAllDay ? "date" : "datetime-local"}
                  value={isAllDay ? end.slice(0, 10) : end}
                  onChange={(e) => setEnd(e.target.value)}
                />
              </div>
            </div>

            {/* Location */}
            <div className="space-y-2">
              <Label htmlFor="push-location" className="flex items-center gap-2 font-medium">
                <MapPin className="h-4 w-4" />
                Location
              </Label>
              <Input
                id="push-location"
                value={location}
                onChange={(e) => setLocation(e.target.value)}
                placeholder="Enter meeting location..."
              />
            </div>

            {/* Description */}
            <div className="space-y-2">
              <Label htmlFor="push-description" className="flex items-center gap-2 font-medium">
                <FileText className="h-4 w-4" />
                Description
              </Label>
              <Textarea
                id="push-description"
                value={description}
                onChange={(e) => setDescription(e.target.value)}
                placeholder="Add event description..."
                rows={3}
                className="resize-none"
              />
            </div>

            {/* Attendees */}
            <div className="space-y-2">
              <Label className="flex items-center gap-2 font-medium">
                <Send className="h-4 w-4" />
                Attendees
              </Label>
              <div className="flex gap-2">
                <Input
                  value={attendeeInput}
                  onChange={(e) => setAttendeeInput(e.target.value)}
                  onKeyDown={(e) => e.key === "Enter" && (e.preventDefault(), addAttendee())}
                  placeholder="Enter email address..."
                  className="flex-1"
                />
                <Button type="button" variant="outline" size="icon" onClick={addAttendee}>
                  <Plus className="h-4 w-4" />
                </Button>
              </div>
              {attendees.length > 0 && (
                <div className="flex flex-wrap gap-1.5 mt-2">
                  {attendees.map((email) => (
                    <Badge key={email} variant="secondary" className="gap-1 pr-1">
                      {email}
                      <button
                        onClick={() => removeAttendee(email)}
                        className="ml-1 rounded-full hover:bg-destructive/20 p-0.5"
                      >
                        <X className="h-3 w-3" />
                      </button>
                    </Badge>
                  ))}
                </div>
              )}
            </div>
          </div>
        )}

        <DialogFooter className="flex gap-2">
          <Button variant="outline" onClick={() => onOpenChange(false)}>
            Cancel
          </Button>
          {calendarAccounts.length > 0 && (
            <Button
              onClick={handlePush}
              disabled={pushing || !selectedAccountId || !title || !start || !end}
              className="bg-primary text-primary-foreground hover:bg-primary/90"
            >
              {pushing ? (
                <>
                  <Loader2 className="h-4 w-4 mr-2 animate-spin" />
                  Pushing...
                </>
              ) : (
                <>
                  <Send className="h-4 w-4 mr-2" />
                  Push to Calendar
                </>
              )}
            </Button>
          )}
        </DialogFooter>
      </DialogContent>
    </Dialog>
  );
}
