import path from "path";
import { unlink } from "fs/promises";
import type { FileLike, UserServiceContext } from "./types.js";
import type { PatchVM, PatchDetail } from "../types.js";
import { ensureDirectoryExists, fileExists, increaseFileName, isEmpty, readText, writeBinary } from "./utils.js";
import { isNullOrWhiteSpace } from "../utils.js";

export class StorageService {
  constructor(private context: UserServiceContext) {}

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
    if (typeof Bun !== "undefined") {
      await Bun.write(absolutePath, "");
      await unlink(absolutePath).catch(() => undefined);
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
