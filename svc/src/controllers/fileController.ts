/**
 * File Controller - Handles file upload/download endpoints
 * Migration from CoreAPI's FileUploadController
 *
 * Endpoints:
 * - POST /api/fileUpload/file - Upload a file (multipart form data)
 * - GET /api/fileUpload/{path} - Download a file
 */

import { Context } from "https://deno.land/x/oak@v17.1.3/mod.ts";
import { createFileService, UploadedFile } from "../services/fileService.ts";
import type { UserContext } from "../types/interfaces.ts";

// Get configuration from environment
const WEB_ROOT_PATH = Deno.env.get("WEB_ROOT_PATH") || "wwwroot";
const BASE_URL = Deno.env.get("BASE_URL") || "/";

// Create file service instance
const fileService = createFileService({
  webRootPath: WEB_ROOT_PATH,
  baseUrl: BASE_URL,
});

/**
 * Extract user context from request state
 * Assumes JWT middleware has already populated the state with user info
 */
function getUserContext(ctx: Context): UserContext {
  const state = ctx.state as any;
  if (state.user) {
    return state.user;
  }
  // Return default context if not authenticated (for testing)
  return {
    userId: "system",
    tenantCode: Deno.env.get("TENANT_CODE") || "default",
    env: Deno.env.get("ENV") || "production",
    connKey: Deno.env.get("CONN_KEY") || "default",
    roles: [],
    roleNames: [],
    userName: "system",
    email: "",
    fullName: "System",
  };
}

/**
 * Parse multipart form data from request
 * Handles file uploads with metadata
 */
async function parseMultipartFormData(
  ctx: Context
): Promise<{ files: UploadedFile[]; fields: Record<string, string> }> {
  const contentType = ctx.request.headers.get("content-type") || "";

  if (!contentType.includes("multipart/form-data")) {
    throw new Error("Content-Type must be multipart/form-data");
  }

  // For Oak v17, use the Body object properly
  const body = await ctx.request.body;
  const files: UploadedFile[] = [];
  const fields: Record<string, string> = {};

  // Handle multipart form data - use type assertion for older Oak patterns
  const bodyAny = body as any;
  if (bodyAny.type === "form-data" && bodyAny.value) {
    const value = await bodyAny.value;

    for await (const part of value) {
      if (part.type === "file") {
        // Read file content
        const content = await part.content.read();

        // Determine content type from filename
        const filename = part.filename || "unknown";
        const contentType = getContentTypeFromFilename(filename);

        files.push({
          name: part.name || "file",
          originalName: filename,
          content: content,
          contentType,
          size: content.length,
        });
      } else if (part.type === "text") {
        // Handle text fields
        const content = await part.content.read();
        const decoder = new TextDecoder("utf-8");
        const value = decoder.decode(content);
        fields[part.name || ""] = value;
      }
    }
  }

  return { files, fields };
}

/**
 * Determine content type from file extension
 */
function getContentTypeFromFilename(filename: string): string {
  const ext = filename.split(".").pop()?.toLowerCase() || "";
  const mimeTypes: Record<string, string> = {
    "jpg": "image/jpeg",
    "jpeg": "image/jpeg",
    "png": "image/png",
    "gif": "image/gif",
    "bmp": "image/bmp",
    "webp": "image/webp",
    "svg": "image/svg+xml",
    "pdf": "application/pdf",
    "doc": "application/msword",
    "docx": "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
    "xls": "application/vnd.ms-excel",
    "xlsx": "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
    "ppt": "application/vnd.ms-powerpoint",
    "pptx": "application/vnd.openxmlformats-officedocument.presentationml.presentation",
    "txt": "text/plain",
    "csv": "text/csv",
    "json": "application/json",
    "xml": "application/xml",
    "zip": "application/zip",
    "rar": "application/x-rar-compressed",
    "7z": "application/x-7z-compressed",
    "tar": "application/x-tar",
    "gz": "application/gzip",
    "mp3": "audio/mpeg",
    "wav": "audio/wav",
    "mp4": "video/mp4",
    "avi": "video/x-msvideo",
    "mov": "video/quicktime",
    "webm": "video/webm",
  };

  return mimeTypes[ext] || "application/octet-stream";
}

/**
 * POST /api/fileUpload/file
 * Upload a file to the server
 * Content-Type: multipart/form-data
 *
 * Form fields:
 * - file: The file to upload (required)
 * - renameOnConflict: Whether to rename file if it exists (optional, default: true)
 *
 * Returns:
 * - success: HTTP path to the uploaded file
 * - fileName: Original filename
 * - size: File size in bytes
 * - contentType: MIME type
 */
export async function uploadFile(ctx: Context): Promise<void> {
  try {
    // Check if request has content
    if (!ctx.request.hasBody) {
      ctx.response.status = 400;
      ctx.response.body = {
        success: false,
        message: "Request body is empty",
      };
      return;
    }

    // Parse multipart form data
    let files: UploadedFile[] = [];
    let fields: Record<string, string> = {};

    try {
      const parsed = await parseMultipartFormData(ctx);
      files = parsed.files;
      fields = parsed.fields;
    } catch (error) {
      ctx.response.status = 400;
      ctx.response.body = {
        success: false,
        message: error instanceof Error ? error.message : "Failed to parse multipart form data",
      };
      return;
    }

    // Check if file was provided
    if (files.length === 0) {
      ctx.response.status = 400;
      ctx.response.body = {
        success: false,
        message: "No file provided in request",
      };
      return;
    }

    // Get user context
    const userContext = getUserContext(ctx);

    // Get rename option (default: true)
    const renameOnConflict = fields.renameOnConflict !== "false";

    // Upload the first file
    const file = files[0];
    const httpPath = await fileService.upload(
      file,
      userContext.tenantCode,
      userContext.userId,
      renameOnConflict
    );

    // Return success response
    ctx.response.status = 200;
    ctx.response.body = {
      success: true,
      message: "File uploaded successfully",
      data: {
        path: httpPath,
        fileName: file.originalName,
        size: file.size,
        contentType: file.contentType,
      },
    };
  } catch (error) {
    console.error("Error uploading file:", error);
    ctx.response.status = 500;
    ctx.response.body = {
      success: false,
      message: error instanceof Error ? error.message : "Failed to upload file",
    };
  }
}

/**
 * GET /api/fileUpload/{path}
 * Download a file from the server
 *
 * Path parameters:
 * - path: The relative path to the file (e.g., "default/file/U123/document.pdf")
 *
 * Query parameters:
 * - inline: If true, display inline instead of download (optional)
 *
 * Returns:
 * - File content with appropriate headers
 */
export async function downloadFile(ctx: Context): Promise<void> {
  try {
    // Get the file path from URL - extract from path after /api/fileUpload/
    const urlPath = ctx.request.url.pathname;
    const path = urlPath.replace("/api/fileUpload/", "");

    if (!path) {
      ctx.response.status = 400;
      ctx.response.body = {
        success: false,
        message: "File path is required",
      };
      return;
    }

    // Check if file exists
    const exists = await fileService.fileExistsCheck(path);

    if (!exists) {
      ctx.response.status = 404;
      ctx.response.body = {
        success: false,
        message: "File not found",
      };
      return;
    }

    // Read file content
    const content = await fileService.readFile(path);

    // Determine content type from path
    const contentType = getContentTypeFromFilename(path);

    // Check if should display inline or as attachment
    const queryParams = ctx.request.url.searchParams;
    const inline = queryParams.get("inline") === "true";

    // Extract filename from path
    const filename = path.split("/").pop() || "download";

    // Set response headers
    if (inline) {
      ctx.response.headers.set("Content-Type", contentType);
      ctx.response.headers.set("Content-Disposition", `inline; filename="${filename}"`);
    } else {
      ctx.response.headers.set("Content-Type", contentType);
      ctx.response.headers.set("Content-Disposition", `attachment; filename="${filename}"`);
    }

    ctx.response.headers.set("Content-Length", content.length.toString());
    ctx.response.headers.set("Cache-Control", "private, max-age=3600");

    // Send file content
    ctx.response.status = 200;
    ctx.response.body = content;
  } catch (error) {
    console.error("Error downloading file:", error);
    ctx.response.status = 500;
    ctx.response.body = {
      success: false,
      message: error instanceof Error ? error.message : "Failed to download file",
    };
  }
}

/**
 * DELETE /api/fileUpload/{path}
 * Delete a file from the server
 *
 * Path parameters:
 * - path: The relative path to the file
 *
 * Returns:
 * - success: Whether the file was deleted
 */
export async function deleteFile(ctx: Context): Promise<void> {
  try {
    // Get the file path from URL - extract from path after /api/fileUpload/
    const urlPath = ctx.request.url.pathname;
    const path = urlPath.replace("/api/fileUpload/", "");

    if (!path) {
      ctx.response.status = 400;
      ctx.response.body = {
        success: false,
        message: "File path is required",
      };
      return;
    }

    const deleted = await fileService.delete(path);

    if (deleted) {
      ctx.response.status = 200;
      ctx.response.body = {
        success: true,
        message: "File deleted successfully",
      };
    } else {
      ctx.response.status = 404;
      ctx.response.body = {
        success: false,
        message: "File not found",
      };
    }
  } catch (error) {
    console.error("Error deleting file:", error);
    ctx.response.status = 500;
    ctx.response.body = {
      success: false,
      message: error instanceof Error ? error.message : "Failed to delete file",
    };
  }
}

