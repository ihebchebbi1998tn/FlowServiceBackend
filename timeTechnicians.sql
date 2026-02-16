issue of time : 
-- =====================================================
-- Migration: Create Planning Module Tables
-- Works for both MainAdminUsers (id=1) and Users (id>=2)
-- =====================================================DA

-- 1. User Working Hours Table (no FK constraint - works for both user types)
CREATE TABLE IF NOT EXISTS "user_working_hours" (
    "id" SERIAL PRIMARY KEY,
    "user_id" INTEGER NOT NULL,
    "day_of_week" INTEGER NOT NULL CHECK ("day_of_week" >= 0 AND "day_of_week" <= 6),
    "start_time" TIME NOT NULL DEFAULT '08:00:00',
    "end_time" TIME NOT NULL DEFAULT '17:00:00',
    "lunch_start" TIME DEFAULT '12:00:00',
    "lunch_end" TIME DEFAULT '13:00:00',
    "is_active" BOOLEAN NOT NULL DEFAULT TRUE,
    "effective_from" TIMESTAMP WITH TIME ZONE,
    "effective_until" TIMESTAMP WITH TIME ZONE,
    "created_at" TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW(),
    "updated_at" TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW(),
    UNIQUE ("user_id", "day_of_week")
);

-- 2. User Leaves Table (no FK constraint - works for both user types)
CREATE TABLE IF NOT EXISTS "user_leaves" (
    "id" SERIAL PRIMARY KEY,
    "user_id" INTEGER NOT NULL,
    "leave_type" VARCHAR(100) NOT NULL,
    "start_date" TIMESTAMP WITH TIME ZONE NOT NULL,
    "end_date" TIMESTAMP WITH TIME ZONE NOT NULL,
    "status" VARCHAR(20) NOT NULL DEFAULT 'approved',
    "reason" TEXT,
    "notes" TEXT,
    "approved_by" INTEGER,
    "approved_at" TIMESTAMP WITH TIME ZONE,
    "created_at" TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW(),
    "updated_at" TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW()
);

-- Indexes for performance
CREATE INDEX IF NOT EXISTS "idx_user_working_hours_user_id" ON "user_working_hours"("user_id");
CREATE INDEX IF NOT EXISTS "idx_user_working_hours_day" ON "user_working_hours"("day_of_week");
CREATE INDEX IF NOT EXISTS "idx_user_leaves_user_id" ON "user_leaves"("user_id");
CREATE INDEX IF NOT EXISTS "idx_user_leaves_dates" ON "user_leaves"("start_date", "end_date");
CREATE INDEX IF NOT EXISTS "idx_user_leaves_status" ON "user_leaves"("status");

-- Insert default working hours for MainAdminUser (id=1) Mon-Fri 08:00-17:00
INSERT INTO "user_working_hours" ("user_id", "day_of_week", "start_time", "end_time", "lunch_start", "lunch_end", "is_active")
VALUES 
    (1, 1, '08:00', '17:00', '12:00', '13:00', TRUE),  -- Monday
    (1, 2, '08:00', '17:00', '12:00', '13:00', TRUE),  -- Tuesday
    (1, 3, '08:00', '17:00', '12:00', '13:00', TRUE),  -- Wednesday
    (1, 4, '08:00', '17:00', '12:00', '13:00', TRUE),  -- Thursday
    (1, 5, '08:00', '17:00', '12:00', '13:00', TRUE),  -- Friday
    (1, 6, '08:00', '17:00', '12:00', '13:00', FALSE), -- Saturday (off)
    (1, 0, '08:00', '17:00', '12:00', '13:00', FALSE)  -- Sunday (off)
ON CONFLICT ("user_id", "day_of_week") DO NOTHING;

-- Verification
SELECT 'Tables created successfully:' as status;
SELECT table_name FROM information_schema.tables 
WHERE table_schema = 'public' AND table_name IN ('user_working_hours', 'user_leaves');

SELECT 'Working hours for MainAdminUser:' as info;
SELECT * FROM "user_working_hours" WHERE "user_id" = 1;
