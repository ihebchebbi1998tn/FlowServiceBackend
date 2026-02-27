# Website Builder Module — Architecture

## Executive Summary

The Website Builder module is **production-ready** with clean separation of concerns, strong TypeScript typing, comprehensive validation, and a scalable component-based design. The codebase follows React best practices and is **fully backend-ready** with abstracted storage, image, and content service layers.

---

## ✅ Architecture Highlights

### 1. **Type System (Excellent)**
- Strong TypeScript types in `types/` folder
- Proper separation: `component.ts`, `site.ts`, `animation.ts`, `editor.ts`, `i18n.ts`, `shared.ts`
- `validation.ts` — Zod schemas for runtime validation
- `BuilderComponent` interface is JSON-serializable and backend-ready
- All shared interfaces (CTAButton, NavLink, FormField, etc.) centralized

### 2. **Validation Layer (New)**
- Comprehensive Zod schemas for all data types
- Runtime validation with `validate()` and `validateOrThrow()`
- Input sanitization helpers for XSS prevention
- Form-specific validation (contact forms, newsletter subscriptions)
- API input/output validation schemas

### 3. **State Management (Excellent)**
- **`SiteContext`** — Centralized site state with auto-save and optimistic updates
- `useEditorState` — Component CRUD, history, clipboard
- `useSiteActions` — Site-level operations (pages, theme, SEO)
- Ref-based approach prevents stale closures

### 4. **Backend Readiness (Excellent)**
- **`storageProvider.ts`** — Abstracted storage interface
  - `IStorageProvider` interface for any backend
  - `LocalStorageProvider` — Default implementation
  - Easy to swap with API/Supabase provider
- **`imageService.ts`** — Abstracted image upload/management
  - `IImageProvider` interface for cloud storage
  - `LocalImageProvider` — Base64 data URL storage (development)
  - Ready for Supabase Storage, S3, Cloudinary
- **`contentService.ts`** — Content operations
  - Translation extraction and application
  - Content search across sites
  - Component statistics and merging
- **`siteService.ts`** — Site CRUD operations
- **`templateService.ts`** — Template management

### 5. **Sharing & Publishing**
- `utils/sharing.ts` — URL generation, embed codes, social sharing
- `ShareSiteDialog` — Full-featured share dialog
- Export/Import JSON functionality
- QR code generation

### 6. **Action System**
- `ComponentAction` type — Navigate, scroll, email, phone, download, etc.
- `useActionHandler` hook — Executes actions
- `ActionButton`/`ActionLink` — Reusable action-enabled components

### 7. **Block System (80+ Components)**
- `blockRegistry.ts` — Centralized type-to-component mapping
- Category-organized blocks in `renderer/blocks/`
- Category-organized palette in `utils/palette/`

---

## Folder Structure

```
website-builder/
├── components/
│   ├── editor/              # Editor UI components
│   │   ├── presets/         # Style preset components
│   │   ├── property-editors/# Property input components
│   │   ├── ImageUploader.tsx
│   │   └── ActionConfigEditor.tsx
│   ├── renderer/
│   │   ├── blocks/          # 80+ block components
│   │   ├── ActionButton.tsx # Action-enabled button
│   │   ├── blockRegistry.ts # Type → Component mapping
│   │   └── ComponentRenderer.tsx
│   ├── ShareSiteDialog.tsx  # Sharing modal
│   ├── TemplateGalleryPage.tsx
│   └── SiteManager.tsx      # Site listing/management
├── config/
│   └── propertyConfig.ts    # Property metadata & defaults
├── context/
│   ├── index.ts
│   └── SiteContext.tsx      # Centralized site state
├── hooks/
│   ├── index.ts             # Barrel export
│   ├── useActionHandler.ts  # Action execution
│   ├── useEditorState.ts    # Component state
│   ├── useSiteActions.ts    # Site operations
│   └── ...
├── pages/
│   ├── SiteEditor.tsx       # Main editor page
│   └── PublicWebsitePage.tsx# Public site viewer
├── services/
│   ├── index.ts             # Barrel export
│   ├── storageProvider.ts   # Storage abstraction
│   ├── imageService.ts      # Image upload abstraction
│   ├── contentService.ts    # Content operations
│   ├── siteService.ts       # Site CRUD
│   └── templateService.ts   # Templates
├── types/
│   ├── index.ts             # Barrel export
│   ├── component.ts         # BuilderComponent
│   ├── site.ts              # WebsiteSite, SitePage
│   ├── shared.ts            # Shared interfaces
│   ├── animation.ts         # Animation settings
│   ├── editor.ts            # Editor state
│   ├── i18n.ts              # Language constants
│   └── validation.ts        # Zod validation schemas
└── utils/
    ├── palette/             # Component palette by category
    ├── templates/           # Site templates
    ├── sharing.ts           # Share/export utilities
    ├── imageUtils.ts        # Image validation/reading
    ├── themeUtils.ts        # Theme styling utilities
    ├── heroUtils.ts         # Hero component helpers
    └── formSubmissions.ts   # Form handling
```

---

## Key Interfaces

### BuilderComponent (Serializable)
```typescript
interface BuilderComponent {
  id: string;
  type: ComponentType;
  label: string;
  props: Record<string, any>;
  styles: ResponsiveStyles;
  animation?: AnimationSettings;
  children?: BuilderComponent[];
  hidden?: Partial<Record<DeviceView, boolean>>;
}
```

### WebsiteSite
```typescript
interface WebsiteSite {
  id: string;
  name: string;
  slug: string;
  description?: string;
  favicon?: string;
  theme: SiteTheme;
  pages: SitePage[];
  published: boolean;
  publishedAt?: string;
  createdAt: string;
  updatedAt: string;
  defaultLanguage?: string;
  languages?: SiteLanguage[];
}
```

### IStorageProvider (Backend Abstraction)
```typescript
interface IStorageProvider {
  // Site CRUD
  listSites(options?: ListSitesOptions): Promise<StorageResult<WebsiteSite[]>>;
  getSite(siteId: string): Promise<StorageResult<WebsiteSite>>;
  createSite(input: CreateSiteInput): Promise<StorageResult<WebsiteSite>>;
  updateSite(input: UpdateSiteInput): Promise<StorageResult<WebsiteSite>>;
  deleteSite(siteId: string): Promise<StorageResult<void>>;
  
  // Page operations
  addPage(siteId: string, page: Omit<SitePage, 'id'>): Promise<StorageResult<SitePage>>;
  updatePage(siteId: string, pageId: string, updates: Partial<SitePage>): Promise<StorageResult<SitePage>>;
  deletePage(siteId: string, pageId: string): Promise<StorageResult<void>>;
  
  // Publishing
  publishSite(siteId: string): Promise<StorageResult<{ url: string }>>;
  unpublishSite(siteId: string): Promise<StorageResult<void>>;
}
```

### IImageProvider (Image Upload Abstraction)
```typescript
interface IImageProvider {
  upload(file: File, options?: ImageUploadOptions): Promise<ServiceResult<ImageUploadResult>>;
  uploadFromUrl(url: string, options?: ImageUploadOptions): Promise<ServiceResult<ImageUploadResult>>;
  delete(publicIdOrUrl: string): Promise<ServiceResult<void>>;
  getTransformedUrl(url: string, options: ImageTransformOptions): string;
  list(folder?: string): Promise<ServiceResult<ImageUploadResult[]>>;
}
```

---

## Backend Integration

### Option 1: Swap Storage Provider
```typescript
import { setStorageProvider } from './services';

class ApiStorageProvider implements IStorageProvider {
  async listSites(options?: ListSitesOptions) {
    const response = await fetch('/api/sites');
    const data = await response.json();
    return { data, error: null, success: true };
  }
  // ... implement other methods
}

setStorageProvider(new ApiStorageProvider());
```

### Option 2: Swap Image Provider
```typescript
import { setImageProvider } from './services';

class SupabaseImageProvider implements IImageProvider {
  async upload(file: File, options?: ImageUploadOptions) {
    const { data, error } = await supabase.storage
      .from('images')
      .upload(`uploads/${file.name}`, file);
    // ...
  }
}

setImageProvider(new SupabaseImageProvider());
```

### Option 3: Use SiteContext
```tsx
import { SiteProvider, useSiteContext } from './context';

function App() {
  return (
    <SiteProvider siteId="abc123" autoSaveDelay={1000}>
      <Editor />
    </SiteProvider>
  );
}

function Editor() {
  const { site, updateComponents, publishSite } = useSiteContext();
  // All operations are auto-saved
}
```

---

## Validation

### Validate User Input
```typescript
import { validate, contactFormDataSchema, sanitizeInput } from './types';

// Validate form data
const result = validate(contactFormDataSchema, formData);
if (!result.success) {
  console.error(result.errors);
}

// Sanitize text input
const safeText = sanitizeInput(userInput, 1000);
```

### Validate Site Data
```typescript
import { websiteSiteSchema, validateOrThrow } from './types';

// Will throw if invalid
const validSite = validateOrThrow(websiteSiteSchema, siteData);
```

---

## Content Operations

### Search Content
```typescript
import { searchContent } from './services';

const results = await searchContent('contact', { siteId: 'abc123' });
// Returns matching text across all components
```

### Extract Translations
```typescript
import { extractTranslatableContent, applyTranslations } from './services';

// Get all translatable text
const texts = extractTranslatableContent(page.components);

// Apply translations
const translated = applyTranslations(components, {
  'comp-1.heading': 'Bienvenue',
  'comp-1.subheading': 'Bonjour le monde',
});
```

---

## Performance Recommendations

1. **Use SiteContext for auto-save** — Debounced saves prevent excessive writes
2. **React.memo for blocks** — Prevent unnecessary re-renders
3. **Virtualize long lists** — For component palettes
4. **Lazy load blocks** — Dynamic imports for rarely-used blocks
5. **Optimize images** — Use `imageService` with `maxWidth`/`maxHeight`

---

## Security Considerations

1. **Input validation** — Use Zod schemas for all user input
2. **XSS prevention** — Use `sanitizeInput()` for text content
3. **URL validation** — Use `urlSchema` for links
4. **Email validation** — Use `emailSchema` for email fields
5. **Phone validation** — Use `phoneSchema` for phone numbers

---

## Conclusion

| Area | Status | Notes |
|------|--------|-------|
| Type Safety | ✅ Excellent | Full TypeScript coverage |
| Validation | ✅ Complete | Zod schemas for all types |
| State Management | ✅ Excellent | SiteContext + hooks |
| Backend Ready | ✅ Excellent | Storage, Image, Content providers |
| Image Handling | ✅ Complete | Abstracted upload service |
| Content Ops | ✅ Complete | Search, translations, cloning |
| Sharing | ✅ Complete | URLs, embeds, social, export |
| Action System | ✅ Complete | Navigation, links, actions |
| Component Library | ✅ 80+ blocks | Well-organized |
| Performance | ✅ Good | Auto-save, memoization |
| Security | ✅ Good | Validation, sanitization |
