const fs = require('fs');
const path = require('path');

function clean(obj) {
    const excludeKeys = new Set(["ComponentGroupId"]);
    if (Array.isArray(obj)) {
        return obj
            .map(clean)
            .filter(v => v !== undefined && !(Array.isArray(v) && v.length === 0));
    } else if (obj && typeof obj === 'object') {
        const result = {};
        for (const [key, value] of Object.entries(obj)) {
            if (excludeKeys.has(key)) continue;
            const cleaned = clean(value);
            if (
                cleaned !== null &&
                cleaned !== false &&
                cleaned !== undefined &&
                cleaned !== "" &&
                !(Array.isArray(cleaned) && cleaned.length === 0)
            ) {
                result[key] = cleaned;
            }
        }
        return result;
    } else {
        return obj;
    }
}

function cleanFile(filePath) {
    const raw = fs.readFileSync(filePath, 'utf8');
    const json = JSON.parse(raw);
    const cleaned = clean(json);
    fs.writeFileSync(filePath, JSON.stringify(cleaned, null, 4));
    console.log('Cleaned JSON written to', filePath);
}

function cleanFolder(folderPath) {
    fs.readdirSync(folderPath).forEach(file => {
        if (file.endsWith('.json')) {
            const filePath = path.join(folderPath, file);
            cleanFile(filePath);
        }
    });
}

// Usage: node cleanJson.js <fileOrFolderPath> [--folder]
const [,, targetPath, flag] = process.argv;
if (!targetPath) {
    console.error('Usage: node cleanJson.js <fileOrFolderPath> [--folder]');
    process.exit(1);
}

if (flag === '--folder') {
    cleanFolder(targetPath);
} else {
    cleanFile(targetPath);
}