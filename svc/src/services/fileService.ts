/**
 * File Service - Handles file operations for the application
 * Migration from CoreAPI's FileService.cs
 *
 * Supports:
 * - Multipart file uploads
 * - CSV parsing and import
 * - File deletion
 * - Public URL generation
 */

import { join, dirname, basename, extname } from "@std/path";

export interface UploadedFile {
  name: string;
  originalName: string;
  content: Uint8Array;
  contentType: string;
  size: number;
}

export interface CsvRow {
  [key: string]: string;
}

export interface CsvParseResult {
  headers: string[];
  rows: CsvRow[];
  totalRows: number;
}

export interface FileServiceConfig {
  webRootPath: string;
  baseUrl: string;
}

/**
 * File Service class for handling file operations
 */
export class FileService {
  private webRootPath: string;
  private baseUrl: string;

  constructor(config: FileServiceConfig) {
    this.webRootPath = config.webRootPath || "wwwroot";
    this.baseUrl = config.baseUrl || "/";
  }

  /**
   * Get the upload directory path for a tenant and user
   * Format: {webRootPath}/upload/{tenant}/file/U{userId}/
   */
  private getUploadDir(tenantCode: string, userId: string): string {
    const tenant = tenantCode || "default";
    const user = userId || "system";
    return join(this.webRootPath, "upload", tenant, "file", `U${user}`);
  }

  /**
   * Generate unique filename if file already exists
   */
  private async increaseFileName(filePath: string): Promise<string> {
    let currentPath = filePath;
    let index = 0;

    while (await this.fileExists(currentPath)) {
      const dir = dirname(filePath);
      const ext = extname(filePath);
      const nameWithoutExt = basename(filePath, ext);
      index++;
      currentPath = join(dir, `${nameWithoutExt}_${index}${ext}`);
    }

    return currentPath;
  }

  /**
   * Check if file exists
   */
  private async fileExists(path: string): Promise<boolean> {
    try {
      const info = await Deno.stat(path);
      return info.isFile;
    } catch (error) {
      if (error instanceof Deno.errors.NotFound) {
        return false;
      }
      throw error;
    }
  }

  /**
   * Ensure directory exists, create if not
   */
  private async ensureDirectoryExist(dirPath: string): Promise<void> {
    try {
      const dirInfo = await Deno.stat(dirPath);
      if (!dirInfo.isDirectory) {
        throw new Error(`Path exists but is not a directory: ${dirPath}`);
      }
    } catch (error) {
      if (error instanceof Deno.errors.NotFound) {
        await Deno.mkdir(dirPath, { recursive: true });
      } else {
        throw error;
      }
    }
  }

  /**
   * Convert absolute path to HTTP relative path
   */
  private getHttpPath(absolutePath: string): string {
    let relativePath = absolutePath.replace(this.webRootPath, "").replace(/\\/g, "/");
    if (!relativePath.startsWith("/")) {
      relativePath = "/" + relativePath;
    }
    return relativePath;
  }

  /**
   * Upload a file to the server
   * Saves to: wwwroot/upload/{tenant}/file/U{userId}/{filename}
   *
   * @param file - The uploaded file data
   * @param tenantCode - The tenant code
   * @param userId - The user ID
   * @param renameOnConflict - If true, renames file if it already exists (default: true)
   * @returns The HTTP path to the uploaded file
   */
  async upload(
    file: UploadedFile,
    tenantCode: string,
    userId: string,
    renameOnConflict: boolean = true
  ): Promise<string> {
    // Get upload directory
    const uploadDir = await this.getUploadDir(tenantCode, userId);

    // Ensure directory exists
    await this.ensureDirectoryExist(uploadDir);

    // Determine file path
    let filePath = join(uploadDir, file.originalName);

    // Handle filename conflicts
    if (renameOnConflict) {
      filePath = await this.increaseFileName(filePath);
    }

    // Write file to disk
    await Deno.writeFile(filePath, file.content);

    // Return HTTP path
    return this.getHttpPath(filePath);
  }

  /**
   * Import and parse a CSV file
   * Returns parsed CSV data as an array of objects
   *
   * @param file - The CSV file to parse
   * @param tenantCode - The tenant code (for potential storage)
   * @returns Parsed CSV data with headers and rows
   */
  async importCsv(file: UploadedFile, tenantCode: string): Promise<CsvParseResult> {
    // Decode CSV content from bytes to string
    const decoder = new TextDecoder("utf-8");
    const csvContent = decoder.decode(file.content);

    const lines = csvContent.split(/\r?\n/).filter(line => line.trim() !== "");

    if (lines.length === 0) {
      return {
        headers: [],
        rows: [],
        totalRows: 0,
      };
    }

    // Parse header row
    const headers = this.parseCsvLine(lines[0]);

    // Parse data rows
    const rows: CsvRow[] = [];
    for (let i = 1; i < lines.length; i++) {
      const values = this.parseCsvLine(lines[i]);
      if (values.length > 0) {
        const row: CsvRow = {};
        headers.forEach((header, index) => {
          row[header] = values[index] || "";
        });
        rows.push(row);
      }
    }

    return {
      headers,
      rows,
      totalRows: rows.length,
    };
  }

  /**
   * Parse a single CSV line, handling quoted values
   */
  private parseCsvLine(line: string): string[] {
    const result: string[] = [];
    let current = "";
    let inQuotes = false;

    for (let i = 0; i < line.length; i++) {
      const char = line[i];
      const nextChar = line[i + 1];

      if (inQuotes) {
        if (char === '"' && nextChar === '"') {
          // Escaped quote
          current += '"';
          i++; // Skip next quote
        } else if (char === '"') {
          // End quote
          inQuotes = false;
        } else {
          current += char;
        }
      } else {
        if (char === '"') {
          // Start quote
          inQuotes = true;
        } else if (char === ",") {
          // Field separator
          result.push(current.trim());
          current = "";
        } else {
          current += char;
        }
      }
    }

    // Add last field
    result.push(current.trim());

    return result;
  }

  /**
   * Delete a file from the server
   *
   * @param filePath - The HTTP path or absolute path to the file
   * @returns True if deleted successfully
   */
  async delete(filePath: string): Promise<boolean> {
    // Convert HTTP path to absolute path if needed
    const absolutePath = this.resolveFilePath(filePath);

    try {
      const info = await Deno.stat(absolutePath);
      if (info.isFile) {
        await Deno.remove(absolutePath);
        return true;
      }
      return false;
    } catch (error) {
      if (error instanceof Deno.errors.NotFound) {
        return false;
      }
      throw error;
    }
  }

  /**
   * Get the public URL for a file
   *
   * @param tenantCode - The tenant code
   * @param fileName - The filename (can include subdirectory)
   * @returns The public URL to access the file
   */
  getFileUrl(tenantCode: string, fileName: string): string {
    const basePath = join("upload", tenantCode || "default", "file", fileName);
    const normalizedPath = basePath.replace(/\\/g, "/");

    if (!normalizedPath.startsWith("/")) {
      return `/${normalizedPath}`;
    }
    return normalizedPath;
  }

  /**
   * Resolve file path from HTTP path or filename
   */
  private resolveFilePath(filePath: string): string {
    // If it's already an absolute path, return as is
    if (filePath.startsWith("/") || filePath.match(/^[a-zA-Z]:/)) {
      // It's a path - convert HTTP path to absolute
      if (filePath.startsWith("/")) {
        return join(this.webRootPath, filePath.substring(1));
      }
      return filePath;
    }

    // It's just a filename, construct path from baseUrl
    return join(this.webRootPath, filePath);
  }

  /**
   * Read file content as bytes
   *
   * @param filePath - The HTTP path or absolute path to the file
   * @returns File content as Uint8Array
   */
  async readFile(filePath: string): Promise<Uint8Array> {
    const absolutePath = this.resolveFilePath(filePath);
    return await Deno.readFile(absolutePath);
  }

  /**
   * Check if a file exists
   *
   * @param filePath - The HTTP path or absolute path to the file
   * @returns True if file exists
   */
  async fileExistsCheck(filePath: string): Promise<boolean> {
    const absolutePath = this.resolveFilePath(filePath);
    return await this.fileExists(absolutePath);
  }
}

/**
 * Create a default FileService instance
 */
export function createFileService(config?: Partial<FileServiceConfig>): FileService {
  return new FileService({
    webRootPath: config?.webRootPath || "wwwroot",
    baseUrl: config?.baseUrl || "/",
  });
}
