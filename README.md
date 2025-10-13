# MyApi - .NET 8 Backend

A comprehensive business management API built with .NET 8, Entity Framework Core, and PostgreSQL.

## 🚀 Features

- **Authentication & Authorization**: JWT-based authentication with role management
- **User Management**: Admin users, regular users, roles, and skills
- **Contact Management**: CRM functionality with tags, notes, and search
- **Article Management**: Materials and services catalog with inventory tracking
- **Calendar System**: Events, appointments, reminders, and attendees
- **Lookup System**: Configurable dropdowns and reference data
- **Multi-tenant Support**: User preferences and customization
- **Comprehensive API**: RESTful endpoints with full CRUD operations

## 🏗️ Technology Stack

- **.NET 8**: Latest C# features and performance improvements
- **ASP.NET Core**: High-performance web API framework
- **Entity Framework Core**: Modern ORM with PostgreSQL provider
- **PostgreSQL**: Robust relational database with JSONB support
- **JWT Authentication**: Secure token-based authentication
- **Swagger/OpenAPI**: Auto-generated API documentation
- **BCrypt**: Secure password hashing

## 📋 Prerequisites

- .NET 8 SDK
- PostgreSQL 12+
- (Optional) Docker for containerized deployment

## 🚀 Getting Started

### 1. Clone the Repository
\`\`\`bash
git clone [your-repo-url]
cd FlowServiceBackend
\`\`\`

### 2. Database Setup

#### Option A: Using Connection String
Update your connection string in `appsettings.Development.json`:
\`\`\`json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=5432;Database=myapi_dev;Username=postgres;Password=your_password"
  }
}
\`\`\`

#### Option B: Using Environment Variables
Set the `DATABASE_URL` environment variable:
\`\`\`bash
export DATABASE_URL="postgresql://username:password@host:port/database"
\`\`\`

### 3. Run Database Migrations
\`\`\`bash
# Create and update database
dotnet ef database update

# Or use the complete recreation script (⚠️ WARNING: Deletes all data!)
psql -d your_database -f Database/complete_database_recreation.sql
\`\`\`

### 4. Start the Application
\`\`\`bash
# Development
dotnet run

# Or with watch for auto-reload
dotnet watch run
\`\`\`

The API will be available at:
- **API**: `https://localhost:7000` or `http://localhost:5000`
- **Swagger Documentation**: `https://localhost:7000/api-docs`
- **Health Check**: `https://localhost:7000/health`

## 📖 API Documentation

### Swagger UI
Visit `/api-docs` for interactive API documentation with:
- Complete endpoint documentation
- Request/response examples
- Built-in testing interface
- JWT authentication support

### Development Tools (Development Only)
- **Dev Token**: `GET /api/dev/token` - 24-hour development token
- **Permanent Token**: `GET /api/dev/permanent-token` - 1-year test token
- **API Info**: `GET /api/dev/info` - Comprehensive API information

### Authentication
All endpoints (except `/api/auth/*` and `/api/dev/*`) require JWT Bearer token:
\`\`\`bash
curl -X GET "https://localhost:7000/api/users" \
  -H "Authorization: Bearer YOUR_JWT_TOKEN"
\`\`\`

## 🗄️ Database Management

### Migrations
\`\`\`bash
# Create new migration
dotnet ef migrations add MigrationName

# Apply migrations
dotnet ef database update

# Remove last migration
dotnet ef migrations remove
\`\`\`

### Database Recreation
For development environments, use the complete recreation script:
\`\`\`bash
psql -d your_database -f Database/complete_database_recreation.sql
\`\`\`

See `Database/README.md` for detailed database documentation.

## 🐳 Docker Deployment

### Build Docker Image
\`\`\`bash
docker build -t myapi .
\`\`\`

### Run with Docker Compose
\`\`\`yaml
version: '3.8'
services:
  api:
    build: .
    ports:
      - "8080:8080"
    environment:
      - DATABASE_URL=postgresql://user:pass@db:5432/myapi
      - ASPNETCORE_ENVIRONMENT=Production
    depends_on:
      - db
  
  db:
    image: postgres:15
    environment:
      - POSTGRES_DB=myapi
      - POSTGRES_USER=user
      - POSTGRES_PASSWORD=pass
    volumes:
      - postgres_data:/var/lib/postgresql/data

volumes:
  postgres_data:
\`\`\`

## ☁️ Cloud Deployment

### Environment Variables
Set these environment variables in your cloud provider:
\`\`\`bash
DATABASE_URL=postgresql://user:password@host:port/database
ASPNETCORE_ENVIRONMENT=Production
JWT_KEY=your-secret-jwt-key
JWT_ISSUER=MyApi
JWT_AUDIENCE=MyApiClients
\`\`\`

### Health Checks
The API includes a health check endpoint at `/health` for monitoring.

## 🔒 Security

- **JWT Authentication**: Secure token-based authentication
- **Password Hashing**: BCrypt with secure salt rounds
- **CORS**: Configurable cross-origin resource sharing
- **Environment-based Configuration**: Sensitive data via environment variables
- **SQL Injection Protection**: Entity Framework parameterized queries

## 📁 Project Structure

\`\`\`
FlowServiceBackend/
├── Controllers/          # API controllers
├── Services/            # Business logic services
├── Models/              # Entity models
├── DTOs/               # Data transfer objects
├── Data/               # Entity Framework context & configurations
├── Configuration/      # App configuration & helpers
├── Migrations/         # Entity Framework migrations
├── Database/           # Database scripts & documentation
└── wwwroot/           # Static files for Swagger UI
\`\`\`

## 🧪 Testing

### Run Tests
\`\`\`bash
dotnet test
\`\`\`

### API Testing
Use the included test script:
\`\`\`bash
chmod +x test_api.sh
./test_api.sh
\`\`\`

## 📝 Contributing

1. Follow the existing code structure and naming conventions
2. Add XML documentation for public APIs
3. Update database migrations when changing models
4. Test your changes using the provided test script
5. Update API documentation if needed

## 📞 Support

### For Issues:
1. Check the API documentation at `/api-docs`
2. Verify database connection and migrations
3. Check application logs for detailed error information
4. Review the `Database/migration_summary.md` for schema details

### Useful Resources:
- **API Documentation**: See `API_DOCUMENTATION.md`
- **Database Schema**: See `Database/migration_summary.md`
- **Health Check**: `GET /health`

## 📄 License

[Add your license information here]

---

**MyApi** - Built with ❤️ using .NET 8 and modern development practices.
