using MyApi.Data;
using MyApi.DTOs;
using MyApi.Models;
using Microsoft.EntityFrameworkCore;

namespace MyApi.Services
{
    public class ContactNoteService : IContactNoteService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<ContactNoteService> _logger;

        public ContactNoteService(ApplicationDbContext context, ILogger<ContactNoteService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<ContactNoteListResponseDto> GetNotesByContactIdAsync(int contactId)
        {
            try
            {
                var notes = await _context.ContactNotes
                    .Where(n => n.ContactId == contactId)
                    .OrderByDescending(n => n.CreatedAt)
                    .ToListAsync();

                var noteDtos = notes.Select(MapToNoteDto).ToList();

                return new ContactNoteListResponseDto
                {
                    Notes = noteDtos,
                    TotalCount = noteDtos.Count
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting notes for contact {ContactId}", contactId);
                throw;
            }
        }

        public async Task<ContactNoteDto?> GetNoteByIdAsync(int id)
        {
            try
            {
                var note = await _context.ContactNotes
                    .Where(n => n.Id == id)
                    .FirstOrDefaultAsync();

                return note != null ? MapToNoteDto(note) : null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting note by id {NoteId}", id);
                throw;
            }
        }

        public async Task<ContactNoteDto> CreateNoteAsync(CreateContactNoteRequestDto createDto, string createdByUser)
        {
            try
            {
                // Verify contact exists
                var contactExists = await _context.Contacts
                    .AnyAsync(c => c.Id == createDto.ContactId && !c.IsDeleted);

                if (!contactExists)
                {
                    throw new InvalidOperationException("Contact not found");
                }

                var note = new ContactNote
                {
                    ContactId = createDto.ContactId,
                    Content = createDto.Content,
                    CreatedBy = createdByUser,
                    CreatedAt = DateTime.UtcNow
                };

                _context.ContactNotes.Add(note);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Contact note created successfully with ID {NoteId}", note.Id);
                return MapToNoteDto(note);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating contact note");
                throw;
            }
        }

        public async Task<ContactNoteDto?> UpdateNoteAsync(int id, UpdateContactNoteRequestDto updateDto)
        {
            try
            {
                var note = await _context.ContactNotes
                    .Where(n => n.Id == id)
                    .FirstOrDefaultAsync();

                if (note == null)
                {
                    return null;
                }

                note.Content = updateDto.Content;

                await _context.SaveChangesAsync();

                _logger.LogInformation("Contact note updated successfully with ID {NoteId}", id);
                return MapToNoteDto(note);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating contact note with ID {NoteId}", id);
                throw;
            }
        }

        public async Task<bool> DeleteNoteAsync(int id)
        {
            try
            {
                var note = await _context.ContactNotes
                    .Where(n => n.Id == id)
                    .FirstOrDefaultAsync();

                if (note == null)
                {
                    return false;
                }

                _context.ContactNotes.Remove(note);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Contact note deleted successfully with ID {NoteId}", id);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting contact note with ID {NoteId}", id);
                throw;
            }
        }

        private static ContactNoteDto MapToNoteDto(ContactNote note)
        {
            return new ContactNoteDto
            {
                Id = note.Id,
                ContactId = note.ContactId,
                Content = note.Content,
                CreatedAt = note.CreatedAt,
                CreatedBy = note.CreatedBy
            };
        }
    }
}