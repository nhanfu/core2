# Svc Deno Conversion TODO

## Phase 1: Configuration Files
- [ ] Create deno.json for Deno configuration
- [ ] Update package.json - remove Bun-specific scripts and dependencies
- [ ] Update tsconfig.json - replace bun-types with Deno types

## Phase 2: Source Code Updates
- [ ] Update metadataStore.ts - Replace Bun.file with Deno APIs
- [ ] Update userService/utils.ts - Replace Bun APIs with Deno APIs
- [ ] Update userService/storageService.ts - Replace Bun APIs with Deno APIs

## Phase 3: Test Updates
- [ ] Update test/userService.test.ts - Convert bun:test to Deno test
- [ ] Update test/metadataStore.test.ts - Convert bun:test to Deno test
- [ ] Update test/permission.test.ts - Convert bun:test to Deno test
- [ ] Update test/sql.test.ts - Convert bun:test to Deno test

## Phase 4: Cleanup
- [ ] Remove bun.lock file
- [ ] Verify all changes compile with `deno check`
