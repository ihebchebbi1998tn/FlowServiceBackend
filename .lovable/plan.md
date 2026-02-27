## Unit Quantity Types - Database Migration

Run these SQL statements to add the Unit column to material tables:

```sql
ALTER TABLE "MaterialUsage" ADD COLUMN "Unit" VARCHAR(20) NOT NULL DEFAULT 'piece';
ALTER TABLE "ServiceOrderMaterials" ADD COLUMN "Unit" VARCHAR(20) NOT NULL DEFAULT 'piece';
```

Also ensure the Articles table has a Unit column (if not already):

```sql
ALTER TABLE "Articles" ADD COLUMN IF NOT EXISTS "Unit" VARCHAR(20) NOT NULL DEFAULT 'piece';
```
