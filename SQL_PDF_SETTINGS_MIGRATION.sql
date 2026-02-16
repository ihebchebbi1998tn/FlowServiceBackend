-- ==========================================
-- PDF SETTINGS - DEDICATED GLOBAL TABLE
-- ================================,==========
-- This creates a separate table for PDF settings that appliesde
-- to the entire application (not per-user).
-- ==========================================

-- STEP 1: Drop the functions we created earlier (cleanup)
DROP FUNCTION IF EXISTS initialize_pdf_settings(JSONB);
DROP FUNCTION IF EXISTS get_pdf_settings(INT, TEXT);
DROP FUNCTION IF EXISTS update_pdf_settings(INT, TEXT, JSONB);

-- STEP 2: Create the PDF Settings table
CREATE TABLE IF NOT EXISTS "PdfSettings" (
    "Id" SERIAL PRIMARY KEY,
    "Module" VARCHAR(50) NOT NULL UNIQUE,  -- 'offers', 'sales', 'dispatches', 'serviceOrders'
    "SettingsJson" JSONB NOT NULL DEFAULT '{}',
    "CreatedAt" TIMESTAMP DEFAULT NOW(),
    "UpdatedAt" TIMESTAMP DEFAULT NOW()
);

-- STEP 3: Create index for faster lookups
CREATE INDEX IF NOT EXISTS idx_pdf_settings_module ON "PdfSettings" ("Module");

-- STEP 4: Insert default records for each module
INSERT INTO "PdfSettings" ("Module", "SettingsJson") 
VALUES 
    ('offers', '{}'),
    ('sales', '{}'),
    ('dispatches', '{}'),
    ('serviceOrders', '{}')
ON CONFLICT ("Module") DO NOTHING;

-- STEP 5: Create a function to get settings by module
CREATE OR REPLACE FUNCTION get_global_pdf_settings(p_module VARCHAR(50))
RETURNS JSONB AS $$
BEGIN
    RETURN (SELECT "SettingsJson" FROM "PdfSettings" WHERE "Module" = p_module);
END;
$$ LANGUAGE plpgsql;

-- STEP 6: Create a function to update settings by module
CREATE OR REPLACE FUNCTION update_global_pdf_settings(p_module VARCHAR(50), p_settings JSONB)
RETURNS JSONB AS $$
BEGIN
    UPDATE "PdfSettings" 
    SET "SettingsJson" = p_settings, "UpdatedAt" = NOW()
    WHERE "Module" = p_module;
    
    IF NOT FOUND THEN
        INSERT INTO "PdfSettings" ("Module", "SettingsJson")
        VALUES (p_module, p_settings);
    END IF;
    
    RETURN p_settings;
END;
$$ LANGUAGE plpgsql;

-- ==========================================
-- VERIFICATION
-- ==========================================
-- SELECT * FROM "PdfSettings";
