# MyApi - API Documentation

## 🔧 API Base URL
```
https://myapi.onrender.com
```

## 🔐 Authentication
All endpoints require JWT Bearer token authentication (except `/api/auth/*` and `/api/dev/*`)

### Get Test Token (Development)
```bash
# Get 24-hour development token
curl -X GET "https://myapi.onrender.com/api/dev/token"

# Get 1-year permanent test token  
curl -X GET "https://myapi.onrender.com/api/dev/permanent-token"
```

## 📋 Complete API Endpoints Overview

### Authentication Endpoints
- `POST /api/auth/login` - User login
- `POST /api/auth/signup` - User registration
- `POST /api/auth/user-login` - Regular user login
- `GET /api/auth/me` - Get current user
- `PUT /api/auth/me` - Update current user
- `POST /api/auth/logout` - User logout
- `POST /api/auth/refresh` - Refresh token

### User Management
- `GET /api/users` - Get all users
- `GET /api/users/{id}` - Get user by ID
- `POST /api/users` - Create user
- `PUT /api/users/{id}` - Update user
- `DELETE /api/users/{id}` - Delete user

### Roles Management
- `GET /api/roles` - Get all roles
- `GET /api/roles/{id}` - Get role by ID
- `POST /api/roles` - Create role
- `PUT /api/roles/{id}` - Update role
- `DELETE /api/roles/{id}` - Delete role

### Skills Management
- `GET /api/skills` - Get all skills
- `GET /api/skills/{id}` - Get skill by ID
- `POST /api/skills` - Create skill
- `PUT /api/skills/{id}` - Update skill
- `DELETE /api/skills/{id}` - Delete skill

### Contacts Management
- `GET /api/contacts` - Get all contacts
- `GET /api/contacts/{id}` - Get contact by ID
- `POST /api/contacts` - Create contact
- `PUT /api/contacts/{id}` - Update contact
- `DELETE /api/contacts/{id}` - Delete contact
- `GET /api/contacts/search` - Search contacts
- `POST /api/contacts/import` - Bulk import contacts

### Articles Management
- `GET /api/articles` - Get all articles
- `GET /api/articles/{id}` - Get article by ID
- `POST /api/articles` - Create article
- `PUT /api/articles/{id}` - Update article
- `DELETE /api/articles/{id}` - Delete article

### Calendar Management
- `GET /api/calendar` - Get calendar events
- `GET /api/calendar/{id}` - Get event by ID
- `POST /api/calendar` - Create event
- `PUT /api/calendar/{id}` - Update event
- `DELETE /api/calendar/{id}` - Delete event

## 🔍 LOOKUPS API - Complete CRUD Operations

### Available Lookup Types
- `article-categories` - Categories for articles/materials
- `article-statuses` - Status values for articles
- `service-categories` - Categories for services
- `task-statuses` - Status values for tasks
- `event-types` - Types of calendar events
- `priorities` - Priority levels
- `technician-statuses` - Technician availability statuses
- `leave-types` - Types of leave/absence
- `project-statuses` - Project status values
- `project-types` - Types of projects
- `offer-statuses` - Offer/proposal statuses
- `skills` - Skills and competencies
- `countries` - Country list
- `currencies` - Currency definitions

### Lookup Items CRUD Operations

#### 1. GET All Items by Type
```bash
# Get all article categories
curl -X GET "https://flowservicebackend.onrender.com/api/lookups/article-categories" \
  -H "Authorization: Bearer YOUR_TOKEN"

# Get all task statuses
curl -X GET "https://flowservicebackend.onrender.com/api/lookups/task-statuses" \
  -H "Authorization: Bearer YOUR_TOKEN"

# Get all event types
curl -X GET "https://flowservicebackend.onrender.com/api/lookups/event-types" \
  -H "Authorization: Bearer YOUR_TOKEN"

# Get all priorities
curl -X GET "https://flowservicebackend.onrender.com/api/lookups/priorities" \
  -H "Authorization: Bearer YOUR_TOKEN"

# Get all skills
curl -X GET "https://flowservicebackend.onrender.com/api/lookups/skills" \
  -H "Authorization: Bearer YOUR_TOKEN"

# Get all countries
curl -X GET "https://flowservicebackend.onrender.com/api/lookups/countries" \
  -H "Authorization: Bearer YOUR_TOKEN"
```

#### 2. GET Single Item by ID
```bash
# Get specific article category
curl -X GET "https://flowservicebackend.onrender.com/api/lookups/article-categories/hardware" \
  -H "Authorization: Bearer YOUR_TOKEN"
```

#### 3. CREATE New Lookup Item
```bash
# Create new article category
curl -X POST "https://flowservicebackend.onrender.com/api/lookups/article-categories" \
  -H "Authorization: Bearer YOUR_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "name": "Electronics",
    "description": "Electronic components and devices",
    "color": "#3B82F6",
    "isActive": true,
    "sortOrder": 0
  }'

# Create new task status
curl -X POST "https://flowservicebackend.onrender.com/api/lookups/task-statuses" \
  -H "Authorization: Bearer YOUR_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "name": "In Review",
    "description": "Task is under review",
    "color": "#F59E0B",
    "isActive": true,
    "sortOrder": 2,
    "isCompleted": false
  }'

# Create new priority
curl -X POST "https://flowservicebackend.onrender.com/api/lookups/priorities" \
  -H "Authorization: Bearer YOUR_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "name": "Critical",
    "description": "Critical priority items",
    "color": "#DC2626",
    "isActive": true,
    "sortOrder": 0,
    "level": 5
  }'

# Create new skill
curl -X POST "https://flowservicebackend.onrender.com/api/lookups/skills" \
  -H "Authorization: Bearer YOUR_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "name": "React.js",
    "description": "React JavaScript library",
    "color": "#61DAFB",
    "isActive": true,
    "sortOrder": 0,
    "category": "Frontend Development"
  }'

# Create new country
curl -X POST "https://flowservicebackend.onrender.com/api/lookups/countries" \
  -H "Authorization: Bearer YOUR_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "name": "United States",
    "description": "United States of America",
    "isActive": true,
    "sortOrder": 0
  }'
```

#### 4. UPDATE Existing Lookup Item
```bash
# Update article category
curl -X PUT "https://flowservicebackend.onrender.com/api/lookups/article-categories/hardware" \
  -H "Authorization: Bearer YOUR_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "name": "Hardware & Tools",
    "description": "Updated description for hardware tools",
    "color": "#059669",
    "isActive": true,
    "sortOrder": 1
  }'

# Update task status
curl -X PUT "https://flowservicebackend.onrender.com/api/lookups/task-statuses/progress" \
  -H "Authorization: Bearer YOUR_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "name": "In Progress",
    "description": "Currently being worked on",
    "color": "#3B82F6",
    "isCompleted": false
  }'
```

#### 5. DELETE Lookup Item (Soft Delete)
```bash
# Delete article category
curl -X DELETE "https://flowservicebackend.onrender.com/api/lookups/article-categories/old-category" \
  -H "Authorization: Bearer YOUR_TOKEN"

# Delete task status
curl -X DELETE "https://flowservicebackend.onrender.com/api/lookups/task-statuses/deprecated-status" \
  -H "Authorization: Bearer YOUR_TOKEN"

# Delete skill
curl -X DELETE "https://flowservicebackend.onrender.com/api/lookups/skills/obsolete-skill" \
  -H "Authorization: Bearer YOUR_TOKEN"
```

### Currency CRUD Operations

#### 1. GET All Currencies
```bash
curl -X GET "https://flowservicebackend.onrender.com/api/lookups/currencies" \
  -H "Authorization: Bearer YOUR_TOKEN"
```

#### 2. CREATE New Currency
```bash
curl -X POST "https://flowservicebackend.onrender.com/api/lookups/currencies" \
  -H "Authorization: Bearer YOUR_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "name": "Japanese Yen (¥)",
    "symbol": "¥",
    "code": "JPY",
    "isActive": true,
    "isDefault": false,
    "sortOrder": 4
  }'
```

## 📊 Response Formats

### Lookup List Response
```json
{
  "items": [
    {
      "id": "hardware",
      "name": "Hardware",
      "description": "Hardware tools and equipment",
      "color": "#EF4444",
      "isActive": true,
      "sortOrder": 1,
      "createdAt": "2025-01-20T10:00:00Z",
      "updatedAt": "2025-01-20T10:00:00Z",
      "createdUser": "system",
      "modifyUser": null,
      "level": null,
      "isCompleted": null,
      "defaultDuration": null,
      "isAvailable": null,
      "isPaid": null,
      "category": null
    }
  ],
  "totalCount": 4
}
```

### Currency List Response
```json
{
  "items": [
    {
      "id": "USD",
      "name": "USD ($)",
      "symbol": "$",
      "code": "USD",
      "isActive": true,
      "isDefault": true,
      "sortOrder": 0,
      "createdAt": "2025-01-20T10:00:00Z",
      "updatedAt": null,
      "createdUser": "system",
      "modifyUser": null
    }
  ],
  "totalCount": 4
}
```

### Error Response
```json
{
  "title": "Error",
  "detail": "An error occurred while processing the request",
  "status": 500,
  "timestamp": "2025-01-20T10:00:00Z"
}
```

## 🔧 Field Descriptions

### Lookup Item Fields
- `id` - Unique identifier (auto-generated for new items)
- `name` - Display name (required, max 100 chars)
- `description` - Optional description (max 500 chars)
- `color` - Hex color code for UI (max 20 chars)
- `isActive` - Whether item is active/available
- `sortOrder` - Sort order for display
- `level` - Priority level (for priorities)
- `isCompleted` - Completion status (for statuses)
- `defaultDuration` - Default duration in minutes (for events)
- `isAvailable` - Availability flag (for technician statuses)
- `isPaid` - Paid status (for leave types)
- `category` - Category grouping (for skills)

### Currency Fields
- `id` - Currency code (e.g., "USD")
- `name` - Display name (e.g., "USD ($)")
- `symbol` - Currency symbol (e.g., "$")
- `code` - 3-letter currency code (required, max 3 chars)
- `isActive` - Whether currency is active
- `isDefault` - Whether this is the default currency
- `sortOrder` - Sort order for display

## 🚨 Important Notes

### Authentication Required
All lookup endpoints require authentication. Use the `/api/dev/token` endpoint to get a test token:

```bash
# Get token first
TOKEN=$(curl -s -X GET "https://flowservicebackend.onrender.com/api/dev/token" | jq -r '.token')

# Use token in subsequent requests
curl -X GET "https://flowservicebackend.onrender.com/api/lookups/article-categories" \
  -H "Authorization: Bearer $TOKEN"
```

### Soft Deletes
Delete operations are "soft deletes" - items are marked as deleted but not physically removed from the database.

### ID Generation
- Lookup items: IDs are auto-generated GUIDs
- Currencies: IDs are set to the currency code (e.g., "USD")

### Validation
- Required fields must be provided
- String length limits are enforced
- Currency codes are automatically converted to uppercase

### Error Handling
All endpoints return appropriate HTTP status codes:
- `200` - Success
- `201` - Created
- `400` - Bad Request (validation errors)
- `401` - Unauthorized (missing/invalid token)
- `404` - Not Found
- `500` - Internal Server Error

## 🎯 Quick Test Script

```bash
#!/bin/bash
BASE_URL="https://flowservicebackend.onrender.com"

# Get authentication token
echo "Getting authentication token..."
TOKEN=$(curl -s -X GET "$BASE_URL/api/dev/token" | jq -r '.token')
echo "Token: $TOKEN"

# Test getting article categories
echo -e "\n--- Getting Article Categories ---"
curl -s -X GET "$BASE_URL/api/lookups/article-categories" \
  -H "Authorization: Bearer $TOKEN" | jq '.'

# Test creating new article category
echo -e "\n--- Creating New Article Category ---"
curl -s -X POST "$BASE_URL/api/lookups/article-categories" \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "name": "Test Category",
    "description": "Test category created via API",
    "color": "#6366F1",
    "isActive": true,
    "sortOrder": 99
  }' | jq '.'

# Test getting currencies
echo -e "\n--- Getting Currencies ---"
curl -s -X GET "$BASE_URL/api/lookups/currencies" \
  -H "Authorization: Bearer $TOKEN" | jq '.'

echo -e "\n--- API Test Complete ---"
```

## 📖 Additional Resources

- **Swagger Documentation**: https://flowservicebackend.onrender.com/api-docs
- **Health Check**: https://flowservicebackend.onrender.com/health
- **API Info**: https://flowservicebackend.onrender.com/api/dev/info