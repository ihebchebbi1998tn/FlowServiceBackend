# Sales Module - Backend

## Overview
This module handles the complete Sales management functionality including sale creation from offers, modification, item management, activity tracking, and fulfillment management.

## Features
- Create, Read, Update, Delete (CRUD) operations for sales
- Create sales from offers (exact copy conversion)
- Sale items management (add, update, delete line items)
- Activity tracking and audit logging
- Status and stage management workflow
- Fulfillment tracking (materials and service orders)
- Statistics and analytics
- Advanced filtering and search
- Pagination support

## Architecture

### Models
- **Sale**: Main sale entity with contact info, financial details, status, and fulfillment tracking
- **SaleItem**: Line items (articles/services) within a sale with service order tracking
- **SaleActivity**: Activity log for tracking changes and events

### DTOs (Data Transfer Objects)
- **SaleDto**: Full sale representation
- **CreateSaleDto**: For creating new sales
- **CreateSaleFromOfferDto**: For converting offers to sales
- **UpdateSaleDto**: For partial sale updates
- **SaleItemDto**: Sale item representation
- **SaleActivityDto**: Activity log entries
- **SaleStatsDto**: Statistics response

### Services
- **ISaleService**: Service interface
- **SaleService**: Implementation with business logic

### Controllers
- **SalesController**: REST API endpoints

## Database Schema

### Tables
1. **sales** - Main sales table
2. **sale_items** - Line items with fulfillment tracking
3. **sale_activities** - Activity tracking

### Key Features
- Automatic `total_amount` calculation (amount + taxes - discount)
- Automatic `total_price` calculation for items with discounts
- Foreign key to `offers` table for conversion tracking
- Triggers for:
  - Auto-updating timestamps
  - Logging status changes
  - Logging sale creation
  - Recalculating sale amount when items change

## API Endpoints

### Sales Management
\`\`\`
GET    /api/v1/sales                - Get all sales (with filters)
GET    /api/v1/sales/stats          - Get sale statistics
GET    /api/v1/sales/{id}           - Get single sale
POST   /api/v1/sales                - Create new sale
POST   /api/v1/sales/from-offer     - Create sale from offer (conversion)
PATCH  /api/v1/sales/{id}           - Update sale
DELETE /api/v1/sales/{id}           - Delete sale
\`\`\`

### Sale Items
\`\`\`
GET    /api/v1/sales/{id}/activities        - Get activities
POST   /api/v1/sales/{id}/items             - Add item
PATCH  /api/v1/sales/{id}/items/{itemId}    - Update item
DELETE /api/v1/sales/{id}/items/{itemId}    - Delete item
\`\`\`

## Query Parameters

### GET /api/v1/sales
- `status`: Filter by status (new_offer, won, lost, redefined, draft, sent, accepted, completed, cancelled)
- `stage`: Filter by stage (offer, negotiation, closed, converted)
- `priority`: Filter by priority (low, medium, high, urgent)
- `contact_id`: Filter by contact
- `date_from`: Filter from date (ISO 8601)
- `date_to`: Filter to date (ISO 8601)
- `search`: Search in title, contact name, description
- `page`: Page number (default: 1)
- `limit`: Items per page (default: 20)
- `sort_by`: Field to sort by (default: updated_at)
- `sort_order`: asc or desc (default: desc)

## Conversion from Offers

### POST /api/v1/sales/from-offer
Creates an exact copy of an offer as a sale. This is the primary way to convert accepted offers into sales.

**Request Body:**
\`\`\`json
{
  "offerId": "OFFER-001",
  "status": "won",
  "priority": "high"
}
\`\`\`

**Process:**
1. Fetches the complete offer with all items
2. Creates a new sale with identical data
3. Copies all offer items to sale items
4. Sets sale status to "won" and stage to "closed"
5. Links sale back to original offer via `offer_id`
6. Updates offer status to "accepted" and sets `converted_to_sale_id`
7. Adds "Converted" tag to sale
8. Returns the created sale

## Integration Points

### Dependencies
- ApplicationDbContext (EF Core)
- ILogger for logging
- Authorization middleware
- Offers module (for conversion)
- Contacts module (for contact info)

### Required Setup
1. Add SaleService to DI container in Program.cs:
   \`\`\`csharp
   builder.Services.AddScoped<ISaleService, SaleService>();
   \`\`\`
2. Register entity configurations in DbContext
3. Apply database migrations
4. Configure authorization policies

## Error Handling

### Error Codes
- `VALIDATION_ERROR` (400): Invalid request data
- `UNAUTHORIZED` (401): Not authenticated
- `FORBIDDEN` (403): Insufficient permissions
- `SALE_NOT_FOUND` (404): Sale doesn't exist
- `OFFER_NOT_FOUND` (404): Offer doesn't exist (for conversion)
- `ITEM_NOT_FOUND` (404): Item doesn't exist
- `INTERNAL_ERROR` (500): Server error

## Testing

### Sample cURL Commands

#### Create Sale from Offer
\`\`\`bash
curl -X POST https://yourapi.com/api/v1/sales/from-offer \
  -H "Authorization: Bearer YOUR_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "offerId": "OFFER-001",
    "status": "won",
    "priority": "high"
  }'
\`\`\`

#### Get Sales
\`\`\`bash
curl -X GET "https://yourapi.com/api/v1/sales?status=won&page=1&limit=20" \
  -H "Authorization: Bearer YOUR_TOKEN"
\`\`\`

## Performance Considerations

1. **Indexes**: All frequently queried fields are indexed
2. **Pagination**: Always use pagination for list endpoints
3. **Eager Loading**: Items are included with sales via `.Include()`
4. **Computed Columns**: Database computes totals automatically

## Security

- All endpoints require authentication (`[Authorize]` attribute)
- User ID extracted from JWT claims
- All activities logged with user information
- Cascade delete ensures data integrity
- Foreign key constraints prevent orphaned records

## Future Enhancements

- [ ] Payment tracking and invoicing
- [ ] Delivery management
- [ ] Email notifications for status changes
- [ ] PDF generation for sales orders
- [ ] Advanced analytics dashboard
- [ ] Bulk operations
- [ ] Export to Excel/CSV
- [ ] Workflow approvals
- [ ] Integration with accounting systems
- [ ] Recurring sales/subscriptions
