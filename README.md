# MyAPI - .NET 8 Backend

## Render Deployment Instructions

### 1. Database Setup (MySQL)
Create a MySQL database on Render or use an external MySQL service.

### 2. Environment Variables on Render
Set these environment variables in your Render service:

```
MYSQL_HOST=your-mysql-host
MYSQL_PORT=3306
MYSQL_DATABASE=your-database-name
MYSQL_USER=your-mysql-user
MYSQL_PASSWORD=your-mysql-password
ASPNETCORE_ENVIRONMENT=Production
```

### 3. Render Service Configuration
- **Build Command**: `dotnet publish -c Release -o out`
- **Start Command**: `dotnet out/MyApi.dll`
- **Health Check URL**: `/health`

### 4. Local Development

#### Prerequisites
- .NET 8 SDK
- MySQL Server

#### Setup
1. Update connection string in `appsettings.Development.json`
2. Run migrations: `dotnet ef database update`
3. Start the API: `dotnet run`

#### Database Migrations
```bash
# Create a new migration
dotnet ef migrations add MigrationName

# Update database
dotnet ef database update

# Remove last migration
dotnet ef migrations remove
```

### 5. API Endpoints
- Swagger UI: `/swagger` (available in both dev and production)
- Health Check: `/health`
- Weather Forecast: `/api/weatherforecast`

### 6. Frontend Integration
Update your React app to call the API using the Render URL:
```typescript
const API_BASE_URL = 'https://your-api-name.onrender.com/api';
```

### 7. CORS Configuration
Update the frontend domain in `Program.cs`:
```csharp
policy.WithOrigins("https://your-frontend-domain.com")
```

### Notes
- Database migrations run automatically on startup
- Soft delete is implemented via `IsDeleted` property
- Base entity includes audit fields (CreatedAt, UpdatedAt)
- Health check endpoint for monitoring# FlowServiceBackend
