# Implementation TODO

## Phase 1 - HIGH Priority

### 1. Supabase PostgreSQL Adapter
- [x] Create `svc/src/runtime/adapters/supabaseAdapter.ts`

### 2. Supabase Auth Integration  
- [x] Create `svc/src/runtime/adapters/authAdapter.ts`

### 3. Supabase Storage Service Update
- [ ] Modify `svc/src/runtime/userService/storageService.ts`

### 4. Script Execution Engine
- [ ] Create `svc/src/runtime/scriptRunner.ts`
- [ ] Modify `svc/src/runtime/types.ts`
- [ ] Modify `svc/src/runtime/userService/queryService.ts`

## Phase 2 - MEDIUM Priority

### 5. Event System
- [ ] Create `svc/src/runtime/eventBus.ts`
- [ ] Create `svc/src/runtime/componentEvents.ts`
- [ ] Modify `svc/src/runtime/types.ts`

### 6. Test Expansion
- [ ] Create `svc/test/scriptRunner.test.ts`
- [ ] Create `svc/test/eventBus.test.ts`
- [ ] Create `svc/test/adapter.test.ts`
- [ ] Create `svc/test/integration.test.ts`
