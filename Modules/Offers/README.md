# Offers Module - Backend

## Overview
This module handles the complete Offers management functionality including offer creation, modification, item management, activity tracking, and conversion to sales/service orders.

## Features
- ✅ Create, Read, Update, Delete (CRUD) operations for offers
- ✅ Offer items management (add, update, delete line items)
- ✅ Activity tracking and audit logging
- ✅ Status management and workflow
- ✅ Offer renewal (duplication)
- ✅ Offer conversion to Sales/Service Orders
- ✅ Statistics and analytics
- ✅ Advanced filtering and search
- ✅ Pagination support

## Architecture

### Models
- **Offer**: Main offer entity with contact info, financial details, and status
- **OfferItem**: Line items (articles/services) within an offer
- **OfferActivity**: Activity log for tracking changes and events

### DTOs (Data Transfer Objects)
- **OfferDto**: Full offer representation
- **CreateOfferDto**: For creating new offers
- **UpdateOfferDto**: For partial offer updates
- **OfferItemDto**: Offer item representation
- **OfferActivityDto**: Activity log entries
- **ConvertOfferDto**: Conversion request payload
- **OfferStatsDto**: Statistics response

### Services
- **IOfferService**: Service interface
- **OfferService**: Implementation with business logic

### Controllers
- **OffersController**: REST API endpoints

## Database Schema

### Tables
1. **offers** - Main offers table
2. **offer_items** - Line items
3. **offer_activities** - Activity tracking

### Key Features
- Automatic `total_amount` calculation (amount + taxes - discount)
- Automatic `total_price` calculation for items with discounts
- Triggers for:
  - Auto-updating timestamps
  - Logging status changes
  - Logging offer creation
  - Recalculating offer amount when items change

## API Endpoints

### Offers Management
\`\`\`
GET    /api/v1/offers              - Get all offers (with filters)
GET    /api/v1/offers/stats        - Get offer statistics
GET    /api/v1/offers/{id}         - Get single offer
POST   /api/v1/offers              - Create new offer
PATCH  /api/v1/offers/{id}         - Update offer
DELETE /api/v1/offers/{id}         - Delete offer
POST   /api/v1/offers/{id}/renew   - Renew (duplicate) offer
POST   /api/v1/offers/{id}/convert - Convert to sale/service order
\`\`\`

### Offer Items
\`\`\`
GET    /api/v1/offers/{id}/activities        - Get activities
POST   /api/v1/offers/{id}/items             - Add item
PATCH  /api/v1/offers/{id}/items/{itemId}    - Update item
DELETE /api/v1/offers/{id}/items/{itemId}    - Delete item
\`\`\`

## Query Parameters

### GET /api/v1/offers
- `status`: Filter by status (draft, sent, accepted, declined, cancelled, modified)
- `category`: Filter by category ID
- `source`: Filter by source
- `contact_id`: Filter by contact
- `date_from`: Filter from date (ISO 8601)
- `date_to`: Filter to date (ISO 8601)
- `search`: Search in title, contact name, description
- `page`: Page number (default: 1)
- `limit`: Items per page (default: 20)
- `sort_by`: Field to sort by (default: updated_at)
- `sort_order`: asc or desc (default: desc)

## Validation Rules

### Offers
- `title`: Required, max 255 characters
- `contactName`: Required, max 255 characters
- `contactEmail`: Optional, valid email format
- `amount`: Required, >= 0
- `taxes`: Optional, >= 0
- `discount`: Optional, >= 0
- `currency`: Required (USD, EUR, GBP, TND)
- `status`: Required (draft, sent, accepted, declined, cancelled, modified)

### Offer Items
- `itemName`: Required, max 255 characters
- `quantity`: Required, > 0
- `unitPrice`: Required, >= 0
- `type`: Required (article, service)
- `discount`: Optional, >= 0
- `discountType`: Optional (percentage, fixed)

## Business Logic

### Offer Creation
1. Generate unique offer ID (OFFER-XXXXXXXX)
2. Set initial status (draft/sent)
3. Create offer record
4. Add items if provided
5. Trigger auto-calculation of amounts
6. Log creation activity

### Offer Update
1. Validate offer exists
2. Update only provided fields
3. Auto-update `updated_at` timestamp
4. Recalculate totals if financial fields changed
5. Log status changes automatically (via trigger)

### Offer Renewal
1. Duplicate original offer with new ID
2. Set status to "draft"
3. Copy all items
4. Reset conversion fields
5. Set new timestamps

### Offer Conversion
1. Update offer status to "accepted"
2. Generate Sale/Service Order IDs
3. Update conversion tracking fields
4. Return conversion results

## Integration Points

### Dependencies
- ApplicationDbContext (EF Core)
- ILogger for logging
- Authorization middleware

### Required Setup
1. Add OfferService to DI container
2. Register entity configurations in DbContext
3. Apply database migrations
4. Configure authorization policies

## Error Handling

### Error Codes
- `VALIDATION_ERROR` (400): Invalid request data
- `UNAUTHORIZED` (401): Not authenticated
- `FORBIDDEN` (403): Insufficient permissions
- `OFFER_NOT_FOUND` (404): Offer doesn't exist
- `ITEM_NOT_FOUND` (404): Item doesn't exist
- `INTERNAL_ERROR` (500): Server error

## Testing

### Sample cURL Commands

#### Create Offer
\`\`\`bash
curl -X POST https://yourapi.com/api/v1/offers \
  -H "Authorization: Bearer YOUR_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "title": "Test Offer",
    "contactId": "CONT-001",
    "contactName": "John Doe",
    "status": "draft",
    "currency": "TND",
    "items": [
      {
        "type": "service",
        "itemId": "SRV-001",
        "itemName": "Consulting",
        "quantity": 10,
        "unitPrice": 150.00
      }
    ]
  }'
\`\`\`

#### Get Offers
\`\`\`bash
curl -X GET "https://yourapi.com/api/v1/offers?status=sent&page=1&limit=20" \
  -H "Authorization: Bearer YOUR_TOKEN"
\`\`\`

## Performance Considerations

1. **Indexes**: All frequently queried fields are indexed
2. **Pagination**: Always use pagination for list endpoints
3. **Eager Loading**: Items are included with offers via `.Include()`
4. **Computed Columns**: Database computes totals automatically

## Security

- All endpoints require authentication (`[Authorize]` attribute)
- User ID extracted from JWT claims
- All activities logged with user information
- Cascade delete ensures data integrity

## Future Enhancements

- [ ] Email notifications for status changes
- [ ] PDF generation for offers
- [ ] Template management
- [ ] Advanced analytics dashboard
- [ ] Bulk operations
- [ ] Export to Excel/CSV
- [ ] Workflow approvals
- [ ] Integration with external CRM systems
