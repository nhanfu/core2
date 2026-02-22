import type { Component, Feature, FeaturePolicy, RuntimeContext } from "./types.js";

export type PermissionOperation = "read" | "write" | "delete" | "deactivate" | "export";

const roleMatches = (roleId: string | undefined, roleIds: string[] | undefined): boolean => {
  if (!roleId || !roleIds) return false;
  return roleIds.includes(roleId);
};

const componentAllows = (component: Component | undefined, operation: PermissionOperation): boolean => {
  if (!component) return true;
  switch (operation) {
    case "read":
      return component.CanRead !== false && component.CanReadAll !== false;
    case "write":
      return component.CanWrite !== false && component.CanWriteAll !== false;
    case "delete":
      return component.CanDelete !== false && component.CanDeleteAll !== false;
    case "deactivate":
      return component.CanDeactivate !== false && component.CanDeactivateAll !== false;
    case "export":
      return component.CanExport !== false;
    default:
      return true;
  }
};

const policyAllows = (policy: FeaturePolicy, operation: PermissionOperation): boolean => {
  switch (operation) {
    case "read":
      return !!(policy.CanRead || policy.CanReadAll);
    case "write":
      return !!(policy.CanWrite || policy.CanWriteAll);
    case "delete":
      return !!(policy.CanDelete || policy.CanDeleteAll);
    case "deactivate":
      return !!(policy.CanDeactivate || policy.CanDeactivateAll);
    case "export":
      return !!policy.CanExport;
    default:
      return false;
  }
};

export const hasPermission = (
  feature: Feature | undefined,
  component: Component | undefined,
  context: RuntimeContext | undefined,
  operation: PermissionOperation,
  bypass = false,
): boolean => {
  if (bypass) return true;
  if (!componentAllows(component, operation)) return false;
  if (feature?.IsPublic) return true;
  const policies = feature?.FeaturePolicies || [];
  if (!policies.length) return false;
  return policies.some((policy) => roleMatches(policy.RoleId, context?.roleIds) && policyAllows(policy, operation));
};
