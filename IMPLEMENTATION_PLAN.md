# Implementation Plan for CoreJS Roadmap

## Current State Analysis

### ✅ Already Implemented (Phase 1 - In Progress)
1. **Metadata Store** (`svc/src/runtime/metadataStore.ts`)
   - YAML (.yaml, .yml) and JSON file support
   - Tenant-based feature loading

2. **Permission System** (`svc/src/runtime/permission.ts`)
   - Role-based access control (RBAC)
   - Component and feature-level policies

3. **SQL Builder** (`svc/src/runtime/sql.ts`)
   - INSERT/UPDATE patch generation
   - PostgreSQL quoting support

4. **UserService Core** (`svc/src/runtime/userService.ts`)
   - Main runtime entry point
   - Feature, Patch, Query, Storage services

5. **Test Suite**
   - Permission tests
   - MetadataStore tests
   - SQL tests
   - UserService tests

### ❌ Missing / Needs Implementation

| Component | Priority | Status |
|-----------|----------|--------|
| Supabase PostgreSQL Adapter | HIGH | Not implemented |
| Supabase Storage Service | HIGH | Not implemented |
| Supabase Auth Integration | HIGH | Not implemented |
| Script Execution Engine (sandboxed) | HIGH | Basic (uses `new Function`) |
| Advanced Test Coverage (80%) | MEDIUM | Partial |

> **Note:** Data Binding Engine and UI Component Renderer are already implemented in the frontend (`frontend/` folder).

---

## Phase 1 Implementation Plan

### Step 1: Supabase Integration (Priority: HIGH)

#### 1.1 PostgreSQL Adapter for Supabase
```
typescript
// Create: svc/src/runtime/adapters/supabaseAdapter.ts
// - Implement DataAdapter interface
// - Use Bun.SQL with PostgreSQL connection
// - Support Supabase connection string format
```

**Files to create:**
- `svc/src/runtime/adapters/supabaseAdapter.ts`

**Dependencies needed:**
- None (use built-in `Bun.SQL`)

#### 1.2 Supabase Storage Service
```
typescript
// Update: svc/src/runtime/userService/storageService.ts
// - Add Supabase Storage client
// - Use S3-compatible API
```

**Files to modify:**
- `svc/src/runtime/userService/storageService.ts`

#### 1.3 Supabase Auth Integration
```
typescript
// Create: svc/src/runtime/adapters/authAdapter.ts
// - JWT token validation
// - User session management
// - Role/claim extraction
```

**Files to create:**
- `svc/src/runtime/adapters/authAdapter.ts`

---

### Step 2: Script Execution Engine (Priority: HIGH)

#### Current Issue
Uses `new Function()` which is insecure and lacks features.

#### Solution
Create a proper script runner with:
- Sandboxed execution (vm2 or isolated context)
- Safe API access
- Async function support

**Files to create:**
- `svc/src/runtime/scriptRunner.ts`

**Files to modify:**
- `svc/src/runtime/types.ts` (enhance ScriptRunner interface)
- `svc/src/runtime/userService/queryService.ts`

---

### Step 3: Component Event System (Priority: MEDIUM)

**Files to create:**
- `svc/src/runtime/eventBus.ts` - Event emission/handling
- `svc/src/runtime/componentEvents.ts` - Built-in event handlers

**Files to modify:**
- `svc/src/runtime/types.ts` - Add Event definitions

---

### Step 4: Test Coverage Expansion (Priority: MEDIUM)

**Current:** Basic tests for permission, metadata, SQL

**Target:** 80% coverage

**Tests to add:**
- ScriptRunner tests
- EventBus tests
- Supabase adapter tests (mock)
- Integration tests for query flow
- Patch save flow tests

**Files to create:**
- `svc/test/scriptRunner.test.ts`
- `svc/test/eventBus.test.ts`
- `svc/test/adapter.test.ts`
- `svc/test/integration.test.ts`

---

### Step 5: Data Binding & Component Rendering (Priority: MEDIUM)

**Note:** This is more complex and may extend into Phase 2

**Files to create:**
- `svc/src/runtime/dataBinding.ts` - Reactive data binding
- `svc/src/runtime/componentRenderer.ts` - Component tree rendering
- `svc/src/runtime/viewModel.ts` - ViewModel management

---

## Phase 2 Preparation (AI System)

This phase requires:
1. CLI tool integration (to be determined)
2. YAML generation templates
3. Deployment automation
4. Billing system

**Note:** These should be planned after Phase 1 completion.

---

## Recommended Implementation Order

```
Phase 1 Execution Order:
├── 1. Supabase PostgreSQL Adapter
│   └── Create supabaseAdapter.ts
├── 2. Auth Integration  
│   └── Create authAdapter.ts
├── 3. Storage Service Update
│   └── Modify storageService.ts
├── 4. Script Execution Engine
│   └── Create scriptRunner.ts
├── 5. Event System
│   ├── Create eventBus.ts
│   └── Create componentEvents.ts
└── 6. Test Expansion
    ├── Add scriptRunner.test.ts
    ├── Add adapter.test.ts
    └── Add integration.test.ts
```

---

## Skills & Tools Required

| Skill/Tool | Purpose | Priority |
|------------|---------|----------|
| Bun.SQL | PostgreSQL connectivity | HIGH |
| TypeScript | Type-safe runtime | HIGH |
| Bun test framework | TDD approach | HIGH |
| Supabase JS SDK | Auth & Storage (optional) | MEDIUM |
| YAML parsing | Already using `yaml` package | ✅ |

---

## Suggested Package.json Updates

```
json
{
  "dependencies": {
    "yaml": "^2.5.0",
    "supabase": "^2.0.0"
  },
  "devDependencies": {
    "bun-types": "^1.0.0",
    "typescript": "^5.4.5"
  }
}
```

---

## Next Steps

1. **Confirm this plan** with stakeholders
2. **Start with Step 1**: Create Supabase adapter
3. **Iterate**: Build incrementally, test-driven
4. **Review**: Weekly progress checks against roadmap milestones
