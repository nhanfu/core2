const fs = require('fs');
const path = require('path');

function clean(obj) {
    const excludeKeys = new Set(["GroupTypeId", "Active", "ReportTypeId", "ComponentGroupId", 
        "InsertedBy", "InsertedDate", "UpdatedBy", "UpdatedDate"]);
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

// Build nested tree from flat array using ParentId, sort children by Order, remove ParentId
function buildTree(items) {
    const map = {};
    items.forEach(item => {
        map[item.Id] = { ...item, children: [] };
    });
    const roots = [];
    items.forEach(item => {
        if (item.ParentId != null) {
            if (map[item.ParentId]) {
                map[item.ParentId].children.push(map[item.Id]);
            }
        } else {
            roots.push(map[item.Id]);
        }
    });
    // Recursively sort children and remove ParentId
    function processNode(node) {
        if (node.children && node.children.length > 0) {
            node.children.sort((a, b) => (a.Order ?? 0) - (b.Order ?? 0));
            node.children.forEach(processNode);
        }
        delete node.ParentId;
        return node;
    }
    roots.sort((a, b) => (a.Order ?? 0) - (b.Order ?? 0));
    roots.forEach(processNode);
    return roots;
}

function cleanFile(filePath) {
    const raw = fs.readFileSync(filePath, 'utf8');
    const json = JSON.parse(raw);
    const cleaned = clean(json);
    // If cleaned is an array and has ParentId, build tree
    if (Array.isArray(cleaned) && cleaned.some(x => x.ParentId !== undefined)) {
        const tree = buildTree(cleaned);
        fs.writeFileSync(filePath, JSON.stringify(tree, null, 4));
    } else {
        fs.writeFileSync(filePath, JSON.stringify(cleaned, null, 4));
    }
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