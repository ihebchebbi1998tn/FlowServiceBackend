# Retenue à la Source (RS) & TEJ Export Module

## Overview
This module handles Tunisian **Retenue à la Source** (Withholding Tax) management and **TEJ XML file export** for tax authority submission.

## API Endpoints

| Method | Endpoint | Description |
|--------|----------|-------------|
| `GET` | `/api/retenue-source` | List RS records (with filters) |
| `GET` | `/api/retenue-source/{id}` | Get single RS record |
| `POST` | `/api/retenue-source` | Create RS record |
| `PATCH` | `/api/retenue-source/{id}` | Update RS record |
| `DELETE` | `/api/retenue-source/{id}` | Delete RS record |
| `GET` | `/api/retenue-source/calculate` | Calculate RS amount |
| `POST` | `/api/retenue-source/tej-export` | Generate TEJ XML |
| `GET` | `/api/retenue-source/tej-logs` | TEJ export history |
| `GET` | `/api/retenue-source/stats` | RS statistics |

## Query Parameters (GET /api/retenue-source)
- `entity_type` — "offer" or "sale"
- `entity_id` — ID of the offer or sale
- `month` / `year` — filter by payment month/year
- `status` — "pending", "exported", "error"
- `supplier_tax_id` — filter by Matricule Fiscal
- `search` — search invoice number, supplier name, tax ID
- `page` / `limit` — pagination

## RS Type Codes
| Code | Rate | Description |
|------|------|-------------|
| `10` | 10% | Professional Fees / Services |
| `05` | 0.5% | Exported Services |
| `03` | 3% | Certain Professional Fees |
| `20` | 20% | Royalties / Interest |

## Setup
1. Run `Database/migration.sql` on your PostgreSQL database
2. Register in `ApplicationDbContext`:
   ```csharp
   public DbSet<RSRecord> RSRecords { get; set; }
   public DbSet<TEJExportLog> TEJExportLogs { get; set; }
   ```
3. Add configurations in `ApplyEntityConfigurations`:
   ```csharp
   new RSRecordConfiguration().Configure(modelBuilder);
   new TEJExportLogConfiguration().Configure(modelBuilder);
   ```
4. Register service in DI:
   ```csharp
   builder.Services.AddScoped<IRSService, RSService>();
   ```

## TEJ File Output
Generated XML files are saved to `../uploads/tej_exports/` and linked as Documents with `moduleType = "retenue_source"` and `category = "fiscal"`.
