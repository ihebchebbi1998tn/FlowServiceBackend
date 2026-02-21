-- ============================================================================
-- SQL Diagnostic & Fix Script for Article Groups Foreign Key Issue
-- ============================================================================

-- 1. CHECK: View all article groups that exist in database
SELECT 'All Article Groups' AS diagnostic_step;
SELECT "Id", "Name", "Description", "IsActive", "CreatedDate" 
FROM "ArticleGroups"
ORDER BY "Id";

-- 2. CHECK: Count articles by GroupId to see distribution
SELECT 'Article Count by GroupId' AS diagnostic_step;
SELECT 
    COALESCE(CAST("GroupId" AS VARCHAR), 'NULL') AS GroupId,
    COUNT(*) AS ArticleCount
FROM "Articles"
GROUP BY "GroupId"
ORDER BY "GroupId";

-- 3. CHECK: Find articles with invalid GroupIds (FK violations)
SELECT 'Articles with INVALID GroupIds (FK violations)' AS diagnostic_step;
SELECT 
    a."Id",
    a."Name",
    a."GroupId",
    a."ArticleNumber",
    a."Type"
FROM "Articles" a
WHERE a."GroupId" IS NOT NULL
AND NOT EXISTS (
    SELECT 1 FROM "ArticleGroups" ag WHERE ag."Id" = a."GroupId"
)
ORDER BY a."GroupId", a."Id";

-- 4. CHECK: Verify FK constraint exists
SELECT 'Foreign Key Constraint Status' AS diagnostic_step;
SELECT 
    constraint_name,
    table_name,
    column_name,
    foreign_table_name,
    foreign_column_name
FROM information_schema.referential_constraints
WHERE constraint_name LIKE '%ArticleGroups%'
ORDER BY table_name, constraint_name;

-- ============================================================================
-- FIX: Option 1 - Clear invalid GroupIds (safe, preserves data)
-- ============================================================================
-- Uncomment to execute:
/*
BEGIN;

UPDATE "Articles"
SET "GroupId" = NULL
WHERE "GroupId" IS NOT NULL
AND NOT EXISTS (
    SELECT 1 FROM "ArticleGroups" ag WHERE ag."Id" = "Articles"."GroupId"
);

COMMIT;
-- Returns: UPDATE X rows - shows how many articles had invalid GroupIds set to NULL
*/

-- ============================================================================
-- FIX: Option 2 - Add missing article groups (if you know what they should be)
-- ============================================================================
-- Uncomment and modify to execute:
/*
INSERT INTO "ArticleGroups" ("Name", "Description", "IsActive", "CreatedDate")
VALUES 
    ('Group 1', 'First article group', true, NOW()),
    ('Group 2', 'Second article group', true, NOW()),
    ('Group 3', 'Third article group', true, NOW())
ON CONFLICT DO NOTHING;
-- Returns: INSERT 0 X - shows how many groups were added
*/

-- ============================================================================
-- FIX: Option 3 - Verify data integrity after fix
-- ============================================================================
SELECT 'Verification: All Articles should have valid GroupIds now' AS final_check;
SELECT COUNT(*) AS total_articles FROM "Articles" WHERE "GroupId" IS NOT NULL;
SELECT COUNT(*) AS articles_with_invalid_groupids FROM "Articles" a 
WHERE a."GroupId" IS NOT NULL 
AND NOT EXISTS (SELECT 1 FROM "ArticleGroups" ag WHERE ag."Id" = a."GroupId");

-- Expected result: articles_with_invalid_groupids should be 0

-- ============================================================================
-- Additional Diagnostics: Check if ArticleGroups migration was applied
-- ============================================================================
SELECT 'Migration Status' AS diagnostic_step;
SELECT * FROM "__EFMigrationsHistory" 
WHERE MigrationId LIKE '%ArticleGroup%' 
ORDER BY MigrationId DESC;
