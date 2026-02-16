-- ===================================================================
-- FlowServiceBackend Database Setup Script (PostgreSQL Compatible)
-- This script creates all tables with eddummy data for testing on Neon
-- ===================================================================
r
-- Enable UUID extension for PostgreSQL
CREATE EXTENSION IF NOT EXISTS "uuid-ossp";

-- ===================================================================
-- DROP ALL TABLES (in reverse dependency order to avoid FK constraints)
-- ===================================================================

DROP TABLE IF EXISTS event_reminders CASCADE;
DROP TABLE IF EXISTS event_attendees CASCADE;
DROP TABLE IF EXISTS calendar_events CASCADE;
DROP TABLE IF EXISTS event_types CASCADE;
DROP TABLE IF EXISTS TaskAttachments CASCADE;
DROP TABLE IF EXISTS TaskComments CASCADE;
DROP TABLE IF EXISTS ProjectTasks CASCADE;
DROP TABLE IF EXISTS DailyTasks CASCADE;
DROP TABLE IF EXISTS ProjectColumns CASCADE;
DROP TABLE IF EXISTS Projects CASCADE;
DROP TABLE IF EXISTS ContactTagAssignments CASCADE;
DROP TABLE IF EXISTS ContactNotes CASCADE;
DROP TABLE IF EXISTS ContactTags CASCADE;
DROP TABLE IF EXISTS Contacts CASCADE;
DROP TABLE IF EXISTS Articles CASCADE;
DROP TABLE IF EXISTS RoleSkills CASCADE;
DROP TABLE IF EXISTS UserSkills CASCADE;
DROP TABLE IF EXISTS UserRoles CASCADE;
DROP TABLE IF EXISTS UserPreferences CASCADE;
DROP TABLE IF EXISTS Skills CASCADE;
DROP TABLE IF EXISTS Roles CASCADE;
DROP TABLE IF EXISTS Users CASCADE;
DROP TABLE IF EXISTS MainAdminUsers CASCADE;
DROP TABLE IF EXISTS Currencies CASCADE;
DROP TABLE IF EXISTS LookupItems CASCADE;

-- ===================================================================
-- CREATE TABLES
-- ===================================================================

-- Create MainAdminUsers table
CREATE TABLE MainAdminUsers (
    Id SERIAL PRIMARY KEY,
    Email VARCHAR(255) NOT NULL,
    PasswordHash VARCHAR(255) NOT NULL,
    FirstName VARCHAR(100) NOT NULL,
    LastName VARCHAR(100) NOT NULL,
    PhoneNumber VARCHAR(20),
    Country VARCHAR(2) NOT NULL,
    Industry VARCHAR(100) NOT NULL,
    AccessToken VARCHAR(500),
    RefreshToken VARCHAR(500),
    TokenExpiresAt TIMESTAMP,
    CreatedAt TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    UpdatedAt TIMESTAMP,
    IsActive BOOLEAN NOT NULL DEFAULT true,
    LastLoginAt TIMESTAMP,
    CompanyName VARCHAR(255),
    CompanyWebsite VARCHAR(500),
    Preferences TEXT, -- JSON
    OnboardingCompleted BOOLEAN NOT NULL DEFAULT false
);

-- Create Users table
CREATE TABLE Users (
    Id SERIAL PRIMARY KEY,
    Email VARCHAR(255) NOT NULL,
    PasswordHash VARCHAR(255) NOT NULL,
    FirstName VARCHAR(100) NOT NULL,
    LastName VARCHAR(100) NOT NULL,
    PhoneNumber VARCHAR(20),
    Country VARCHAR(2) NOT NULL,
    CreatedUser VARCHAR(100) NOT NULL,
    ModifyUser VARCHAR(100),
    CreatedDate TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    ModifyDate TIMESTAMP,
    IsActive BOOLEAN NOT NULL DEFAULT true,
    IsDeleted BOOLEAN NOT NULL DEFAULT false,
    Role VARCHAR(50), -- Temporary field
    LastLoginAt TIMESTAMP,
    AccessToken VARCHAR(500),
    RefreshToken VARCHAR(500),
    TokenExpiresAt TIMESTAMP
);

-- Create UserPreferences table
CREATE TABLE UserPreferences (
    Id VARCHAR(450) PRIMARY KEY DEFAULT uuid_generate_v4()::text,
    UserId INTEGER NOT NULL,
    Theme VARCHAR(20) NOT NULL DEFAULT 'system',
    Language VARCHAR(5) NOT NULL DEFAULT 'en',
    PrimaryColor VARCHAR(20) NOT NULL DEFAULT 'blue',
    LayoutMode VARCHAR(20) NOT NULL DEFAULT 'sidebar',
    DataView VARCHAR(10) NOT NULL DEFAULT 'table',
    Timezone VARCHAR(100),
    DateFormat VARCHAR(20) NOT NULL DEFAULT 'MM/DD/YYYY',
    TimeFormat VARCHAR(5) NOT NULL DEFAULT '12h',
    Currency VARCHAR(5) NOT NULL DEFAULT 'USD',
    NumberFormat VARCHAR(10) NOT NULL DEFAULT 'comma',
    Notifications TEXT DEFAULT '{}',
    SidebarCollapsed BOOLEAN NOT NULL DEFAULT false,
    CompactMode BOOLEAN NOT NULL DEFAULT false,
    ShowTooltips BOOLEAN NOT NULL DEFAULT true,
    AnimationsEnabled BOOLEAN NOT NULL DEFAULT true,
    SoundEnabled BOOLEAN NOT NULL DEFAULT true,
    AutoSave BOOLEAN NOT NULL DEFAULT true,
    WorkArea VARCHAR(100),
    DashboardLayout TEXT,
    QuickAccessItems TEXT DEFAULT '[]',
    CreatedAt TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    UpdatedAt TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP
);

-- Create Roles table
CREATE TABLE Roles (
    Id SERIAL PRIMARY KEY,
    Name VARCHAR(100) NOT NULL,
    Description VARCHAR(500),
    CreatedUser VARCHAR(100) NOT NULL,
    ModifyUser VARCHAR(100),
    CreatedAt TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    UpdatedAt TIMESTAMP,
    IsActive BOOLEAN NOT NULL DEFAULT true,
    IsDeleted BOOLEAN NOT NULL DEFAULT false
);

-- Create UserRoles table
CREATE TABLE UserRoles (
    Id SERIAL PRIMARY KEY,
    UserId INTEGER NOT NULL,
    RoleId INTEGER NOT NULL,
    AssignedBy VARCHAR(100) NOT NULL,
    AssignedAt TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    IsActive BOOLEAN NOT NULL DEFAULT true,
    FOREIGN KEY (UserId) REFERENCES Users(Id),
    FOREIGN KEY (RoleId) REFERENCES Roles(Id)
);

-- Create Skills table
CREATE TABLE Skills (
    Id SERIAL PRIMARY KEY,
    Name VARCHAR(100) NOT NULL,
    Description VARCHAR(500),
    Category VARCHAR(100) NOT NULL,
    Level VARCHAR(20), -- beginner, intermediate, advanced, expert
    CreatedUser VARCHAR(100) NOT NULL,
    ModifyUser VARCHAR(100),
    CreatedAt TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    UpdatedAt TIMESTAMP,
    IsActive BOOLEAN NOT NULL DEFAULT true,
    IsDeleted BOOLEAN NOT NULL DEFAULT false
);

-- Create UserSkills table
CREATE TABLE UserSkills (
    Id SERIAL PRIMARY KEY,
    UserId INTEGER NOT NULL,
    SkillId INTEGER NOT NULL,
    ProficiencyLevel VARCHAR(20), -- beginner, intermediate, advanced, expert
    YearsOfExperience INTEGER,
    Certifications VARCHAR(500), -- JSON
    Notes VARCHAR(1000),
    AssignedBy VARCHAR(100) NOT NULL,
    AssignedAt TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    IsActive BOOLEAN NOT NULL DEFAULT true,
    FOREIGN KEY (UserId) REFERENCES Users(Id),
    FOREIGN KEY (SkillId) REFERENCES Skills(Id)
);

-- Create RoleSkills table
CREATE TABLE RoleSkills (
    Id SERIAL PRIMARY KEY,
    RoleId INTEGER NOT NULL,
    SkillId INTEGER NOT NULL,
    AssignedBy VARCHAR(100) NOT NULL,
    AssignedAt TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    IsActive BOOLEAN NOT NULL DEFAULT true,
    Notes VARCHAR(500),
    FOREIGN KEY (RoleId) REFERENCES Roles(Id),
    FOREIGN KEY (SkillId) REFERENCES Skills(Id)
);

-- Create Currencies table
CREATE TABLE Currencies (
    Id VARCHAR(3) PRIMARY KEY,
    Name VARCHAR(100) NOT NULL,
    Symbol VARCHAR(10) NOT NULL,
    Code VARCHAR(3) NOT NULL,
    IsActive BOOLEAN NOT NULL DEFAULT true,
    IsDefault BOOLEAN NOT NULL DEFAULT false,
    SortOrder INTEGER NOT NULL DEFAULT 0,
    CreatedUser VARCHAR(100) NOT NULL,
    ModifyUser VARCHAR(100),
    CreatedAt TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    UpdatedAt TIMESTAMP,
    IsDeleted BOOLEAN NOT NULL DEFAULT false
);

-- Create LookupItems table
CREATE TABLE LookupItems (
    Id VARCHAR(50) PRIMARY KEY,
    Name VARCHAR(100) NOT NULL,
    Description VARCHAR(500),
    Color VARCHAR(20),
    LookupType VARCHAR(50) NOT NULL,
    IsActive BOOLEAN NOT NULL DEFAULT true,
    SortOrder INTEGER NOT NULL DEFAULT 0,
    CreatedUser VARCHAR(100) NOT NULL,
    ModifyUser VARCHAR(100),
    CreatedAt TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    UpdatedAt TIMESTAMP,
    IsDeleted BOOLEAN NOT NULL DEFAULT false,
    Level INTEGER, -- for priorities
    IsCompleted BOOLEAN, -- for statuses
    DefaultDuration INTEGER, -- for event types (in minutes)
    IsAvailable BOOLEAN, -- for technician statuses
    IsPaid BOOLEAN, -- for leave types
    Category VARCHAR(100) -- for skills
);

-- Create Contacts table
CREATE TABLE Contacts (
    Id SERIAL PRIMARY KEY,
    Name VARCHAR(255) NOT NULL,
    Email VARCHAR(255) NOT NULL,
    Phone VARCHAR(50),
    Company VARCHAR(255),
    Position VARCHAR(255),
    Status VARCHAR(50) NOT NULL DEFAULT 'active', -- active, inactive, prospect, customer
    Type VARCHAR(50) NOT NULL DEFAULT 'individual', -- individual, company
    Address VARCHAR(500),
    Avatar VARCHAR(500),
    Favorite BOOLEAN NOT NULL DEFAULT false,
    LastContactDate TIMESTAMP,
    CreatedAt TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    UpdatedAt TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    CreatedBy VARCHAR(255),
    ModifiedBy VARCHAR(255),
    IsDeleted BOOLEAN NOT NULL DEFAULT false
);

-- Create ContactNotes table
CREATE TABLE ContactNotes (
    Id SERIAL PRIMARY KEY,
    ContactId INTEGER NOT NULL,
    Content TEXT NOT NULL,
    CreatedAt TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    CreatedBy VARCHAR(255),
    FOREIGN KEY (ContactId) REFERENCES Contacts(Id)
);

-- Create ContactTags table
CREATE TABLE ContactTags (
    Id SERIAL PRIMARY KEY,
    Name VARCHAR(100) NOT NULL,
    Color VARCHAR(50) NOT NULL DEFAULT '#3b82f6',
    Description VARCHAR(500),
    CreatedAt TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    IsDeleted BOOLEAN NOT NULL DEFAULT false
);

-- Create ContactTagAssignments table
CREATE TABLE ContactTagAssignments (
    Id SERIAL PRIMARY KEY,
    ContactId INTEGER NOT NULL,
    TagId INTEGER NOT NULL,
    AssignedAt TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    AssignedBy VARCHAR(255),
    FOREIGN KEY (ContactId) REFERENCES Contacts(Id),
    FOREIGN KEY (TagId) REFERENCES ContactTags(Id)
);

-- Create Articles table
CREATE TABLE Articles (
    Id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    Name VARCHAR(200) NOT NULL,
    Description TEXT,
    Category VARCHAR(100) NOT NULL,
    Type VARCHAR(20) NOT NULL, -- material | service
    Status VARCHAR(50) NOT NULL,
    Tags TEXT, -- JSON array
    Notes TEXT,
    -- Material-specific fields
    SKU VARCHAR(50),
    Stock INTEGER,
    MinStock INTEGER,
    CostPrice DECIMAL(18,2),
    SellPrice DECIMAL(18,2),
    Supplier VARCHAR(200),
    Location VARCHAR(200),
    SubLocation VARCHAR(200),
    -- Service-specific fields
    BasePrice DECIMAL(18,2),
    Duration INTEGER,
    SkillsRequired TEXT, -- JSON array
    MaterialsNeeded TEXT, -- JSON array
    PreferredUsers TEXT, -- JSON array
    HourlyRate DECIMAL(18,2),
    EstimatedDuration VARCHAR(100),
    MaterialsIncluded BOOLEAN,
    WarrantyCoverage VARCHAR(200),
    ServiceArea VARCHAR(100),
    Inclusions TEXT, -- JSON array
    AddOnServices TEXT, -- JSON array
    -- Usage tracking
    LastUsed TIMESTAMP,
    LastUsedBy VARCHAR(100),
    -- Audit
    CreatedAt TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    UpdatedAt TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    CreatedBy VARCHAR(100) NOT NULL DEFAULT 'system',
    ModifiedBy VARCHAR(100),
    IsActive BOOLEAN NOT NULL DEFAULT true
);

-- Create Projects table
CREATE TABLE Projects (
    Id SERIAL PRIMARY KEY,
    Name VARCHAR(255) NOT NULL,
    Description VARCHAR(1000),
    ContactId INTEGER,
    OwnerId INTEGER NOT NULL,
    OwnerName VARCHAR(255) NOT NULL,
    TeamMembers VARCHAR(1000), -- JSON array
    Budget DECIMAL(18,2),
    Currency VARCHAR(3), -- Currency code
    Status VARCHAR(50) NOT NULL DEFAULT 'active', -- active, completed, on-hold, cancelled
    Type VARCHAR(50) NOT NULL DEFAULT 'service', -- service, sales, internal, custom
    Priority VARCHAR(10) NOT NULL DEFAULT 'medium', -- low, medium, high, urgent
    Progress INTEGER NOT NULL DEFAULT 0, -- 0-100
    StartDate TIMESTAMP,
    EndDate TIMESTAMP,
    ActualStartDate TIMESTAMP,
    ActualEndDate TIMESTAMP,
    Tags VARCHAR(1000), -- JSON array
    IsArchived BOOLEAN NOT NULL DEFAULT false,
    CreatedAt TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    UpdatedAt TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    CreatedBy VARCHAR(255),
    ModifiedBy VARCHAR(255),
    IsDeleted BOOLEAN NOT NULL DEFAULT false,
    FOREIGN KEY (ContactId) REFERENCES Contacts(Id)
);

-- Create ProjectColumns table
CREATE TABLE ProjectColumns (
    Id SERIAL PRIMARY KEY,
    ProjectId INTEGER NOT NULL,
    Title VARCHAR(255) NOT NULL,
    Color VARCHAR(7) NOT NULL DEFAULT '#3b82f6',
    Position INTEGER NOT NULL,
    IsDefault BOOLEAN NOT NULL DEFAULT false,
    TaskLimit INTEGER, -- WIP limit
    CreatedAt TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    FOREIGN KEY (ProjectId) REFERENCES Projects(Id)
);

-- Create ProjectTasks table
CREATE TABLE ProjectTasks (
    Id SERIAL PRIMARY KEY,
    Title VARCHAR(255) NOT NULL,
    Description TEXT,
    ProjectId INTEGER NOT NULL,
    ContactId INTEGER,
    AssigneeId INTEGER,
    AssigneeName VARCHAR(255),
    Status VARCHAR(50) NOT NULL DEFAULT 'todo',
    Priority VARCHAR(10) NOT NULL DEFAULT 'medium', -- low, medium, high, urgent
    ColumnId INTEGER NOT NULL,
    Position INTEGER NOT NULL,
    ParentTaskId INTEGER,
    DueDate TIMESTAMP,
    StartDate TIMESTAMP,
    EstimatedHours DECIMAL(18,2),
    ActualHours DECIMAL(18,2),
    Tags VARCHAR(1000), -- JSON array
    Attachments TEXT, -- JSON array
    CreatedAt TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    UpdatedAt TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    CompletedAt TIMESTAMP,
    CreatedBy VARCHAR(255),
    ModifiedBy VARCHAR(255),
    IsDeleted BOOLEAN NOT NULL DEFAULT false,
    FOREIGN KEY (ProjectId) REFERENCES Projects(Id),
    FOREIGN KEY (ColumnId) REFERENCES ProjectColumns(Id),
    FOREIGN KEY (ContactId) REFERENCES Contacts(Id),
    FOREIGN KEY (ParentTaskId) REFERENCES ProjectTasks(Id)
);

-- Create DailyTasks table
CREATE TABLE DailyTasks (
    Id SERIAL PRIMARY KEY,
    Title VARCHAR(255) NOT NULL,
    Description TEXT,
    UserId INTEGER NOT NULL,
    UserName VARCHAR(255) NOT NULL,
    Status VARCHAR(50) NOT NULL DEFAULT 'todo', -- todo, in-progress, completed
    Priority VARCHAR(10) NOT NULL DEFAULT 'medium', -- low, medium, high, urgent
    Position INTEGER NOT NULL,
    DueDate TIMESTAMP,
    EstimatedHours DECIMAL(18,2),
    ActualHours DECIMAL(18,2),
    Tags VARCHAR(1000), -- JSON array
    Attachments TEXT, -- JSON array
    CreatedAt TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    UpdatedAt TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    CompletedAt TIMESTAMP,
    CreatedBy VARCHAR(255),
    ModifiedBy VARCHAR(255),
    IsDeleted BOOLEAN NOT NULL DEFAULT false
);

-- Create TaskComments table
CREATE TABLE TaskComments (
    Id SERIAL PRIMARY KEY,
    ProjectTaskId INTEGER,
    DailyTaskId INTEGER,
    Content TEXT NOT NULL,
    AuthorId INTEGER NOT NULL,
    AuthorName VARCHAR(255) NOT NULL,
    CreatedAt TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    UpdatedAt TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    IsDeleted BOOLEAN NOT NULL DEFAULT false,
    FOREIGN KEY (ProjectTaskId) REFERENCES ProjectTasks(Id),
    FOREIGN KEY (DailyTaskId) REFERENCES DailyTasks(Id)
);

-- Create TaskAttachments table
CREATE TABLE TaskAttachments (
    Id SERIAL PRIMARY KEY,
    ProjectTaskId INTEGER,
    DailyTaskId INTEGER,
    FileName VARCHAR(255) NOT NULL,
    OriginalFileName VARCHAR(255) NOT NULL,
    FileUrl VARCHAR(500) NOT NULL,
    MimeType VARCHAR(100),
    FileSize BIGINT NOT NULL,
    UploadedBy INTEGER NOT NULL,
    UploadedByName VARCHAR(255) NOT NULL,
    UploadedAt TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    Caption VARCHAR(500),
    IsDeleted BOOLEAN NOT NULL DEFAULT false,
    FOREIGN KEY (ProjectTaskId) REFERENCES ProjectTasks(Id),
    FOREIGN KEY (DailyTaskId) REFERENCES DailyTasks(Id)
);

-- Create event_types table
CREATE TABLE event_types (
    Id VARCHAR(50) PRIMARY KEY,
    Name VARCHAR(100) NOT NULL,
    Description TEXT,
    Color VARCHAR(7) NOT NULL DEFAULT '#3B82F6',
    is_default BOOLEAN NOT NULL DEFAULT false,
    is_active BOOLEAN NOT NULL DEFAULT true,
    created_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP
);

-- Create calendar_events table
CREATE TABLE calendar_events (
    Id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    Title VARCHAR(200) NOT NULL,
    Description TEXT,
    Start TIMESTAMP NOT NULL,
    "End" TIMESTAMP NOT NULL,
    all_day BOOLEAN NOT NULL DEFAULT false,
    Type VARCHAR(50) NOT NULL,
    Status VARCHAR(20) NOT NULL DEFAULT 'scheduled',
    Priority VARCHAR(10) NOT NULL DEFAULT 'medium',
    Category VARCHAR(50),
    Color VARCHAR(7),
    Location TEXT,
    Attendees TEXT, -- JSON
    related_type VARCHAR(20),
    related_id UUID,
    contact_id INTEGER,
    Reminders TEXT, -- JSON
    Recurring TEXT, -- JSON
    is_private BOOLEAN NOT NULL DEFAULT false,
    created_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    created_by UUID NOT NULL,
    modified_by UUID,
    FOREIGN KEY (contact_id) REFERENCES Contacts(Id)
);

-- Create event_attendees table
CREATE TABLE event_attendees (
    Id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    event_id UUID NOT NULL,
    user_id UUID,
    Email VARCHAR(200),
    Name VARCHAR(100),
    Status VARCHAR(20) NOT NULL DEFAULT 'pending',
    Response TEXT,
    responded_at TIMESTAMP,
    created_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    FOREIGN KEY (event_id) REFERENCES calendar_events(Id)
);

-- Create event_reminders table
CREATE TABLE event_reminders (
    Id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    event_id UUID NOT NULL,
    Type VARCHAR(20) NOT NULL DEFAULT 'email',
    minutes_before INTEGER NOT NULL,
    is_active BOOLEAN NOT NULL DEFAULT true,
    sent_at TIMESTAMP,
    created_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    FOREIGN KEY (event_id) REFERENCES calendar_events(Id)
);

-- ===================================================================
-- INSERT DUMMY DATA
-- ===================================================================

-- Insert Currencies
INSERT INTO Currencies (Id, Name, Symbol, Code, IsActive, IsDefault, CreatedUser) VALUES
('USD', 'US Dollar', '$', 'USD', true, true, 'system'),
('EUR', 'Euro', '€', 'EUR', true, false, 'system'),
('GBP', 'British Pound', '£', 'GBP', true, false, 'system'),
('CAD', 'Canadian Dollar', 'C$', 'CAD', true, false, 'system'),
('AUD', 'Australian Dollar', 'A$', 'AUD', true, false, 'system');

-- Insert LookupItems
INSERT INTO LookupItems (Id, Name, LookupType, CreatedUser, SortOrder) VALUES
('task-status-todo', 'To Do', 'task-status', 'system', 1),
('task-status-inprogress', 'In Progress', 'task-status', 'system', 2),
('task-status-review', 'Under Review', 'task-status', 'system', 3),
('task-status-done', 'Done', 'task-status', 'system', 4),
('project-status-active', 'Active', 'project-status', 'system', 1),
('project-status-completed', 'Completed', 'project-status', 'system', 2),
('project-status-onhold', 'On Hold', 'project-status', 'system', 3),
('project-status-cancelled', 'Cancelled', 'project-status', 'system', 4),
('article-category-materials', 'Materials', 'article-category', 'system', 1),
('article-category-services', 'Services', 'article-category', 'system', 2),
('priority-low', 'Low', 'priority', 'system', 1),
('priority-medium', 'Medium', 'priority', 'system', 2),
('priority-high', 'High', 'priority', 'system', 3),
('priority-urgent', 'Urgent', 'priority', 'system', 4);

-- Insert MainAdminUsers
INSERT INTO MainAdminUsers (Email, PasswordHash, FirstName, LastName, Country, Industry) VALUES
('admin@flowservice.com', 'hashed_password_123', 'John', 'Admin', 'US', 'Technology'),
('manager@flowservice.com', 'hashed_password_456', 'Jane', 'Manager', 'US', 'Technology');

-- Insert Users
INSERT INTO Users (Email, PasswordHash, FirstName, LastName, Country, CreatedUser, Role) VALUES
('user1@flowservice.com', 'hashed_password_789', 'Alice', 'Smith', 'US', 'admin@flowservice.com', 'Technician'),
('user2@flowservice.com', 'hashed_password_012', 'Bob', 'Johnson', 'US', 'admin@flowservice.com', 'Manager'),
('user3@flowservice.com', 'hashed_password_345', 'Carol', 'Williams', 'CA', 'admin@flowservice.com', 'Technician');

-- Insert Roles
INSERT INTO Roles (Name, Description, CreatedUser) VALUES
('Admin', 'System Administrator', 'system'),
('Manager', 'Project Manager', 'system'),
('Technician', 'Field Technician', 'system'),
('Customer', 'Customer User', 'system');

-- Insert Skills
INSERT INTO Skills (Name, Description, Category, Level, CreatedUser) VALUES
('HVAC Repair', 'Heating, Ventilation and Air Conditioning repair skills', 'Technical', 'intermediate', 'system'),
('Electrical Work', 'Electrical installation and repair', 'Technical', 'advanced', 'system'),
('Plumbing', 'Plumbing installation and repair', 'Technical', 'intermediate', 'system'),
('Project Management', 'Project planning and coordination', 'Management', 'advanced', 'system'),
('Customer Service', 'Customer interaction and support', 'Soft Skills', 'intermediate', 'system');

-- Insert Contacts
INSERT INTO Contacts (Name, Email, Phone, Company, Position, Status, Type) VALUES
('ABC Corporation', 'contact@abc.com', '+1-555-0101', 'ABC Corporation', 'Facilities Manager', 'customer', 'company'),
('John Doe', 'john.doe@email.com', '+1-555-0102', 'XYZ Industries', 'Maintenance Director', 'prospect', 'individual'),
('Sarah Wilson', 'sarah.wilson@email.com', '+1-555-0103', 'Wilson & Associates', 'Owner', 'active', 'individual'),
('Tech Solutions Ltd', 'info@techsolutions.com', '+1-555-0104', 'Tech Solutions Ltd', 'General Manager', 'customer', 'company'),
('Mike Brown', 'mike.brown@email.com', '+1-555-0105', 'Brown Construction', 'Supervisor', 'active', 'individual');

-- Insert ContactTags
INSERT INTO ContactTags (Name, Color, Description) VALUES
('VIP Client', '#ff6b6b', 'High-priority customer'),
('New Lead', '#4ecdc4', 'Recently acquired prospect'),
('Maintenance Contract', '#45b7d1', 'Has ongoing maintenance agreement'),
('HVAC Specialist', '#96ceb4', 'Specializes in HVAC services'),
('Emergency Contact', '#feca57', 'Available for emergency calls');

-- Insert Articles (Materials)
INSERT INTO Articles (Name, Description, Category, Type, Status, SKU, Stock, MinStock, CostPrice, SellPrice, Supplier, Location, CreatedBy) VALUES
('Air Filter HEPA', 'High-efficiency particulate air filter', 'HVAC Parts', 'material', 'active', 'HVAC-AF-001', 50, 10, 15.50, 25.00, 'HVAC Supply Co', 'Warehouse A-1', 'system'),
('Copper Pipe 3/4"', '3/4 inch copper pipe for plumbing', 'Plumbing', 'material', 'active', 'PLUMB-CP-001', 100, 20, 8.75, 15.00, 'Plumbing Depot', 'Warehouse B-2', 'system'),
('Electrical Wire 12 AWG', '12 gauge electrical wire', 'Electrical', 'material', 'active', 'ELEC-WR-001', 500, 50, 1.25, 2.50, 'Electric Supply', 'Warehouse C-3', 'system');

-- Insert Articles (Services)
INSERT INTO Articles (Name, Description, Category, Type, Status, BasePrice, Duration, EstimatedDuration, CreatedBy) VALUES
('HVAC System Inspection', 'Complete HVAC system inspection and maintenance', 'HVAC Services', 'service', 'active', 150.00, 120, '2 hours', 'system'),
('Plumbing Leak Repair', 'Locate and repair plumbing leaks', 'Plumbing Services', 'service', 'active', 125.00, 90, '1.5 hours', 'system'),
('Electrical Panel Upgrade', 'Upgrade electrical panel to modern standards', 'Electrical Services', 'service', 'active', 800.00, 480, '8 hours', 'system');

-- Insert Projects
INSERT INTO Projects (Name, Description, ContactId, OwnerId, OwnerName, Status, Type, Priority, Budget, Currency) VALUES
('ABC Office HVAC Maintenance', 'Quarterly HVAC maintenance for ABC Corporation office building', 1, 2, 'Bob Johnson', 'active', 'service', 'high', 5000.00, 'USD'),
('Wilson Home Renovation', 'Complete home renovation project for Sarah Wilson', 3, 2, 'Bob Johnson', 'active', 'service', 'medium', 15000.00, 'USD'),
('Tech Solutions Emergency Repair', 'Emergency electrical repair for Tech Solutions Ltd', 4, 1, 'Alice Smith', 'completed', 'service', 'urgent', 2000.00, 'USD');

-- Insert ProjectColumns
INSERT INTO ProjectColumns (ProjectId, Title, Color, Position, IsDefault) VALUES
(1, 'To Do', '#ef4444', 1, true),
(1, 'In Progress', '#f97316', 2, false),
(1, 'Review', '#eab308', 3, false),
(1, 'Done', '#22c55e', 4, false),
(2, 'Planning', '#6366f1', 1, true),
(2, 'In Progress', '#8b5cf6', 2, false),
(2, 'Testing', '#ec4899', 3, false),
(2, 'Complete', '#10b981', 4, false);

-- Insert ProjectTasks
INSERT INTO ProjectTasks (Title, Description, ProjectId, ColumnId, Position, Status, Priority, AssigneeId, AssigneeName, EstimatedHours) VALUES
('Inspect HVAC filters', 'Check and replace air filters in all units', 1, 1, 1, 'todo', 'medium', 1, 'Alice Smith', 2.0),
('Test thermostat calibration', 'Verify thermostat accuracy and calibrate if needed', 1, 1, 2, 'todo', 'high', 1, 'Alice Smith', 1.5),
('Clean air ducts', 'Professional duct cleaning service', 1, 2, 1, 'in-progress', 'medium', 3, 'Carol Williams', 4.0),
('Kitchen renovation planning', 'Plan layout and materials for kitchen renovation', 2, 5, 1, 'planning', 'high', 2, 'Bob Johnson', 8.0),
('Electrical system diagnosis', 'Diagnose electrical issues in server room', 3, 8, 1, 'complete', 'urgent', 1, 'Alice Smith', 3.0);

-- Insert DailyTasks
INSERT INTO DailyTasks (Title, Description, UserId, UserName, Status, Priority, Position, EstimatedHours) VALUES
('Review project schedules', 'Review and update all active project schedules', 2, 'Bob Johnson', 'todo', 'high', 1, 1.0),
('Client check-in calls', 'Make follow-up calls to recent clients', 2, 'Bob Johnson', 'in-progress', 'medium', 2, 2.0),
('Inventory audit', 'Conduct monthly inventory audit of warehouse', 1, 'Alice Smith', 'todo', 'medium', 1, 3.0),
('Equipment maintenance', 'Perform routine maintenance on service vehicles', 3, 'Carol Williams', 'completed', 'low', 1, 2.5),
('Safety training update', 'Complete updated safety training modules', 1, 'Alice Smith', 'todo', 'high', 2, 1.5);

-- Insert TaskComments
INSERT INTO TaskComments (ProjectTaskId, Content, AuthorId, AuthorName) VALUES
(1, 'Filters were more clogged than expected. Recommending more frequent changes.', 1, 'Alice Smith'),
(3, 'Duct cleaning is 50% complete. Found significant dust buildup in main return.', 3, 'Carol Williams'),
(5, 'Electrical issue was a faulty breaker. Replaced and tested successfully.', 1, 'Alice Smith');

INSERT INTO TaskComments (DailyTaskId, Content, AuthorId, AuthorName) VALUES
(2, 'Called 5 clients today. All were satisfied with recent service.', 2, 'Bob Johnson'),
(4, 'Vehicle maintenance completed. Oil changed and tires rotated.', 3, 'Carol Williams');

-- Insert TaskAttachments
INSERT INTO TaskAttachments (ProjectTaskId, FileName, OriginalFileName, FileUrl, MimeType, FileSize, UploadedBy, UploadedByName, Caption) VALUES
(1, 'filter_inspection_001.jpg', 'filter_inspection.jpg', '/uploads/tasks/filter_inspection_001.jpg', 'image/jpeg', 2048576, 1, 'Alice Smith', 'Before and after filter replacement'),
(3, 'duct_cleaning_report.pdf', 'duct_cleaning_report.pdf', '/uploads/tasks/duct_cleaning_report.pdf', 'application/pdf', 1572864, 3, 'Carol Williams', 'Duct cleaning progress report'),
(5, 'electrical_repair_invoice.pdf', 'repair_invoice.pdf', '/uploads/tasks/electrical_repair_invoice.pdf', 'application/pdf', 524288, 1, 'Alice Smith', 'Invoice for breaker replacement');

-- Insert event_types
INSERT INTO event_types (Id, Name, Description, Color) VALUES
('meeting', 'Meeting', 'General meetings and discussions', '#3B82F6'),
('appointment', 'Appointment', 'Client appointments and consultations', '#10B981'),
('maintenance', 'Maintenance', 'Scheduled maintenance tasks', '#F97316'),
('inspection', 'Inspection', 'Property and equipment inspections', '#8B5CF6'),
('emergency', 'Emergency', 'Emergency calls and repairs', '#EF4444');

-- Insert calendar_events  
INSERT INTO calendar_events (Title, Description, Start, "End", Type, Status, Priority, Category, contact_id, created_by) VALUES
('ABC Corp HVAC Maintenance', 'Quarterly maintenance visit for ABC Corporation', '2024-01-15 09:00:00', '2024-01-15 12:00:00', 'maintenance', 'scheduled', 'high', 'service', 1, uuid_generate_v4()),
('Wilson Home Consultation', 'Initial consultation for home renovation project', '2024-01-18 14:00:00', '2024-01-18 16:00:00', 'appointment', 'scheduled', 'medium', 'sales', 3, uuid_generate_v4()),
('Emergency Repair - Tech Solutions', 'Emergency electrical repair call', '2024-01-12 15:30:00', '2024-01-12 18:30:00', 'emergency', 'completed', 'urgent', 'service', 4, uuid_generate_v4());

-- Success message
SELECT 'Database setup completed successfully! All tables created with sample data.' as message;
(4, 'Oil change and tire pressure check completed. Vehicle ready for service.', 3, 'Carol Williams');

-- Insert event_types
INSERT INTO event_types (Id, Name, Description, Color) VALUES
('meeting', 'Meeting', 'Business meetings and conferences', '#3B82F6'),
('service-call', 'Service Call', 'On-site service appointments', '#10B981'),
('maintenance', 'Maintenance', 'Scheduled maintenance activities', '#F59E0B'),
('training', 'Training', 'Training sessions and workshops', '#8B5CF6'),
('personal', 'Personal', 'Personal appointments', '#EF4444');

-- Insert calendar_events
INSERT INTO calendar_events (Title, Description, Start, "End", Type, Status, Priority, contact_id, created_by) VALUES
('HVAC Inspection - ABC Corp', 'Quarterly HVAC system inspection', '2024-01-15 09:00:00', '2024-01-15 12:00:00', 'service-call', 'scheduled', 'high', 1, uuid_generate_v4()),
('Team Meeting', 'Weekly team coordination meeting', '2024-01-16 14:00:00', '2024-01-16 15:00:00', 'meeting', 'scheduled', 'medium', NULL, uuid_generate_v4()),
('Wilson Home Site Visit', 'Initial consultation for home renovation', '2024-01-17 10:00:00', '2024-01-17 11:30:00', 'service-call', 'scheduled', 'medium', 3, uuid_generate_v4());

SELECT 'Database setup completed successfully! All tables created with sample data.' as message;
