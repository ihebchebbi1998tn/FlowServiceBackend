# Service Orders Module - Backend

## Overview
This module handles the complete Service Orders management functionality including order creation from sales, technician dispatch, work tracking, time entry management, material usage, and service completion with approval workflows.

## Features
- ✅ Create, Read, Update, Delete (CRUD) operations for service orders
- ✅ Create service orders from completed sales
- ✅ Job management and technician dispatch
- ✅ Time entry tracking for technicians
- ✅ Material usage and inventory tracking
- ✅ Work order status management and workflow
- ✅ Approval workflows and sign-offs
- ✅ Financial tracking and cost calculations
- ✅ Statistics and analytics
- ✅ Advanced filtering and search
- ✅ Pagination support

## Architecture

### Models
- **ServiceOrder**: Main service order entity with financial and status tracking
- **ServiceOrderJob**: Individual jobs within a service order

### DTOs (Data Transfer Objects)
- **ServiceOrderDto**: Full service order representation
- **CreateServiceOrderDto**: For creating new service orders
- **UpdateServiceOrderDto**: For partial service order updates
- **CreateFromSaleDto**: For creating from completed sales
- **ServiceOrderJobDto**: Job representation
- **ServiceOrderStatsDto**: Statistics response
- **PaginatedServiceOrderResponse**: Paginated list response

### Services
- **IServiceOrderService**: Service interface
- **ServiceOrderService**: Implementation with business logic

### Controllers
- **ServiceOrdersController**: REST API endpoints

## Database Schema

### Tables
1. **service_orders** - Main service orders table
2. **service_order_jobs** - Individual jobs within orders

### Key Features
- Automatic `total_labor_cost` calculation from time entries
- Automatic `total_material_cost` calculation from materials
- Automatic `total_cost` calculation (labor + materials + overhead)
- Automatic `profit_margin` calculation
- Timestamps for creation and updates

## API Endpoints

### Service Orders Management
\`\`\`
GET    /api/v1/service-orders              - Get all service orders (with filters)
GET    /api/v1/service-orders/stats        - Get service order statistics
GET    /api/v1/service-orders/{id}         - Get single service order
POST   /api/v1/service-orders              - Create new service order
POST   /api/v1/service-orders/from-sale    - Create from completed sale
PATCH  /api/v1/service-orders/{id}         - Update service order
DELETE /api/v1/service-orders/{id}         - Delete service order
\`\`\`

### Status Management
\`\`\`
PATCH  /api/v1/service-orders/{id}/status           - Update status
POST   /api/v1/service-orders/{id}/approve          - Approve order
POST   /api/v1/service-orders/{id}/complete         - Complete order
POST   /api/v1/service-orders/{id}/cancel           - Cancel order
\`\`\`

### Jobs Management
\`\`\`
GET    /api/v1/service-orders/{id}/jobs             - Get jobs
POST   /api/v1/service-orders/{id}/jobs             - Add job
PATCH  /api/v1/service-orders/{id}/jobs/{jobId}     - Update job
DELETE /api/v1/service-orders/{id}/jobs/{jobId}     - Delete job
\`\`\`

## Query Parameters

### GET /api/v1/service-orders
- `status`: Filter by status (draft, scheduled, in_progress, completed, cancelled, on_hold)
- `priority`: Filter by priority (low, medium, high, urgent)
- `contact_id`: Filter by contact
- `installation_id`: Filter by installation
- `date_from`: Filter from date (ISO 8601)
- `date_to`: Filter to date (ISO 8601)
- `search`: Search in order number, contact name, description
- `page`: Page number (default: 1)
- `limit`: Items per page (default: 20)
- `sort_by`: Field to sort by (default: updated_at)
- `sort_order`: asc or desc (default: desc)

## Validation Rules

### Service Orders
- `orderNumber`: Required, unique, max 50 characters
- `status`: Required (draft, scheduled, in_progress, completed, cancelled, on_hold)
- `priority`: Required (low, medium, high, urgent)
- `contactId`: Required
- `installationId`: Optional
- `description`: Required, max 2000 characters
- `scheduledDate`: Required, valid date
- `estimatedDuration`: Required, > 0
- `estimatedCost`: Required, >= 0
- `currency`: Required (USD, EUR, GBP, TND)

### Jobs
- `jobType`: Required (inspection, repair, maintenance, installation, upgrade)
- `description`: Required, max 1000 characters
- `assignedTechnician`: Required, max 255 characters
- `estimatedHours`: Required, > 0
- `status`: Required (pending, in_progress, completed, on_hold)

## Business Logic

### Service Order Creation
1. Generate unique order number (SO-XXXXXXXX)
2. Set initial status (draft/scheduled)
3. Create service order record
4. Create associated jobs
5. Calculate estimated costs
6. Log creation activity

### Create from Sale
1. Validate sale is completed
2. Generate new service order
3. Copy relevant information from sale
4. Create initial job
5. Set scheduled date
6. Link to installation if applicable

### Status Transitions
1. Draft → Scheduled (when technician assigned)
2. Scheduled → In Progress (when work starts)
3. In Progress → Completed (when work done)
4. Any → On Hold (pause work)
5. Any → Cancelled (cancel order)

### Approval Workflow
1. Complete all work
2. Submit for approval
3. Manager reviews
4. Approve or request changes
5. Generate invoice
6. Mark as completed

## Integration Points

### Dependencies
- ApplicationDbContext (EF Core)
- ILogger for logging
- Authorization middleware

### Related Modules
- **Sales**: Service orders created from completed sales
- **Installations**: Service orders linked to installations
- **Contacts**: Contact information for service orders
- **Offers**: Original offer information

## Error Handling

### Error Codes
- `VALIDATION_ERROR` (400): Invalid request data
- `UNAUTHORIZED` (401): Not authenticated
- `FORBIDDEN` (403): Insufficient permissions
- `ORDER_NOT_FOUND` (404): Service order doesn't exist
- `JOB_NOT_FOUND` (404): Job doesn't exist
- `SALE_NOT_FOUND` (404): Sale doesn't exist
- `INVALID_STATUS_TRANSITION` (400): Invalid status change
- `INTERNAL_ERROR` (500): Server error

## Testing

### Sample cURL Commands

#### Create Service Order
\`\`\`bash
curl -X POST https://yourapi.com/api/v1/service-orders \
  -H "Authorization: Bearer YOUR_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "status": "draft",
    "priority": "high",
    "contactId": "CONT-001",
    "installationId": "INST-001",
    "description": "HVAC system maintenance and repair",
    "scheduledDate": "2025-02-01",
    "estimatedDuration": 4,
    "estimatedCost": 500.00,
    "currency": "TND",
    "jobs": [
      {
        "jobType": "maintenance",
        "description": "Regular maintenance check",
        "assignedTechnician": "Ahmed",
        "estimatedHours": 2
      }
    ]
  }'
\`\`\`

#### Create from Sale
\`\`\`bash
curl -X POST https://yourapi.com/api/v1/service-orders/from-sale \
  -H "Authorization: Bearer YOUR_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "saleId": "SALE-001",
    "scheduledDate": "2025-02-01",
    "priority": "high"
  }'
\`\`\`

#### Get Service Orders
\`\`\`bash
curl -X GET "https://yourapi.com/api/v1/service-orders?status=scheduled&page=1&limit=20" \
  -H "Authorization: Bearer YOUR_TOKEN"
\`\`\`

## Performance Considerations

1. **Indexes**: All frequently queried fields are indexed
2. **Pagination**: Always use pagination for list endpoints
3. **Eager Loading**: Related data is included via `.Include()`
4. **Computed Columns**: Database computes costs automatically

## Security

- All endpoints require authentication (`[Authorize]` attribute)
- User ID extracted from JWT claims
- All activities logged with user information
- Cascade delete ensures data integrity

## Future Enhancements

- [ ] Real-time technician location tracking
- [ ] Mobile app for technician work orders
- [ ] Automated scheduling and dispatch
- [ ] Customer notifications and updates
- [ ] Photo/video documentation of work
- [ ] Integration with accounting system
- [ ] Advanced analytics and reporting
- [ ] Predictive maintenance recommendations
