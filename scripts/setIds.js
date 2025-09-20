const fs = require('fs');
const path = require('path');

function readJson(filePath) {
    const data = fs.readFileSync(filePath, 'utf8');
    return JSON.parse(data);
}

function writeJson(filePath, obj) {
    fs.writeFileSync(filePath, JSON.stringify(obj, null, 4), 'utf8');
}

function isGuid(id) {
    if (!id || typeof id !== 'string') return false;
    // match RFC4122 v4-ish GUID format and some placeholder variants
    return /^[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12}$/.test(id) || /-0000-0000-8000-/.test(id);
}

// track used ids to avoid collisions when generating meaningful ids
const usedIds = new Set();

function markExistingId(id) {
    if (!id) return;
    usedIds.add(String(id));
}

function sanitizeForId(s) {
    if (!s) return '';
    return String(s)
        .trim()
        // replace sequences of non-alphanumeric with underscore
        .replace(/[^a-zA-Z0-9]+/g, '_')
        // trim leading/trailing underscores
        .replace(/^_+|_+$/g, '')
        || 'node';
}

function makeBaseId(parent, node) {
    const p = parent || {};
    const pKey = (typeof p.FieldName === 'string' && p.FieldName.trim())
        ? p.FieldName
        : (typeof p.Name === 'string' && p.Name.trim() ? p.Name : (typeof p.ComponentType === 'string' ? p.ComponentType : null));
    const nKey = (typeof node.FieldName === 'string' && node.FieldName.trim())
        ? node.FieldName
        : (typeof node.Name === 'string' && node.Name.trim() ? node.Name : (typeof node.ComponentType === 'string' ? node.ComponentType : null));
    if (pKey && nKey) {
        let base = sanitizeForId(pKey) + '.' + sanitizeForId(nKey);
        if (typeof node.Order !== 'undefined' && node.Order !== null) base += '.' + String(node.Order);
        return base;
    }
    // fallback to something using component type and order to keep it meaningful
    const fallbackParent = (typeof p.Name === 'string' && p.Name.trim()) ? p.Name : (typeof p.FieldName === 'string' ? p.FieldName : (p.ComponentType || 'root'));
    const fallbackNode = node.ComponentType || node.Name || node.FieldName || 'node';
    let base = sanitizeForId(fallbackParent) + '.' + sanitizeForId(fallbackNode);
    if (typeof node.Order !== 'undefined' && node.Order !== null) base += '.' + String(node.Order);
    return base;
}

function makeUniqueId(base) {
    if (!base) base = 'node';
    let candidate = String(base);
    let i = 0;
    let out = candidate;
    while (usedIds.has(out)) {
        i++;
        out = candidate + '_' + i;
    }
    usedIds.add(out);
    return out;
}

function ensureMeaningfulId(parent, node) {
    if (!node || typeof node !== 'object') return null;
    // if node has an id that is not a GUID-like string, keep it and reserve
    if (node.Id && !isGuid(node.Id)) {
        markExistingId(node.Id);
        return node.Id;
    }
    // generate meaningful id from parent + node
    const base = makeBaseId(parent, node);
    const id = makeUniqueId(base);
    node.Id = id;
    return id;
}

function traverseComponents(arr, parent) {
    if (!Array.isArray(arr)) return 0;
    let count = 0;
    for (const c of arr) {
        if (c && typeof c === 'object') {
            // replace GUID-like ids or assign if missing
            if (!c.Id || isGuid(c.Id)) {
                ensureMeaningfulId(parent, c);
                count++;
            } else {
                // keep existing meaningful id
                markExistingId(c.Id);
            }
            // recurse into nested Components arrays; pass current node as parent
            if (Array.isArray(c.Components)) {
                count += traverseComponents(c.Components, c);
            }
            // also look for other nested arrays that may contain component-like objects
            for (const key of Object.keys(c)) {
                if (key === 'Components') continue;
                const val = c[key];
                if (Array.isArray(val)) {
                    if (val.length && val.some(x => x && typeof x === 'object' && ('ComponentType' in x || 'FieldName' in x || 'Label' in x || 'Name' in x))) {
                        count += traverseComponents(val, c);
                    }
                }
            }
        }
    }
    return count;
}

function processGridPolicies(json) {
    if (!Array.isArray(json.GridPolicies)) return 0;
    let count = 0;
    for (const gp of json.GridPolicies) {
        if (gp && typeof gp === 'object') {
            // if id missing or is GUID, replace with meaningful id
            if (!gp.Id || isGuid(gp.Id)) {
                // generate base using parent json and gp
                const base = makeBaseId(json, gp);
                gp.Id = makeUniqueId(base);
                count++;
            } else {
                markExistingId(gp.Id);
            }
        }
    }
    return count;
}

function removeFeaturePolicyIds(json) {
    if (!Array.isArray(json.FeaturePolicies)) return 0;
    let removed = 0;
    for (const fp of json.FeaturePolicies) {
        if (fp && typeof fp === 'object' && 'Id' in fp) {
            delete fp.Id;
            removed++;
        }
    }
    return removed;
}

function setTopIdFromName(json) {
    if (!json || typeof json !== 'object') return 0;
    let changed = 0;
    // remove ParentId if present
    if ('ParentId' in json) {
        delete json.ParentId;
        changed++;
    }
    // build meaningful id from Name or Label or FieldName
    const nameKey = (typeof json.Name === 'string' && json.Name.trim()) ? json.Name
        : (typeof json.Label === 'string' && json.Label.trim() ? json.Label : (typeof json.FieldName === 'string' && json.FieldName.trim() ? json.FieldName : null));
    const base = nameKey ? sanitizeForId(nameKey) : 'root';
    // ensure uniqueness
    const id = makeUniqueId(base);
    if (!json.Id || isGuid(json.Id) || json.Id !== id) {
        json.Id = id;
        changed++;
    }
    return changed;
}

function transform(json) {
    // pre-mark any existing non-GUID ids to avoid collisions
    function markAllExisting(obj) {
        if (!obj || typeof obj !== 'object') return;
        if (obj.Id && !isGuid(obj.Id)) markExistingId(obj.Id);
        for (const k of Object.keys(obj)) {
            const v = obj[k];
            if (Array.isArray(v)) {
                for (const el of v) markAllExisting(el);
            } else if (v && typeof v === 'object') {
                markAllExisting(v);
            }
        }
    }
    markAllExisting(json);

    let changed = 0;
    changed += setTopIdFromName(json);
    changed += removeFeaturePolicyIds(json);
    changed += processGridPolicies(json);
    if (Array.isArray(json.Components)) {
        changed += traverseComponents(json.Components, json);
    }
    // also process ComponentGroup if present
    if (Array.isArray(json.ComponentGroup)) {
        changed += traverseComponents(json.ComponentGroup, json);
    }
    return changed;
}

function processFile(filePath) {
    try {
        const json = readJson(filePath);
        const changed = transform(json);
        if (changed > 0) {
            writeJson(filePath, json);
            console.log(`Processed: ${filePath} (modified ${changed} fields)`);
        } else {
            console.log(`Processed: ${filePath} (no changes)`);
        }
    } catch (err) {
        console.error('Error processing', filePath, err.message);
    }
}

function processFolder(folderPath) {
    fs.readdirSync(folderPath).forEach(f => {
        const abs = path.join(folderPath, f);
        const stat = fs.statSync(abs);
        if (stat.isDirectory()) processFolder(abs);
        else if (f.endsWith('.json')) processFile(abs);
    });
}

// CLI
const target = process.argv[2];
if (!target) {
    console.log('Usage: node scripts\\setIds.js <file|folder>');
    process.exit(1);
}
const absTarget = path.resolve(target);
if (!fs.existsSync(absTarget)) {
    console.error('Target not found:', absTarget);
    process.exit(1);
}
if (fs.statSync(absTarget).isDirectory()) processFolder(absTarget);
else processFile(absTarget);
