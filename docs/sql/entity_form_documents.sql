-- ============================================
-- Entity Form Documents - hBackend SQL Script
-- For PostgreSQL / .NET Core Backend
-- ============================================

-- ===========================================
-- 1. CREATE TABLE: EntityFormDocuments
-- ===========================================
CREATE TABLE IF NOT EXISTS "EntityFormDocuments" (
    "Id" SERIAL PRIMARY KEY,
    "EntityType" VARCHAR(50) NOT NULL,           -- 'offer' or 'sale'
    "EntityId" INTEGER NOT NULL,                 -- FK to Offers.Id or Sales.Id
    "FormId" INTEGER NOT NULL,                   -- FK to DynamicForms.Id
    "FormVersion" INTEGER NOT NULL DEFAULT 1,   -- Version at time of creation
    "Title" VARCHAR(200) NULL,                   -- Optional custom title
    "Status" VARCHAR(20) NOT NULL DEFAULT 'draft', -- 'draft' or 'completed'
    "Responses" JSONB NOT NULL DEFAULT '{}',     -- Form field values
    
    -- Audit fields (matching BaseEntity pattern)
    "CreatedAt" TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    "UpdatedAt" TIMESTAMP NULL,
    "CreatedBy" VARCHAR(100) NULL,
    "ModifiedBy" VARCHAR(100) NULL,
    "IsDeleted" BOOLEAN NOT NULL DEFAULT FALSE,
    "DeletedAt" TIMESTAMP NULL,
    "DeletedBy" VARCHAR(100) NULL,
    
    -- Constraints
    CONSTRAINT "CK_EntityFormDocuments_EntityType" CHECK ("EntityType" IN ('offer', 'sale')),
    CONSTRAINT "CK_EntityFormDocuments_Status" CHECK ("Status" IN ('draft', 'completed')),
    CONSTRAINT "FK_EntityFormDocuments_DynamicForms" FOREIGN KEY ("FormId") 
        REFERENCES "DynamicForms" ("Id") ON DELETE RESTRICT
);

-- Add comment to table
COMMENT ON TABLE "EntityFormDocuments" IS 'Stores dynamic form documents attached to offers and sales';

-- ===========================================
-- 2. CREATE INDEXES
-- ===========================================
CREATE INDEX IF NOT EXISTS "IX_EntityFormDocuments_EntityType_EntityId" 
    ON "EntityFormDocuments" ("EntityType", "EntityId") 
    WHERE "IsDeleted" = FALSE;

CREATE INDEX IF NOT EXISTS "IX_EntityFormDocuments_FormId" 
    ON "EntityFormDocuments" ("FormId");

CREATE INDEX IF NOT EXISTS "IX_EntityFormDocuments_Status" 
    ON "EntityFormDocuments" ("Status") 
    WHERE "IsDeleted" = FALSE;

CREATE INDEX IF NOT EXISTS "IX_EntityFormDocuments_CreatedAt" 
    ON "EntityFormDocuments" ("CreatedAt" DESC);

-- ===========================================
-- 3. CREATE VIEW: vw_EntityFormDocuments
-- ===========================================
-- This view joins with DynamicForms to include form name
CREATE OR REPLACE VIEW "vw_EntityFormDocuments" AS
SELECT 
    efd."Id",
    efd."EntityType",
    efd."EntityId",
    efd."FormId",
    efd."FormVersion",
    df."NameEn" AS "FormNameEn",
    df."NameFr" AS "FormNameFr",
    efd."Title",
    efd."Status",
    efd."Responses",
    efd."CreatedBy",
    efd."CreatedAt",
    efd."UpdatedAt",
    efd."IsDeleted"
FROM "EntityFormDocuments" efd
LEFT JOIN "DynamicForms" df ON efd."FormId" = df."Id"
WHERE efd."IsDeleted" = FALSE;

-- ===========================================
-- 4. SAMPLE DATA (Optional - for testing)
-- ===========================================
-- INSERT INTO "EntityFormDocuments" ("EntityType", "EntityId", "FormId", "FormVersion", "Title", "Status", "Responses", "CreatedBy")
-- VALUES 
--     ('offer', 1, 1, 1, 'Site Inspection Form', 'completed', '{"field1": "value1"}', 'admin'),
--     ('sale', 1, 2, 1, 'Installation Checklist', 'draft', '{"field2": "value2"}', 'admin');

-- ===========================================
-- 5. BACKEND API ENDPOINTS (Reference)
-- ===========================================
/*
Required API Endpoints for EntityFormDocumentsController:

GET    /api/EntityFormDocuments/{entityType}/{entityId}
       - Returns all form documents for an entity
       - Uses vw_EntityFormDocuments view
       - Filter: IsDeleted = false

GET    /api/EntityFormDocuments/{id}
       - Returns single form document by ID
       
POST   /api/EntityFormDocuments
       - Creates new form document
       - Body: { EntityType, EntityId, FormId, Title?, Responses }
       - Auto-sets: FormVersion from DynamicForms.Version, Status='draft'

PUT    /api/EntityFormDocuments/{id}
       - Updates existing form document
       - Body: { Title?, Status?, Responses }
       - Only allow update if Status != 'completed' OR only updating to 'completed'

DELETE /api/EntityFormDocuments/{id}
       - Soft delete (set IsDeleted = true)
*/

-- ===========================================
-- 6. .NET ENTITY MODEL (Reference)
-- ===========================================
/*
public class EntityFormDocument : BaseEntityWithSoftDelete
{
    public string EntityType { get; set; } // "offer" or "sale"
    public int EntityId { get; set; }
    public int FormId { get; set; }
    public int FormVersion { get; set; }
    public string? Title { get; set; }
    public string Status { get; set; } = "draft";
    public JsonDocument Responses { get; set; }
    
    // Navigation
    public virtual DynamicForm Form { get; set; }
}
*/

-- ===========================================
-- 7. DTOs (Reference)
-- ===========================================
/*
// Read DTO
public class EntityFormDocumentDto
{
    public int Id { get; set; }
    public string EntityType { get; set; }
    public int EntityId { get; set; }
    public int FormId { get; set; }
    public int FormVersion { get; set; }
    public string FormNameEn { get; set; }
    public string FormNameFr { get; set; }
    public string? Title { get; set; }
    public string Status { get; set; }
    public JsonDocument Responses { get; set; }
    public string CreatedBy { get; set; }
    public string? CreatedByName { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

// Create DTO
public class CreateEntityFormDocumentDto
{
    [Required]
    public string EntityType { get; set; }
    [Required]
    public int EntityId { get; set; }
    [Required]
    public int FormId { get; set; }
    public string? Title { get; set; }
    public JsonDocument? Responses { get; set; }
}

// Update DTO
public class UpdateEntityFormDocumentDto
{
    public string? Title { get; set; }
    public string? Status { get; set; }
    public JsonDocument? Responses { get; set; }
}
*/

-- ===========================================
-- 8. TRIGGER: Auto-update UpdatedAt
-- ===========================================
CREATE OR REPLACE FUNCTION update_entity_form_documents_updated_at()
RETURNS TRIGGER AS $$
BEGIN
    NEW."UpdatedAt" = CURRENT_TIMESTAMP;
    RETURN NEW;
END;
$$ LANGUAGE plpgsql;

DROP TRIGGER IF EXISTS trg_entity_form_documents_updated_at ON "EntityFormDocuments";
CREATE TRIGGER trg_entity_form_documents_updated_at
    BEFORE UPDATE ON "EntityFormDocuments"
    FOR EACH ROW
    EXECUTE FUNCTION update_entity_form_documents_updated_at();

-- ===========================================
-- DONE!
-- ===========================================
-- After running this script:
-- 1. Add the Entity model to your .NET project
-- 2. Create the Controller and Service
-- 3. Add AutoMapper mappings for DTOs
-- 4. Register the service in DI container
