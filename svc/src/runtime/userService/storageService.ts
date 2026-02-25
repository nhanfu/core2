import path from "node:path";
import { unlink } from "node:fs/promises";
import type { FileLike, UserServiceContext } from "./types.ts";
import type { PatchVM, PatchDetail } from "../types.ts";
import { ensureDirectoryExists, fileExists, increaseFileName, readText, writeBinary } from "./utils.ts";
import { isNullOrWhiteSpace } from "../utils.ts";

/**
 * Supabase Storage configuration
 */
export interface SupabaseStorageConfig {
  supabaseUrl: string;
  supabaseKey: string;
  bucket?: string;
}

export class StorageService {
  private supabaseConfig?: SupabaseStorageConfig;

  constructor(private context: UserServiceContext) {}

  /**
   * Configure Supabase Storage
   */
  configureSupabase(config: SupabaseStorageConfig): void {
    this.supabaseConfig = config;
  }

  /**
   * Check if Supabase Storage is configured
   */
  isSupabaseConfigured(): boolean {
    return !!this.supabaseConfig?.supabaseUrl && !!this.supabaseConfig?.supabaseKey;
  }

  /**
   * Upload file to Supabase Storage
   */
  async uploadToSupabase(filePath: string, fileName: string, folder?: string): Promise<string> {
    if (!this.supabaseConfig) {
      throw new Error("Supabase Storage is not configured");
    }

    const bucket = this.supabaseConfig.bucket || "files";
    const pathParts = folder ? `${folder}/${fileName}` : fileName;
    
    const content = await readText(filePath);
    if (!content) {
      throw new Error("Failed to read file content");
    }

    const response = await fetch(
      `${this.supabaseConfig.supabaseUrl}/storage/v1/object/${bucket}/${pathParts}`,
      {
        method: "POST",
        headers: {
          "Content-Type": "application/octet-stream",
          Authorization: `Bearer ${this.supabaseConfig.supabaseKey}`,
        },
        body: content,
      }
    );

    if (!response.ok) {
      const error = await response.text();
      throw new Error(`Failed to upload to Supabase: ${error}`);
    }

    return `${this.supabaseConfig.supabaseUrl}/storage/v1/object/public/${bucket}/${pathParts}`;
  }

  /**
   * Download file from Supabase Storage
   */
  async downloadFromSupabase(filePath: string, destinationPath: string): Promise<void> {
    if (!this.supabaseConfig) {
      throw new Error("Supabase Storage is not configured");
    }

    const response = await fetch(filePath, {
      headers: {
        Authorization: `Bearer ${this.supabaseConfig.supabaseKey}`,
      },
    });

    if (!response.ok) {
      throw new Error("Failed to download from Supabase");
    }

    const arrayBuffer = await response.arrayBuffer();
    await writeBinary(destinationPath, arrayBuffer);
  }

  /**
   * Delete file from Supabase Storage
   */
  async deleteFromSupabase(publicUrl: string): Promise<boolean> {
    if (!this.supabaseConfig) {
      throw new Error("Supabase Storage is not configured");
    }

    // Extract path from public URL
    const bucket = this.supabaseConfig.bucket || "files";
    const pathMatch = publicUrl.match(/object\/public\/[^/]+\/(.+)$/);
    
    if (!pathMatch) {
      throw new Error("Invalid Supabase public URL");
    }

    const objectPath = pathMatch[1];
    
    const response = await fetch(
      `${this.supabaseConfig.supabaseUrl}/storage/v1/object/${bucket}/${objectPath}`,
      {
        method: "DELETE",
        headers: {
          Authorization: `Bearer ${this.supabaseConfig.supabaseKey}`,
        },
      }
    );

    return response.ok;
  }

  /**
   * List files in Supabase Storage bucket
   */
  async listSupabaseFiles(prefix?: string): Promise<{ name: string; id: string; updatedAt: string }[]> {
    if (!this.supabaseConfig) {
      throw new Error("Supabase Storage is not configured");
    }

    const bucket = this.supabaseConfig.bucket || "files";
    const url = new URL(`${this.supabaseConfig.supabaseUrl}/storage/v1/object/list/${bucket}`);
    
    if (prefix) {
      url.searchParams.set("prefix", prefix);
    }

    const response = await fetch(url.toString(), {
      method: "POST",
      headers: {
        "Content-Type": "application/json",
        Authorization: `Bearer ${this.supabaseConfig.supabaseKey}`,
      },
    });

    if (!response.ok) {
      throw new Error("Failed to list Supabase files");
    }

    return (await response.json()) as { name: string; id: string; updatedAt: string }[];
  }

  /**
   * Get signed URL for private file
   */
  async getSignedUrl(filePath: string, expiresIn: number = 3600): Promise<string> {
    if (!this.supabaseConfig) {
      throw new Error("Supabase Storage is not configured");
    }

    const bucket = this.supabaseConfig.bucket || "files";
    
    const response = await fetch(
      `${this.supabaseConfig.supabaseUrl}/storage/v1/object/sign/${bucket}/${filePath}`,
      {
        method: "POST",
        headers: {
          "Content-Type": "application/json",
          Authorization: `Bearer ${this.supabaseConfig.supabaseKey}`,
        },
        body: JSON.stringify({ expiresIn }),
      }
    );

    if (!response.ok) {
      throw new Error("Failed to get signed URL");
    }

    const data = await response.json();
    return `${this.supabaseConfig.supabaseUrl}/storage/v1${data.signedURL}`;
  }

  async postImageAsync(imageBase64: string, name = "Captured", reup = false): Promise<string> {
    const fileName = `${path.parse(name).name}${path.parse(name).ext}`;
    const uploadPath = this.getUploadPath(fileName, this.context.webRootPath);
    await ensureDirectoryExists(uploadPath);
    const finalPath = reup ? await increaseFileName(uploadPath) : uploadPath;
    const normalized = imageBase64.includes("base64,") ? imageBase64.split("base64,")[1] : imageBase64;
    const binary = Buffer.from(normalized, "base64");
    await writeBinary(finalPath, binary.buffer.slice(binary.byteOffset, binary.byteOffset + binary.byteLength));
    return this.getHttpPath(finalPath, this.context.webRootPath);
  }

  async postFileAsync(file: FileLike, reup = false): Promise<string> {
    const fileName = `${path.parse(file.name).name}${crypto.randomUUID()}${path.parse(file.name).ext}`;
    const uploadPath = this.getUploadPath(fileName, this.context.webRootPath);
    await ensureDirectoryExists(uploadPath);
    const finalPath = reup ? await increaseFileName(uploadPath) : uploadPath;
    const content = await file.arrayBuffer();
    await writeBinary(finalPath, content);
    return this.getHttpPath(finalPath, this.context.webRootPath);
  }

  async saveFileToUpload(file: FileLike, reup = false): Promise<string> {
    const fileName = `${path.parse(file.name).name}${crypto.randomUUID()}${path.parse(file.name).ext}`;
    const uploadPath = this.getUploadPath(fileName, this.context.webRootPath);
    await ensureDirectoryExists(uploadPath);
    const finalPath = reup ? await increaseFileName(uploadPath) : uploadPath;
    const content = await file.arrayBuffer();
    await writeBinary(finalPath, content);
    return finalPath;
  }

  async deleteFile(filePath: string): Promise<boolean> {
    const absolutePath = path.isAbsolute(filePath)
      ? filePath
      : path.join(this.context.webRootPath, filePath);
    const exists = await fileExists(absolutePath);
    if (!exists) return true;
    // Use Deno's remove if available (Deno runtime), otherwise fallback to Node.ts
    if (typeof Deno !== "undefined") {
      try {
        await Deno.remove(absolutePath);
      } catch {
        // Ignore errors
      }
      return true;
    }
    await unlink(absolutePath).catch(() => undefined);
    return true;
  }


  getUploadPath(fileName: string, webRootPath: string): string {
    return path.join(webRootPath, "upload", this.context.TenantCode || "system", "file", `U${this.context.UserId || "0"}`, fileName);
  }

  getHttpPath(filePath: string, webRootPath: string): string {
    const scheme = this.context.request?.scheme || "http";
    const host = this.context.request?.host || "localhost";
    return `${scheme}://${host}${filePath.replace(webRootPath, "").replace(/\\/g, "/")}`;
  }

  async parseCsvFile(filePath: string, table: string): Promise<PatchVM[]> {
    const content = await readText(filePath);
    if (!content) return [];
    const lines = content.split(/\r?\n/).filter((line) => !isNullOrWhiteSpace(line));
    if (lines.length === 0) return [];
    const headers = this.parseCsvLine(lines[0]);
    const patches: PatchVM[] = [];
    for (let i = 1; i < lines.length; i += 1) {
      const values = this.parseCsvLine(lines[i]);
      const changes: PatchDetail[] = values.map((value, index) => ({
        Field: headers[index] || "",
        Value: value,
      }));
      patches.push({ Table: table, Changes: changes });
    }
    return patches;
  }

  private parseCsvLine(line: string): string[] {
    const result: string[] = [];
    let current = "";
    let inQuotes = false;
    for (let i = 0; i < line.length; i += 1) {
      const ch = line[i];
      if (ch === '"') {
        inQuotes = !inQuotes;
        continue;
      }
      if (ch === "," && !inQuotes) {
        result.push(current);
        current = "";
      } else {
        current += ch;
      }
    }
    result.push(current);
    return result;
  }
}
