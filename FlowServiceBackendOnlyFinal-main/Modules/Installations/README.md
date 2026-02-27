# Installations Module - Backend

## Overview
This module handles the complete Installations management functionality including installation creation, maintenance history tracking, warranty management, and service coordination.

## Features
- ✅ Create, Read, Update, Delete (CRUD) operations for installations
- ✅ Maintenance history tracking and scheduling
- ✅ Warranty management and expiration tracking
- ✅ Location and specification management
- ✅ Contact and metadata tracking
- ✅ Status management and workflow
- ✅ Statistics and analytics
- ✅ Advanced filtering and search
- ✅ Pagination support

## Architecture

### Models
- **Installation**: Main installation entity with location, specifications, warranty, and maintenance details
- **MaintenanceHistory**: Maintenance records and service history tracking

### DTOs (Data Transfer Objects)
- **InstallationDto**: Full installation representation
- **CreateInstallationDto**: For creating new installations
- **UpdateInstallationDto**: For partial installation updates
- **MaintenanceHistoryDto**: Maintenance record representation
- **InstallationStatsDto**: Statistics response
- **PaginatedInstallationResponse**: Paginated list response

### Services
- **IInstallationService**: Service interface
- **InstallationService**: Implementation with business logic

### Controllers
- **InstallationsController**: REST API endpoints

## Database Schema

### Tables
1. **installations** - Main installations table
2. **maintenance_histories** - Maintenance tracking

### Key Features
- Automatic `next_maintenance_date` calculation based on frequency
- Automatic `warranty_expiry_date` calculation from installation date
- Automatic `total_maintenance_cost` calculation
- Timestamps for creation and updates

## API Endpoints

### Installations Management
\`\`\`
GET    /api/v1/installations              - Get all installations (with filters)
GET    /api/v1/installations/stats        - Get installation statistics
GET    /api/v1/installations/{id}         - Get single installation
POST   /api/v1/installations              - Create new installation
PATCH  /api/v1/installations/{id}         - Update installation
DELETE /api/v1/installations/{id}         - Delete installation
\`\`\`

### Maintenance Management
\`\`\`
GET    /api/v1/installations/{id}/maintenance        - Get maintenance history
POST   /api/v1/installations/{id}/maintenance        - Add maintenance record
PATCH  /api/v1/installations/{id}/maintenance/{mId}  - Update maintenance record
DELETE /api/v1/installations/{id}/maintenance/{mId}  - Delete maintenance record
\`\`\`

## Query Parameters

### GET /api/v1/installations
- `status`: Filter by status (active, inactive, maintenance, warranty_expired)
- `category`: Filter by category
- `contact_id`: Filter by contact
- `warranty_status`: Filter by warranty status (valid, expired, expiring_soon)
- `maintenance_due`: Filter by maintenance due status (true/false)
- `date_from`: Filter from date (ISO 8601)
- `date_to`: Filter to date (ISO 8601)
- `search`: Search in title, location, contact name
- `page`: Page number (default: 1)
- `limit`: Items per page (default: 20)
- `sort_by`: Field to sort by (default: updated_at)
- `sort_order`: asc or desc (default: desc)

## Validation Rules

### Installations
- `title`: Required, max 255 characters
- `category`: Required, max 100 characters
- `status`: Required (active, inactive, maintenance, warranty_expired)
- `location`: Required object with address, city, country
- `specifications`: Required object with model, serialNumber, capacity
- `warranty`: Required object with startDate, endDate, provider
- `maintenance`: Required object with frequency, lastDate, nextDate
- `contact`: Required object with name, email, phone
- `metadata`: Optional object for custom fields

### Maintenance History
- `date`: Required, valid date
- `type`: Required (preventive, corrective, inspection)
- `description`: Required, max 1000 characters
- `cost`: Optional, >= 0
- `technician`: Optional, max 255 characters

## Business Logic

### Installation Creation
1. Generate unique installation ID (INST-XXXXXXXX)
2. Set initial status (active/inactive)
3. Create installation record
4. Calculate warranty expiry date
5. Calculate next maintenance date
6. Log creation activity

### Installation Update
1. Validate installation exists
2. Update only provided fields
3. Auto-update `updated_at` timestamp
4. Recalculate dates if relevant fields changed
5. Log status changes automatically

### Maintenance Tracking
1. Record maintenance activity
2. Update `last_maintenance_date`
3. Calculate next maintenance date
4. Update maintenance cost totals
5. Log maintenance activity

## Integration Points

### Dependencies
- ApplicationDbContext (EF Core)
- ILogger for logging
- Authorization middleware

### Related Modules
- **Sales**: Installations created from completed sales
- **ServiceOrders**: Service orders created for installations
- **Contacts**: Contact information for installation locations

## Error Handling

### Error Codes
- `VALIDATION_ERROR` (400): Invalid request data
- `UNAUTHORIZED` (401): Not authenticated
- `FORBIDDEN` (403): Insufficient permissions
- `INSTALLATION_NOT_FOUND` (404): Installation doesn't exist
- `MAINTENANCE_NOT_FOUND` (404): Maintenance record doesn't exist
- `INTERNAL_ERROR` (500): Server error

## Testing

### Sample cURL Commands

#### Create Installation
\`\`\`bash
curl -X POST https://yourapi.com/api/v1/installations \
  -H "Authorization: Bearer YOUR_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "title": "HVAC System Installation",
    "category": "HVAC",
    "status": "active",
    "location": {
      "address": "123 Main St",
      "city": "Tunis",
      "country": "Tunisia"
    },
    "specifications": {
      "model": "XYZ-2000",
      "serialNumber": "SN123456",
      "capacity": "5000 BTU"
    },
    "warranty": {
      "startDate": "2025-01-15",
      "endDate": "2027-01-15",
      "provider": "Manufacturer"
    },
    "maintenance": {
      "frequency": "quarterly",
      "lastDate": "2025-01-15",
      "nextDate": "2025-04-15"
    },
    "contact": {
      "name": "John Doe",
      "email": "john@example.com",
      "phone": "+216 XX XXX XXX"
    }
  }'
\`\`\`

#### Get Installations
\`\`\`bash
curl -X GET "https://yourapi.com/api/v1/installations?status=active&page=1&limit=20" \
  -H "Authorization: Bearer YOUR_TOKEN"
\`\`\`

## Performance Considerations

1. **Indexes**: All frequently queried fields are indexed
2. **Pagination**: Always use pagination for list endpoints
3. **Eager Loading**: Related data is included via `.Include()`
4. **Computed Columns**: Database computes dates automatically

## Security

- All endpoints require authentication (`[Authorize]` attribute)
- User ID extracted from JWT claims
- All activities logged with user information
- Cascade delete ensures data integrity

## Future Enhancements

- [ ] Email notifications for warranty expiration
- [ ] Maintenance scheduling and reminders
- [ ] PDF generation for installation certificates
- [ ] Integration with IoT sensors for monitoring
- [ ] Predictive maintenance analytics
- [ ] Mobile app for technician access
- [ ] Integration with external service providers
