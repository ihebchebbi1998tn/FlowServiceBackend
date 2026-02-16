import React, { useState } from "react";
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "@/components/ui/card";
import { Tabs, TabsContent, TabsList, TabsTrigger } from "@/components/ui/tabs";
import { Badge } from "@/components/ui/badge";
import { ScrollArea } from "@/components/ui/scroll-area";
import { Accordion, AccordionContent, AccordionItem, AccordionTrigger } from "@/components/ui/accordion";
import { Button } from "@/components/ui/button";
import { Separator } from "@/components/ui/separator";
import { 
  Folder, FileCode, Database, Server, Settings, Layers, 
  Code, Terminal, Copy, Check, ArrowRight, Plus, Menu,
  Layout, Navigation, Smartphone, Monitor, Route, Shield,
  Activity, Bug, FileJson, Wrench, Clock, Users, Package
} from "lucide-react";
import { toast } from "sonner";

const CodeBlock = ({ code, language = "typescript" }: { code: string; language?: string }) => {
  const [copied, setCopied] = useState(false);
  
  const handleCopy = () => {
    navigator.clipboard.writeText(code);
    setCopied(true);
    toast.success("Code copied to clipboard");
    setTimeout(() => setCopied(false), 2000);
  };

  return (
    <div className="relative group">
      <pre className="bg-muted/50 border rounded-lg p-4 overflow-x-auto text-sm font-mono whitespace-pre-wrap">
        <code>{code}</code>
      </pre>
      <Button
        size="icon"
        variant="ghost"
        className="absolute top-2 right-2 h-8 w-8 opacity-0 group-hover:opacity-100 transition-opacity"
        onClick={handleCopy}
      >
        {copied ? <Check className="h-4 w-4 text-success" /> : <Copy className="h-4 w-4" />}
      </Button>
    </div>
  );
};

const StepCard = ({ step, title, children }: { step: number; title: string; children: React.ReactNode }) => (
  <Card className="border-l-4 border-l-primary">
    <CardHeader className="pb-2">
      <div className="flex items-center gap-3">
        <div className="w-8 h-8 rounded-full bg-primary text-primary-foreground flex items-center justify-center font-bold text-sm">
          {step}
        </div>
        <CardTitle className="text-lg">{title}</CardTitle>
      </div>
    </CardHeader>
    <CardContent>{children}</CardContent>
  </Card>
);

// Code examples as constants to avoid JSX parsing issues
const folderStructureCode = `src/modules/your-module/
├── components/           # Reusable UI components
│   ├── YourList.tsx     # List/table component
│   ├── YourForm.tsx     # Form dialogs
│   └── YourCard.tsx     # Card components
├── hooks/               # Custom React hooks
│   └── useYourData.ts   # Data fetching hooks
├── pages/               # Page components (routes)
│   ├── YourPage.tsx     # Main list page
│   └── YourDetailPage.tsx # Detail view page
├── services/            # API service functions
│   └── your.service.ts  # API calls
├── types/               # TypeScript interfaces
│   └── index.ts         # Type definitions
├── locale/              # i18n translations (optional)
│   ├── en.json
│   └── fr.json
├── migrations/          # Database schema (optional)
│   └── 001_your_tables.json
├── YourModule.tsx       # Module entry with Routes
└── index.ts             # Public exports`;

const moduleEntryCode = `// src/modules/your-module/YourModule.tsx
import { Routes, Route } from "react-router-dom";
import YourPage from "./pages/YourPage";
import YourDetailPage from "./pages/YourDetailPage";

export function YourModule() {
  return (
    <Routes>
      {/* List view - /dashboard/your-module */}
      <Route index element={<YourPage />} />
      
      {/* Detail view - /dashboard/your-module/:id */}
      <Route path=":id" element={<YourDetailPage />} />
      
      {/* Add more routes as needed */}
      {/* <Route path="add" element={<AddYourItemPage />} /> */}
    </Routes>
  );
}`;

const indexExportCode = `// src/modules/your-module/index.ts
export { YourModule } from "./YourModule";
export * from "./types";
// Export any other public components/hooks`;

const typesCode = `// src/modules/your-module/types/index.ts
export interface YourEntity {
  id: string;
  name: string;
  description?: string;
  status: "active" | "inactive" | "pending";
  created_at: string;
  updated_at: string;
  created_by?: string;
}

export interface CreateYourEntityDto {
  name: string;
  description?: string;
  status?: "active" | "inactive" | "pending";
}

export interface UpdateYourEntityDto {
  name?: string;
  description?: string;
  status?: "active" | "inactive" | "pending";
}

export interface YourEntityFilters {
  search?: string;
  status?: string;
  page?: number;
  limit?: number;
  sort_by?: string;
  sort_order?: "asc" | "desc";
}`;

const serviceCode = `// src/modules/your-module/services/your.service.ts
import apiClient from "@/services/api/apiClient";
import { YourEntity, CreateYourEntityDto, UpdateYourEntityDto, YourEntityFilters } from "../types";

const BASE_URL = "/api/YourEntities";

export const yourService = {
  // Get all with filters
  async getAll(filters?: YourEntityFilters): Promise<YourEntity[]> {
    const params = new URLSearchParams();
    if (filters?.search) params.append("search", filters.search);
    if (filters?.status) params.append("status", filters.status);
    if (filters?.page) params.append("page", filters.page.toString());
    if (filters?.limit) params.append("limit", filters.limit.toString());
    
    const response = await apiClient.get(BASE_URL + "?" + params);
    return response.data;
  },

  // Get by ID
  async getById(id: string): Promise<YourEntity> {
    const response = await apiClient.get(BASE_URL + "/" + id);
    return response.data;
  },

  // Create
  async create(data: CreateYourEntityDto): Promise<YourEntity> {
    const response = await apiClient.post(BASE_URL, data);
    return response.data;
  },

  // Update
  async update(id: string, data: UpdateYourEntityDto): Promise<YourEntity> {
    const response = await apiClient.put(BASE_URL + "/" + id, data);
    return response.data;
  },

  // Delete
  async delete(id: string): Promise<void> {
    await apiClient.delete(BASE_URL + "/" + id);
  }
};`;

const hookCode = `// src/modules/your-module/hooks/useYourData.ts
import { useQuery, useMutation, useQueryClient } from "@tanstack/react-query";
import { yourService } from "../services/your.service";
import { CreateYourEntityDto, UpdateYourEntityDto, YourEntityFilters } from "../types";
import { toast } from "sonner";

export const useYourEntities = (filters?: YourEntityFilters) => {
  return useQuery({
    queryKey: ["your-entities", filters],
    queryFn: () => yourService.getAll(filters),
  });
};

export const useYourEntity = (id: string) => {
  return useQuery({
    queryKey: ["your-entity", id],
    queryFn: () => yourService.getById(id),
    enabled: !!id,
  });
};

export const useCreateYourEntity = () => {
  const queryClient = useQueryClient();
  
  return useMutation({
    mutationFn: (data: CreateYourEntityDto) => yourService.create(data),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["your-entities"] });
      toast.success("Created successfully");
    },
    onError: (error: any) => {
      toast.error(error?.response?.data?.message || "Failed to create");
    },
  });
};

export const useUpdateYourEntity = () => {
  const queryClient = useQueryClient();
  
  return useMutation({
    mutationFn: ({ id, data }: { id: string; data: UpdateYourEntityDto }) => 
      yourService.update(id, data),
    onSuccess: (_, { id }) => {
      queryClient.invalidateQueries({ queryKey: ["your-entities"] });
      queryClient.invalidateQueries({ queryKey: ["your-entity", id] });
      toast.success("Updated successfully");
    },
    onError: (error: any) => {
      toast.error(error?.response?.data?.message || "Failed to update");
    },
  });
};

export const useDeleteYourEntity = () => {
  const queryClient = useQueryClient();
  
  return useMutation({
    mutationFn: (id: string) => yourService.delete(id),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["your-entities"] });
      toast.success("Deleted successfully");
    },
    onError: (error: any) => {
      toast.error(error?.response?.data?.message || "Failed to delete");
    },
  });
};`;

const pageComponentCode = `// src/modules/your-module/pages/YourPage.tsx
import { useState } from "react";
import { useNavigate } from "react-router-dom";
import { useTranslation } from "react-i18next";
import { Plus, Search, Filter } from "lucide-react";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { useYourEntities } from "../hooks/useYourData";
import { YourEntityFilters } from "../types";

export default function YourPage() {
  const { t } = useTranslation();
  const navigate = useNavigate();
  const [filters, setFilters] = useState<YourEntityFilters>({});
  
  const { data: entities, isLoading, error } = useYourEntities(filters);

  if (error) {
    return <div className="p-6">Error loading data</div>;
  }

  return (
    <div className="p-6 space-y-6">
      {/* Header */}
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-2xl font-bold">Your Module</h1>
          <p className="text-muted-foreground">Manage your entities</p>
        </div>
        <Button onClick={() => navigate("add")}>
          <Plus className="h-4 w-4 mr-2" />
          Add New
        </Button>
      </div>

      {/* Filters */}
      <div className="flex gap-4">
        <div className="relative flex-1 max-w-sm">
          <Search className="absolute left-3 top-1/2 -translate-y-1/2 h-4 w-4" />
          <Input
            placeholder="Search..."
            className="pl-10"
            value={filters.search || ""}
            onChange={(e) => setFilters({ ...filters, search: e.target.value })}
          />
        </div>
      </div>

      {/* Content */}
      {isLoading ? (
        <div className="p-6 space-y-4 animate-pulse">
          <div className="h-7 w-48 bg-muted rounded" />
          <div className="grid gap-4 md:grid-cols-2 lg:grid-cols-3">
            {[...Array(6)].map((_, i) => (
              <div key={i} className="h-32 bg-muted/60 rounded-lg" />
            ))}
          </div>
        </div>
      ) : (
        <div className="grid gap-4 md:grid-cols-2 lg:grid-cols-3">
          {entities?.map((entity) => (
            <Card 
              key={entity.id} 
              className="cursor-pointer hover:shadow-md transition-shadow"
              onClick={() => navigate(entity.id)}
            >
              <CardHeader>
                <CardTitle>{entity.name}</CardTitle>
              </CardHeader>
              <CardContent>
                <p className="text-sm text-muted-foreground">{entity.description}</p>
              </CardContent>
            </Card>
          ))}
        </div>
      )}
    </div>
  );
}`;

const registerModuleCode = `// At the top - import your module
import { YourModule } from "@/modules/your-module";
// OR for lazy loading (recommended for large modules):
const YourModule = React.lazy(() => 
  import("@/modules/your-module").then(m => ({ default: m.YourModule }))
);

// Inside the Routes component, add:
<Route path="your-module/*" element={
  <PermissionRoute module="your_module" action="read">
    <Suspense fallback={<TableLoadingPlaceholder />}>
      <YourModule />
    </Suspense>
  </PermissionRoute>
} />`;

const sidebarConfigCode = `// Find the DEFAULT_SIDEBAR_ITEMS array and add your item:

const DEFAULT_SIDEBAR_ITEMS: SidebarItemConfig[] = [
  // ... existing items ...
  
  // Add to appropriate group (workspace, crm, service, or system)
  {
    id: 'your-module',           // Unique identifier
    title: 'your-module',        // Translation key (will use t('your-module'))
    url: '/dashboard/your-module',
    icon: 'YourIcon',            // Icon name from sidebarIcons.ts
    group: 'workspace',          // Group: workspace | crm | service | system
    active: true,
    order: 5,                    // Position within group
    description: 'Manage your entities',
    
    // Optional: Add dropdown sub-items
    dropdown: [
      { title: 'All Items', url: '/dashboard/your-module' },
      { title: 'Add New', url: '/dashboard/your-module/add' }
    ]
  }
];`;

const iconRegistryCode = `// Import your icon from lucide-react
import { YourIcon } from "lucide-react";

// Add to ICON_REGISTRY
export const ICON_REGISTRY = {
  // ... existing icons ...
  YourIcon: YourIcon,
};

// Add to IconName type
export type IconName = 
  // ... existing names ...
  | 'YourIcon';`;

const permissionMapCode = `// Find SIDEBAR_PERMISSION_MAP and add your mapping:

const SIDEBAR_PERMISSION_MAP: Record<string, PermissionModule> = {
  // ... existing mappings ...
  
  'your-module': 'your_module',  // Maps to permission module
};`;

const permissionTypeCode = `// Add to PermissionModule type
export type PermissionModule = 
  | 'contacts'
  | 'sales'
  | 'offers'
  // ... existing modules ...
  | 'your_module';  // Add your module`;

const translationsCode = `// en.json
{
  "your-module": "Your Module",
  "sidebarDescriptions": {
    "your-module": "Manage your entities and data"
  }
}

// fr.json
{
  "your-module": "Votre Module",
  "sidebarDescriptions": {
    "your-module": "Gérer vos entités et données"
  }
}`;

const backendModelCode = `// Modules/YourModule/Models/YourEntity.cs
using MyApi.Modules.Shared.Domain.Common;

namespace MyApi.Modules.YourModule.Models
{
    public class YourEntity : BaseEntityWithSoftDelete
    {
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string Status { get; set; } = "active";
        
        // Foreign keys
        public int? ContactId { get; set; }
        public virtual Contact? Contact { get; set; }
        
        // Navigation properties
        public virtual ICollection<YourEntityItem> Items { get; set; } 
            = new List<YourEntityItem>();
    }
}`;

const backendDtosCode = `// Modules/YourModule/DTOs/YourEntityDto.cs
namespace MyApi.Modules.YourModule.DTOs
{
    public record YourEntityDto(
        int Id,
        string Name,
        string? Description,
        string Status,
        DateTime CreatedAt,
        DateTime? UpdatedAt
    );

    public record CreateYourEntityDto(
        string Name,
        string? Description,
        string Status = "active"
    );

    public record UpdateYourEntityDto(
        string? Name,
        string? Description,
        string? Status
    );
}`;

const backendServiceInterfaceCode = `// Modules/YourModule/Services/IYourEntityService.cs
using MyApi.Modules.YourModule.DTOs;

namespace MyApi.Modules.YourModule.Services
{
    public interface IYourEntityService
    {
        Task<IEnumerable<YourEntityDto>> GetAllAsync(
            string? search = null, 
            string? status = null,
            int page = 1, 
            int limit = 20);
        Task<YourEntityDto?> GetByIdAsync(int id);
        Task<YourEntityDto> CreateAsync(CreateYourEntityDto dto, string userId);
        Task<YourEntityDto?> UpdateAsync(int id, UpdateYourEntityDto dto, string userId);
        Task<bool> DeleteAsync(int id, string userId);
    }
}`;

const backendServiceImplCode = `// Modules/YourModule/Services/YourEntityService.cs
using Microsoft.EntityFrameworkCore;
using MyApi.Data;
using MyApi.Modules.YourModule.DTOs;
using MyApi.Modules.YourModule.Models;

namespace MyApi.Modules.YourModule.Services
{
    public class YourEntityService : IYourEntityService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<YourEntityService> _logger;

        public YourEntityService(
            ApplicationDbContext context,
            ILogger<YourEntityService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<IEnumerable<YourEntityDto>> GetAllAsync(
            string? search, string? status, int page, int limit)
        {
            var query = _context.YourEntities
                .Where(e => !e.IsDeleted)
                .AsQueryable();

            if (!string.IsNullOrEmpty(search))
            {
                query = query.Where(e => 
                    e.Name.Contains(search) || 
                    (e.Description != null && e.Description.Contains(search)));
            }

            if (!string.IsNullOrEmpty(status))
            {
                query = query.Where(e => e.Status == status);
            }

            return await query
                .OrderByDescending(e => e.CreatedAt)
                .Skip((page - 1) * limit)
                .Take(limit)
                .Select(e => new YourEntityDto(
                    e.Id, e.Name, e.Description, e.Status,
                    e.CreatedAt, e.UpdatedAt
                ))
                .ToListAsync();
        }

        public async Task<YourEntityDto?> GetByIdAsync(int id)
        {
            var entity = await _context.YourEntities
                .Where(e => e.Id == id && !e.IsDeleted)
                .FirstOrDefaultAsync();

            return entity == null ? null : new YourEntityDto(
                entity.Id, entity.Name, entity.Description, 
                entity.Status, entity.CreatedAt, entity.UpdatedAt);
        }

        public async Task<YourEntityDto> CreateAsync(
            CreateYourEntityDto dto, string userId)
        {
            var entity = new YourEntity
            {
                Name = dto.Name,
                Description = dto.Description,
                Status = dto.Status,
                CreatedBy = userId,
                CreatedAt = DateTime.UtcNow
            };

            _context.YourEntities.Add(entity);
            await _context.SaveChangesAsync();

            _logger.LogInformation(
                "YourEntity {Id} created by user {UserId}", 
                entity.Id, userId);

            return new YourEntityDto(
                entity.Id, entity.Name, entity.Description,
                entity.Status, entity.CreatedAt, entity.UpdatedAt);
        }

        public async Task<YourEntityDto?> UpdateAsync(
            int id, UpdateYourEntityDto dto, string userId)
        {
            var entity = await _context.YourEntities
                .FirstOrDefaultAsync(e => e.Id == id && !e.IsDeleted);

            if (entity == null) return null;

            if (dto.Name != null) entity.Name = dto.Name;
            if (dto.Description != null) entity.Description = dto.Description;
            if (dto.Status != null) entity.Status = dto.Status;
            
            entity.UpdatedAt = DateTime.UtcNow;
            entity.ModifiedBy = userId;

            await _context.SaveChangesAsync();

            return new YourEntityDto(
                entity.Id, entity.Name, entity.Description,
                entity.Status, entity.CreatedAt, entity.UpdatedAt);
        }

        public async Task<bool> DeleteAsync(int id, string userId)
        {
            var entity = await _context.YourEntities
                .FirstOrDefaultAsync(e => e.Id == id && !e.IsDeleted);

            if (entity == null) return false;

            // Soft delete
            entity.IsDeleted = true;
            entity.DeletedAt = DateTime.UtcNow;
            entity.DeletedBy = userId;

            await _context.SaveChangesAsync();

            _logger.LogInformation(
                "YourEntity {Id} deleted by user {UserId}", 
                id, userId);

            return true;
        }
    }
}`;

const backendControllerCode = `// Modules/YourModule/Controllers/YourEntitiesController.cs
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MyApi.Modules.YourModule.DTOs;
using MyApi.Modules.YourModule.Services;
using System.Security.Claims;

namespace MyApi.Modules.YourModule.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class YourEntitiesController : ControllerBase
    {
        private readonly IYourEntityService _service;

        public YourEntitiesController(IYourEntityService service)
        {
            _service = service;
        }

        private string GetUserId() => 
            User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "";

        [HttpGet]
        public async Task<ActionResult<IEnumerable<YourEntityDto>>> GetAll(
            [FromQuery] string? search,
            [FromQuery] string? status,
            [FromQuery] int page = 1,
            [FromQuery] int limit = 20)
        {
            var result = await _service.GetAllAsync(search, status, page, limit);
            return Ok(result);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<YourEntityDto>> GetById(int id)
        {
            var result = await _service.GetByIdAsync(id);
            if (result == null) 
                return NotFound(new { message = "Entity not found" });
            return Ok(result);
        }

        [HttpPost]
        public async Task<ActionResult<YourEntityDto>> Create(
            [FromBody] CreateYourEntityDto dto)
        {
            var result = await _service.CreateAsync(dto, GetUserId());
            return CreatedAtAction(
                nameof(GetById), 
                new { id = result.Id }, 
                result);
        }

        [HttpPut("{id}")]
        public async Task<ActionResult<YourEntityDto>> Update(
            int id, 
            [FromBody] UpdateYourEntityDto dto)
        {
            var result = await _service.UpdateAsync(id, dto, GetUserId());
            if (result == null) 
                return NotFound(new { message = "Entity not found" });
            return Ok(result);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var success = await _service.DeleteAsync(id, GetUserId());
            if (!success) 
                return NotFound(new { message = "Entity not found" });
            return NoContent();
        }
    }
}`;

const registerServiceCode = `// In Program.cs, add to service registration:

// Your Module Services
builder.Services.AddScoped<IYourEntityService, YourEntityService>();`;

const dbContextCode = `// In Data/ApplicationDbContext.cs

public class ApplicationDbContext : DbContext
{
    // ... existing DbSets ...
    
    public DbSet<YourEntity> YourEntities { get; set; }
    
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // ... existing configurations ...
        
        // Configure YourEntity
        modelBuilder.Entity<YourEntity>(entity =>
        {
            entity.ToTable("your_entities");
            entity.HasIndex(e => e.Name);
            entity.HasIndex(e => e.Status);
            entity.HasIndex(e => e.IsDeleted);
        });
    }
}`;

const migrationJsonCode = `{
  "module": "your_module",
  "version": "001",
  "description": "Your module database tables",
  "tables": [
    {
      "name": "CRM.your_entities",
      "primaryKey": "id",
      "fields": {
        "id": {
          "type": "integer",
          "autoIncrement": true
        },
        "name": {
          "type": "string",
          "required": true,
          "maxLength": 255
        },
        "description": {
          "type": "string",
          "maxLength": 1000
        },
        "status": {
          "type": "string",
          "required": true,
          "default": "active",
          "enum": ["active", "inactive", "pending"]
        },
        "contact_id": {
          "type": "integer",
          "foreignKey": "CRM.contacts.id"
        },
        "created_at": {
          "type": "datetime",
          "required": true,
          "default": "CURRENT_TIMESTAMP"
        },
        "updated_at": {
          "type": "datetime"
        },
        "created_by": {
          "type": "string",
          "foreignKey": "SYS.users.id"
        },
        "is_deleted": {
          "type": "boolean",
          "default": false
        }
      },
      "indexes": ["name", "status", "contact_id", "is_deleted"]
    }
  ]
}`;

const efMigrationCode = `# Generate migration
dotnet ef migrations add AddYourEntitiesTable

# Apply migration to database
dotnet ef database update

# Or generate SQL script for manual review
dotnet ef migrations script --output Migrations/Scripts/AddYourEntitiesTable.sql`;

const sqlMigrationCode = `-- Migrations/Scripts/001_create_your_entities.sql

CREATE TABLE CRM.your_entities (
    id INT IDENTITY(1,1) PRIMARY KEY,
    name NVARCHAR(255) NOT NULL,
    description NVARCHAR(MAX) NULL,
    status NVARCHAR(50) NOT NULL DEFAULT 'active',
    contact_id INT NULL,
    created_at DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    updated_at DATETIME2 NULL,
    created_by NVARCHAR(50) NULL,
    modified_by NVARCHAR(50) NULL,
    is_deleted BIT NOT NULL DEFAULT 0,
    deleted_at DATETIME2 NULL,
    deleted_by NVARCHAR(50) NULL,
    
    CONSTRAINT FK_YourEntities_Contact 
        FOREIGN KEY (contact_id) REFERENCES CRM.contacts(id)
);

-- Create indexes
CREATE INDEX IX_YourEntities_Name ON CRM.your_entities(name);
CREATE INDEX IX_YourEntities_Status ON CRM.your_entities(status);
CREATE INDEX IX_YourEntities_IsDeleted ON CRM.your_entities(is_deleted);

-- Create trigger for updated_at
CREATE TRIGGER TR_YourEntities_UpdatedAt
ON CRM.your_entities
AFTER UPDATE
AS
BEGIN
    UPDATE CRM.your_entities
    SET updated_at = GETUTCDATE()
    FROM CRM.your_entities t
    INNER JOIN inserted i ON t.id = i.id;
END;`;

const lookupValuesCode = `-- Add lookup values for your module
INSERT INTO LU.lookup_values (lookup_type, code, name, display_order, is_active)
VALUES 
    ('your_entity_status', 'active', 'Active', 1, 1),
    ('your_entity_status', 'inactive', 'Inactive', 2, 1),
    ('your_entity_status', 'pending', 'Pending', 3, 1);`;

const frontendLoggingCode = `// In your component
import { useLogger } from "@/hooks/useLogger";

export function YourComponent() {
  const logger = useLogger("YourModule");

  const handleAction = async () => {
    logger.info("Starting action", { entityId: 123 });
    
    try {
      await performAction();
      logger.info("Action completed successfully");
    } catch (error) {
      logger.error("Action failed", { error });
    }
  };

  return <Button onClick={handleAction}>Do Action</Button>;
}`;

const backendLoggingCode = `// In your service, inject ILogger<T>
public class YourEntityService : IYourEntityService
{
    private readonly ILogger<YourEntityService> _logger;

    public YourEntityService(ILogger<YourEntityService> logger)
    {
        _logger = logger;
    }

    public async Task<YourEntityDto> CreateAsync(CreateYourEntityDto dto, string userId)
    {
        _logger.LogInformation(
            "Creating YourEntity. Name: {Name}, UserId: {UserId}", 
            dto.Name, userId);

        try
        {
            var entity = new YourEntity { /* ... */ };
            await _context.SaveChangesAsync();

            _logger.LogInformation(
                "YourEntity created successfully. Id: {Id}, UserId: {UserId}",
                entity.Id, userId);

            return MapToDto(entity);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, 
                "Failed to create YourEntity. Name: {Name}, UserId: {UserId}",
                dto.Name, userId);
            throw;
        }
    }
}`;

const auditTrailCode = `// Create an audit service or use existing one
public interface IAuditService
{
    Task LogAsync(
        string action,
        string entityType,
        string entityId,
        string userId,
        object? oldValue = null,
        object? newValue = null);
}

// Usage in your service
public async Task<YourEntityDto> UpdateAsync(int id, UpdateYourEntityDto dto, string userId)
{
    var entity = await _context.YourEntities.FindAsync(id);
    var oldValue = new { entity.Name, entity.Status };
    
    // Apply updates...
    entity.Name = dto.Name ?? entity.Name;
    
    await _context.SaveChangesAsync();

    // Log to audit trail
    await _auditService.LogAsync(
        action: "UPDATE",
        entityType: "YourEntity",
        entityId: id.ToString(),
        userId: userId,
        oldValue: oldValue,
        newValue: new { entity.Name, entity.Status }
    );

    return MapToDto(entity);
}`;

const activityLoggingCode = `// Model for activities
public class YourEntityActivity
{
    public int Id { get; set; }
    public int YourEntityId { get; set; }
    public string Action { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string? OldValue { get; set; }
    public string? NewValue { get; set; }
    public string CreatedBy { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}

// In your service
private async Task LogActivityAsync(
    int entityId, 
    string action, 
    string description, 
    string userId,
    string? oldValue = null,
    string? newValue = null)
{
    var activity = new YourEntityActivity
    {
        YourEntityId = entityId,
        Action = action,
        Description = description,
        OldValue = oldValue,
        NewValue = newValue,
        CreatedBy = userId,
        CreatedAt = DateTime.UtcNow
    };
    
    _context.YourEntityActivities.Add(activity);
    await _context.SaveChangesAsync();
}`;

const viewLogsCode = `// Frontend: Call logs API to view
import { logsApi } from "@/services/api/logsApi";

// Get logs filtered by module
const logs = await logsApi.getLogs({
  module: "your-module",
  level: "error", // or "info", "warn", "debug"
  from: "2024-01-01",
  to: "2024-12-31"
});`;

export default function DeveloperOnboardingGuide() {
  return (
    <div className="space-y-6">
      {/* Introduction */}
      <Card>
        <CardHeader>
          <div className="flex items-center gap-2">
            <Users className="h-6 w-6 text-primary" />
            <CardTitle>Developer Onboarding Guide</CardTitle>
          </div>
          <CardDescription>
            Complete step-by-step guide for new developers joining the FlowService project.
            Learn how to create new modules, add navigation, implement backend APIs, and set up database tables.
          </CardDescription>
        </CardHeader>
      </Card>

      <Tabs defaultValue="frontend" className="space-y-4">
        <TabsList className="grid w-full grid-cols-5">
          <TabsTrigger value="frontend" className="flex items-center gap-2">
            <Layout className="h-4 w-4" />
            <span className="hidden md:inline">Frontend Module</span>
          </TabsTrigger>
          <TabsTrigger value="navigation" className="flex items-center gap-2">
            <Navigation className="h-4 w-4" />
            <span className="hidden md:inline">Navigation</span>
          </TabsTrigger>
          <TabsTrigger value="backend" className="flex items-center gap-2">
            <Server className="h-4 w-4" />
            <span className="hidden md:inline">Backend API</span>
          </TabsTrigger>
          <TabsTrigger value="database" className="flex items-center gap-2">
            <Database className="h-4 w-4" />
            <span className="hidden md:inline">Database</span>
          </TabsTrigger>
          <TabsTrigger value="logging" className="flex items-center gap-2">
            <Activity className="h-4 w-4" />
            <span className="hidden md:inline">Logging</span>
          </TabsTrigger>
        </TabsList>

        {/* ==================== FRONTEND MODULE TAB ==================== */}
        <TabsContent value="frontend" className="space-y-4">
          <Card>
            <CardHeader>
              <CardTitle className="flex items-center gap-2">
                <Folder className="h-5 w-5" />
                Creating a New Frontend Module
              </CardTitle>
              <CardDescription>
                Follow these steps to create a new feature module with proper structure, routing, and components.
              </CardDescription>
            </CardHeader>
            <CardContent className="space-y-6">
              <StepCard step={1} title="Create Module Folder Structure">
                <p className="text-sm text-muted-foreground mb-4">
                  Create a new folder under <code className="bg-muted px-1 rounded">src/modules/</code> with the following structure:
                </p>
                <CodeBlock code={folderStructureCode} />
              </StepCard>

              <StepCard step={2} title="Create Module Entry Component (YourModule.tsx)">
                <p className="text-sm text-muted-foreground mb-4">
                  This component defines all routes within your module using React Router:
                </p>
                <CodeBlock code={moduleEntryCode} />
              </StepCard>

              <StepCard step={3} title="Create Public Export (index.ts)">
                <CodeBlock code={indexExportCode} />
              </StepCard>

              <StepCard step={4} title="Define TypeScript Types">
                <CodeBlock code={typesCode} />
              </StepCard>

              <StepCard step={5} title="Create API Service">
                <CodeBlock code={serviceCode} />
              </StepCard>

              <StepCard step={6} title="Create Data Fetching Hook">
                <CodeBlock code={hookCode} />
              </StepCard>

              <StepCard step={7} title="Create Main Page Component">
                <CodeBlock code={pageComponentCode} />
              </StepCard>

              <StepCard step={8} title="Register Module in DashboardContent.tsx">
                <p className="text-sm text-muted-foreground mb-4">
                  Add your module to the dashboard routing in <code className="bg-muted px-1 rounded">src/modules/dashboard/components/DashboardContent.tsx</code>:
                </p>
                <CodeBlock code={registerModuleCode} />
              </StepCard>
            </CardContent>
          </Card>
        </TabsContent>

        {/* ==================== NAVIGATION TAB ==================== */}
        <TabsContent value="navigation" className="space-y-4">
          <Card>
            <CardHeader>
              <CardTitle className="flex items-center gap-2">
                <Menu className="h-5 w-5" />
                Adding Module to Navigation (Sidebar & Mobile)
              </CardTitle>
              <CardDescription>
                The navigation is dynamically configured via the sidebar service. Here's how to add your module.
              </CardDescription>
            </CardHeader>
            <CardContent className="space-y-6">
              <StepCard step={1} title="Update Sidebar Default Configuration">
                <p className="text-sm text-muted-foreground mb-4">
                  Edit <code className="bg-muted px-1 rounded">src/modules/dashboard/services/sidebar.service.ts</code> to add your module to the default items:
                </p>
                <CodeBlock code={sidebarConfigCode} />
              </StepCard>

              <StepCard step={2} title="Register Icon in Icon Registry">
                <p className="text-sm text-muted-foreground mb-4">
                  Add your icon to <code className="bg-muted px-1 rounded">src/modules/dashboard/components/sidebarIcons.ts</code>:
                </p>
                <CodeBlock code={iconRegistryCode} />
              </StepCard>

              <StepCard step={3} title="Add Permission Mapping">
                <p className="text-sm text-muted-foreground mb-4">
                  Map your module title to a permission in <code className="bg-muted px-1 rounded">src/modules/dashboard/components/AppSidebar.tsx</code> AND <code className="bg-muted px-1 rounded">src/components/navigation/TopNavigation.tsx</code>:
                </p>
                <CodeBlock code={permissionMapCode} />
              </StepCard>

              <StepCard step={4} title="Add Permission Module Type">
                <p className="text-sm text-muted-foreground mb-4">
                  Add your permission module to <code className="bg-muted px-1 rounded">src/types/permissions.ts</code>:
                </p>
                <CodeBlock code={permissionTypeCode} />
              </StepCard>

              <StepCard step={5} title="Add Navigation Translations">
                <p className="text-sm text-muted-foreground mb-4">
                  Add translations in <code className="bg-muted px-1 rounded">src/locales/en.json</code> and <code className="bg-muted px-1 rounded">src/locales/fr.json</code>:
                </p>
                <CodeBlock code={translationsCode} />
              </StepCard>

              <Card className="bg-primary/5 border-primary/20">
                <CardContent className="pt-4">
                  <div className="flex items-start gap-3">
                    <Smartphone className="h-5 w-5 text-primary mt-0.5" />
                    <div>
                      <h4 className="font-semibold text-primary">Mobile Navigation</h4>
                      <p className="text-sm text-muted-foreground mt-1">
                        The mobile navigation in <code className="bg-muted px-1 rounded">TopNavigation.tsx</code> 
                        automatically reads from the same sidebar service configuration. No separate mobile setup needed!
                        Just ensure your item is added to <code>sidebar.service.ts</code>.
                      </p>
                    </div>
                  </div>
                </CardContent>
              </Card>

              <Card className="bg-warning/5 border-warning/20">
                <CardContent className="pt-4">
                  <div className="flex items-start gap-3">
                    <Bug className="h-5 w-5 text-warning mt-0.5" />
                    <div>
                      <h4 className="font-semibold text-warning">Troubleshooting: Item Not Showing?</h4>
                      <p className="text-sm text-muted-foreground mt-1">
                        The sidebar config is cached in localStorage. To force a refresh:<br />
                        1. Open browser DevTools → Application → Local Storage<br />
                        2. Delete <code className="bg-muted px-1 rounded">app-sidebar-config</code><br />
                        3. Refresh the page
                      </p>
                    </div>
                  </div>
                </CardContent>
              </Card>
            </CardContent>
          </Card>
        </TabsContent>

        {/* ==================== BACKEND TAB ==================== */}
        <TabsContent value="backend" className="space-y-4">
          <Card>
            <CardHeader>
              <CardTitle className="flex items-center gap-2">
                <Server className="h-5 w-5" />
                Creating Backend API (.NET 8)
              </CardTitle>
              <CardDescription>
                Follow the existing module patterns to create a new backend API endpoint.
              </CardDescription>
            </CardHeader>
            <CardContent className="space-y-6">
              <StepCard step={1} title="Create Model Entity">
                <p className="text-sm text-muted-foreground mb-4">
                  Create your entity in <code className="bg-muted px-1 rounded">Modules/YourModule/Models/</code>:
                </p>
                <CodeBlock language="csharp" code={backendModelCode} />
              </StepCard>

              <StepCard step={2} title="Create DTOs (Data Transfer Objects)">
                <CodeBlock language="csharp" code={backendDtosCode} />
              </StepCard>

              <StepCard step={3} title="Create Service Interface">
                <CodeBlock language="csharp" code={backendServiceInterfaceCode} />
              </StepCard>

              <StepCard step={4} title="Implement Service">
                <CodeBlock language="csharp" code={backendServiceImplCode} />
              </StepCard>

              <StepCard step={5} title="Create Controller">
                <CodeBlock language="csharp" code={backendControllerCode} />
              </StepCard>

              <StepCard step={6} title="Register Service in Program.cs">
                <CodeBlock language="csharp" code={registerServiceCode} />
              </StepCard>

              <StepCard step={7} title="Register DbSet in ApplicationDbContext">
                <CodeBlock language="csharp" code={dbContextCode} />
              </StepCard>
            </CardContent>
          </Card>
        </TabsContent>

        {/* ==================== DATABASE TAB ==================== */}
        <TabsContent value="database" className="space-y-4">
          <Card>
            <CardHeader>
              <CardTitle className="flex items-center gap-2">
                <Database className="h-5 w-5" />
                Creating Database Tables
              </CardTitle>
              <CardDescription>
                Follow our naming conventions and create migrations for your new module.
              </CardDescription>
            </CardHeader>
            <CardContent className="space-y-6">

              <StepCard step={1} title="Create Migration JSON Schema">
                <p className="text-sm text-muted-foreground mb-4">
                  Create <code className="bg-muted px-1 rounded">src/modules/your-module/migrations/001_your_tables.json</code>:
                </p>
                <CodeBlock code={migrationJsonCode} />
              </StepCard>

              <StepCard step={2} title="Create SQL Migration (Entity Framework)">
                <p className="text-sm text-muted-foreground mb-4">
                  Generate and apply migration using EF Core CLI:
                </p>
                <CodeBlock code={efMigrationCode} />
              </StepCard>

              <StepCard step={3} title="Raw SQL Migration (Alternative)">
                <CodeBlock code={sqlMigrationCode} />
              </StepCard>

              <StepCard step={4} title="Add Lookup Values (If Needed)">
                <CodeBlock code={lookupValuesCode} />
              </StepCard>

              <Card className="bg-warning/5 border-warning/20">
                <CardContent className="pt-4">
                  <div className="flex items-start gap-3">
                    <Shield className="h-5 w-5 text-warning mt-0.5" />
                    <div>
                      <h4 className="font-semibold text-warning">Don't Forget Row-Level Security!</h4>
                      <p className="text-sm text-muted-foreground mt-1">
                        If using Supabase/PostgreSQL, add RLS policies to protect data access.
                        Ensure users can only access their own data based on organization/tenant.
                      </p>
                    </div>
                  </div>
                </CardContent>
              </Card>
            </CardContent>
          </Card>
        </TabsContent>

        {/* ==================== LOGGING TAB ==================== */}
        <TabsContent value="logging" className="space-y-4">
          <Card>
            <CardHeader>
              <CardTitle className="flex items-center gap-2">
                <Activity className="h-5 w-5" />
                Adding Logging & Audit Trail
              </CardTitle>
              <CardDescription>
                Implement proper logging for debugging and audit compliance.
              </CardDescription>
            </CardHeader>
            <CardContent className="space-y-6">
              <StepCard step={1} title="Frontend Logging Hook">
                <p className="text-sm text-muted-foreground mb-4">
                  Use the existing <code className="bg-muted px-1 rounded">useLogger</code> hook:
                </p>
                <CodeBlock code={frontendLoggingCode} />
              </StepCard>

              <StepCard step={2} title="Backend Logging (Serilog)">
                <CodeBlock language="csharp" code={backendLoggingCode} />
              </StepCard>

              <StepCard step={3} title="Audit Trail Integration">
                <p className="text-sm text-muted-foreground mb-4">
                  Log important actions to the audit trail table:
                </p>
                <CodeBlock language="csharp" code={auditTrailCode} />
              </StepCard>

              <StepCard step={4} title="Activity Logging Pattern">
                <p className="text-sm text-muted-foreground mb-4">
                  Create activity records for user-facing history (like in Sales/Offers):
                </p>
                <CodeBlock language="csharp" code={activityLoggingCode} />
              </StepCard>

              <StepCard step={5} title="View Logs in Settings">
                <p className="text-sm text-muted-foreground mb-4">
                  System logs can be viewed at <code className="bg-muted px-1 rounded">/dashboard/settings/logs</code>.
                  To add your module's logs to the system:
                </p>
                <CodeBlock code={viewLogsCode} />
              </StepCard>

              <Card className="bg-success/5 border-success/20">
                <CardContent className="pt-4">
                  <div className="flex items-start gap-3">
                    <Check className="h-5 w-5 text-success mt-0.5" />
                    <div>
                      <h4 className="font-semibold text-success">Logging Best Practices</h4>
                      <ul className="text-sm text-muted-foreground mt-1 space-y-1">
                        <li>• Log at appropriate levels: DEBUG, INFO, WARN, ERROR</li>
                        <li>• Include contextual data (userId, entityId, action)</li>
                        <li>• Never log sensitive data (passwords, tokens, PII)</li>
                        <li>• Use structured logging for easy querying</li>
                        <li>• Log both success and failure cases</li>
                      </ul>
                    </div>
                  </div>
                </CardContent>
              </Card>
            </CardContent>
          </Card>
        </TabsContent>
      </Tabs>

      {/* Quick Reference Summary */}
      <Card>
        <CardHeader>
          <CardTitle className="flex items-center gap-2">
            <Package className="h-5 w-5" />
            Quick Reference: New Module Checklist
          </CardTitle>
        </CardHeader>
        <CardContent>
          <div className="grid md:grid-cols-2 gap-6">
            <div>
              <h4 className="font-semibold mb-3 flex items-center gap-2">
                <Layout className="h-4 w-4" /> Frontend Tasks
              </h4>
              <ul className="space-y-2 text-sm">
                <li className="flex items-center gap-2">
                  <div className="w-5 h-5 rounded border flex items-center justify-center text-xs">□</div>
                  Create folder structure under <code className="bg-muted px-1 rounded">src/modules/</code>
                </li>
                <li className="flex items-center gap-2">
                  <div className="w-5 h-5 rounded border flex items-center justify-center text-xs">□</div>
                  Define TypeScript types/interfaces
                </li>
                <li className="flex items-center gap-2">
                  <div className="w-5 h-5 rounded border flex items-center justify-center text-xs">□</div>
                  Create API service with CRUD operations
                </li>
                <li className="flex items-center gap-2">
                  <div className="w-5 h-5 rounded border flex items-center justify-center text-xs">□</div>
                  Create React Query hooks for data fetching
                </li>
                <li className="flex items-center gap-2">
                  <div className="w-5 h-5 rounded border flex items-center justify-center text-xs">□</div>
                  Build page and component files
                </li>
                <li className="flex items-center gap-2">
                  <div className="w-5 h-5 rounded border flex items-center justify-center text-xs">□</div>
                  Create module entry with Routes
                </li>
                <li className="flex items-center gap-2">
                  <div className="w-5 h-5 rounded border flex items-center justify-center text-xs">□</div>
                  Register in DashboardContent.tsx
                </li>
                <li className="flex items-center gap-2">
                  <div className="w-5 h-5 rounded border flex items-center justify-center text-xs">□</div>
                  Add sidebar configuration
                </li>
                <li className="flex items-center gap-2">
                  <div className="w-5 h-5 rounded border flex items-center justify-center text-xs">□</div>
                  Add permission mapping
                </li>
                <li className="flex items-center gap-2">
                  <div className="w-5 h-5 rounded border flex items-center justify-center text-xs">□</div>
                  Add translations (en.json, fr.json)
                </li>
              </ul>
            </div>
            <div>
              <h4 className="font-semibold mb-3 flex items-center gap-2">
                <Server className="h-4 w-4" /> Backend Tasks
              </h4>
              <ul className="space-y-2 text-sm">
                <li className="flex items-center gap-2">
                  <div className="w-5 h-5 rounded border flex items-center justify-center text-xs">□</div>
                  Create Model entity (inheriting BaseEntity)
                </li>
                <li className="flex items-center gap-2">
                  <div className="w-5 h-5 rounded border flex items-center justify-center text-xs">□</div>
                  Create DTOs for API contracts
                </li>
                <li className="flex items-center gap-2">
                  <div className="w-5 h-5 rounded border flex items-center justify-center text-xs">□</div>
                  Create Service interface
                </li>
                <li className="flex items-center gap-2">
                  <div className="w-5 h-5 rounded border flex items-center justify-center text-xs">□</div>
                  Implement Service with business logic
                </li>
                <li className="flex items-center gap-2">
                  <div className="w-5 h-5 rounded border flex items-center justify-center text-xs">□</div>
                  Create Controller with REST endpoints
                </li>
                <li className="flex items-center gap-2">
                  <div className="w-5 h-5 rounded border flex items-center justify-center text-xs">□</div>
                  Register DbSet in ApplicationDbContext
                </li>
                <li className="flex items-center gap-2">
                  <div className="w-5 h-5 rounded border flex items-center justify-center text-xs">□</div>
                  Register Service in Program.cs
                </li>
                <li className="flex items-center gap-2">
                  <div className="w-5 h-5 rounded border flex items-center justify-center text-xs">□</div>
                  Create and run database migration
                </li>
                <li className="flex items-center gap-2">
                  <div className="w-5 h-5 rounded border flex items-center justify-center text-xs">□</div>
                  Add indexes for frequently queried fields
                </li>
                <li className="flex items-center gap-2">
                  <div className="w-5 h-5 rounded border flex items-center justify-center text-xs">□</div>
                  Implement logging and audit trail
                </li>
              </ul>
            </div>
          </div>
        </CardContent>
      </Card>
    </div>
  );
}
