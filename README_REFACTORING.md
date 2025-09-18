# FlowServiceBackend Refactoring Guide

## Overview
This document outlines the new professional structure for the FlowServiceBackend project to improve maintainability and reduce large files.

## New Structure

### 1. Data Layer Structure
```
FlowServiceBackend/
├── Data/
│   ├── ApplicationDbContext.Partial.cs      # Main DbContext (clean)
│   ├── ApplicationDbContext.Legacy.cs       # Legacy configs (to be refactored)
│   ├── Configurations/                      # Entity configurations by domain
│   │   ├── IEntityConfiguration.cs          # Base interface
│   │   ├── Users/                           # User-related configurations
│   │   │   ├── MainAdminUserConfiguration.cs
│   │   │   ├── UserPreferencesConfiguration.cs
│   │   │   └── UserConfiguration.cs
│   │   ├── Roles/                           # Role-related configurations
│   │   │   ├── RoleConfiguration.cs
│   │   │   └── UserRoleConfiguration.cs
│   │   ├── Skills/                          # Skill-related configurations
│   │   │   ├── SkillConfiguration.cs
│   │   │   ├── UserSkillConfiguration.cs
│   │   │   └── RoleSkillConfiguration.cs
│   │   └── Contacts/                        # Contact-related configurations
│   │       ├── ContactConfiguration.cs
│   │       ├── ContactNoteConfiguration.cs
│   │       └── ContactTagConfiguration.cs
│   └── SeedData/                            # Seed data classes
│       ├── LookupSeedData.cs
│       └── CurrencySeedData.cs
```

### 2. Domain Layer (New)
```
FlowServiceBackend/
├── Domain/
│   └── Common/
│       └── BaseEntity.cs                    # Base entities with audit fields
```

### 3. Services Layer (Refactored)
```
FlowServiceBackend/
├── Services/
│   ├── Users/
│   │   └── UserManagementService.cs         # Focused user management
│   ├── Contacts/
│   │   └── ContactManagementService.cs      # Focused contact management
│   ├── Roles/
│   │   └── RoleManagementService.cs         # Role management (to be created)
│   └── Skills/
│       └── SkillManagementService.cs        # Skill management (to be created)
```

## Benefits of New Structure

### 1. Single Responsibility Principle
- Each configuration file handles one entity
- Each service focuses on one domain
- Clear separation of concerns

### 2. Maintainability
- Smaller, focused files are easier to maintain
- Changes to one entity don't affect others
- Easy to locate and modify specific functionality

### 3. Scalability
- Easy to add new domains/entities
- Consistent patterns for new features
- Modular structure supports team development

### 4. Testability
- Smaller services are easier to unit test
- Domain separation enables focused testing
- Mock dependencies more easily

## Migration Steps

### Phase 1: Entity Configurations (✅ DONE)
- [x] Create configuration base interface
- [x] Split Users domain configurations
- [x] Split Roles domain configurations  
- [x] Split Skills domain configurations
- [x] Split Contacts domain configurations
- [x] Create seed data classes
- [x] Update main ApplicationDbContext

### Phase 2: Remaining Configurations (TODO)
- [ ] Create Articles/Materials configurations
- [ ] Create Calendar configurations
- [ ] Create Lookup configurations
- [ ] Remove ApplicationDbContext.Legacy.cs

### Phase 3: Service Layer (IN PROGRESS)
- [x] Create domain-focused service structure
- [ ] Migrate existing service logic
- [ ] Update dependency injection registration
- [ ] Update controllers to use new services

### Phase 4: Domain Models (TODO)
- [ ] Create domain entities inheriting from BaseEntity
- [ ] Implement domain-specific validation
- [ ] Add domain services for business logic

## Usage Guidelines

### Adding New Entity Configuration
1. Create new configuration class implementing `IEntityConfiguration`
2. Place in appropriate domain folder under `Data/Configurations/`
3. Add configuration call in `ApplicationDbContext.ApplyEntityConfigurations()`

### Adding New Domain
1. Create domain folder under `Data/Configurations/`
2. Create service folder under `Services/`
3. Follow existing patterns for consistency

### Best Practices
- Keep configuration files focused on single entities
- Use meaningful names that reflect business domains
- Maintain consistent folder structure
- Document complex relationships
- Follow dependency injection patterns

## Rollback Plan
If issues arise, the original `ApplicationDbContext.cs` can be restored by:
1. Reverting to the original single file
2. Removing the new partial classes
3. Updating Program.cs registrations

## Next Steps
1. Complete Phase 2 configurations
2. Migrate service implementations
3. Add unit tests for new structure
4. Update documentation
5. Remove legacy code