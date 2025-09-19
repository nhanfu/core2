const fs = require('fs');
const path = require('path');

function readJson(filePath) {
    const data = fs.readFileSync(filePath, 'utf8');
    return JSON.parse(data);
}

function writeJson(filePath, obj) {
    fs.writeFileSync(filePath, JSON.stringify(obj, null, 4), 'utf8');
}

function renameKeyRecursive(obj, oldKey, newKey) {
    if (Array.isArray(obj)) {
        for (const el of obj) renameKeyRecursive(el, oldKey, newKey);
        return;
    }
    if (obj && typeof obj === 'object') {
        for (const key of Object.keys(obj)) {
            const val = obj[key];
            if (key === oldKey) {
                // move value, avoid overwriting existing newKey
                if (obj[newKey] == null) obj[newKey] = val;
                else {
                    // merge arrays if both are arrays
                    if (Array.isArray(obj[newKey]) && Array.isArray(val)) {
                        obj[newKey] = obj[newKey].concat(val);
                    }
                }
                delete obj[oldKey];
                // continue traversal on the moved value
                renameKeyRecursive(obj[newKey], oldKey, newKey);
            } else {
                renameKeyRecursive(val, oldKey, newKey);
            }
        }
    }
}

function transform(json) {
    renameKeyRecursive(json, 'Children', 'Components');

    // move top-level Components into ComponentGroup[0].Components
    if (Array.isArray(json.Components)) {
        let components = json.Components;
        let text = JSON.stringify(json.ComponentGroup);
        if (components.length && text.indexOf(components[0].Id) >= 0) {
            json.ComponentGroup[0].Components = json.ComponentGroup[0].Components.concat(json.Components);
        }
    }

    json.Components = json.ComponentGroup;
    delete json.ComponentGroup;

    return json;
}

function processFile(filePath) {
    try {
        const json = readJson(filePath);
        const out = transform(json);
        writeJson(filePath, out);
        console.log('Processed:', filePath);
    } catch (err) {
        console.error('Error processing', filePath, err.message);
    }
}

function processFolder(folderPath) {
    fs.readdirSync(folderPath).forEach(f => {
        const abs = path.join(folderPath, f);
        if (fs.statSync(abs).isDirectory()) processFolder(abs);
        else if (f.endsWith('.json')) processFile(abs);
    });
}
// CLI
const target = process.argv[2];
if (!target) {
    console.log('Usage: node scripts\\transformComponents.js <file|folder>');
    process.exit(1);
}
const absTarget = path.resolve(target);
if (!fs.existsSync(absTarget)) {
    console.error('Target not found:', absTarget);
    process.exit(1);
}
if (fs.statSync(absTarget).isDirectory()) processFolder(absTarget);
else processFile(absTarget);