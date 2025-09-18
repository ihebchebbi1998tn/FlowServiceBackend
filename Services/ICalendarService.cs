using BackendApi.DTOs;
using BackendApi.Models;

namespace BackendApi.Services
{
    public interface ICalendarService
    {
        // Calendar Events
        Task<IEnumerable<CalendarEventDto>> GetAllEventsAsync();
        Task<CalendarEventDto?> GetEventByIdAsync(Guid id);
        Task<IEnumerable<CalendarEventDto>> GetEventsByDateRangeAsync(DateTime start, DateTime end);
        Task<IEnumerable<CalendarEventDto>> GetEventsByContactAsync(Guid contactId);
        Task<CalendarEventDto> CreateEventAsync(CreateCalendarEventDto createDto);
        Task<CalendarEventDto?> UpdateEventAsync(Guid id, UpdateCalendarEventDto updateDto);
        Task<bool> DeleteEventAsync(Guid id);

        // Event Types
        Task<IEnumerable<EventTypeDto>> GetAllEventTypesAsync();
        Task<EventTypeDto?> GetEventTypeByIdAsync(string id);
        Task<EventTypeDto> CreateEventTypeAsync(CreateEventTypeDto createDto);
        Task<bool> DeleteEventTypeAsync(string id);

        // Event Attendees
        Task<IEnumerable<EventAttendeeDto>> GetEventAttendeesAsync(Guid eventId);
        Task<EventAttendeeDto> CreateEventAttendeeAsync(CreateEventAttendeeDto createDto);
        Task<bool> DeleteEventAttendeeAsync(Guid id);

        // Event Reminders
        Task<IEnumerable<EventReminderDto>> GetEventRemindersAsync(Guid eventId);
        Task<EventReminderDto> CreateEventReminderAsync(CreateEventReminderDto createDto);
        Task<bool> DeleteEventReminderAsync(Guid id);
    }
}