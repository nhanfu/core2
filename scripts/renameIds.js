const fs = require('fs');
const path = require('path');

function processFile(filePath) {
    const data = fs.readFileSync(filePath, 'utf8');
    let json;
    try {
        json = JSON.parse(data);
    } catch (e) {
        console.error(`Error parsing ${filePath}:`, e.message);
        return;
    }
    const sections = json.ComponentGroup;
    if (sections == null) {
        console.log('No need to transform', filePath);
    }
    json.ComponentGroup = buildTree(sections);
    fs.writeFileSync(filePath, JSON.stringify(json, null, 4), 'utf8');
    console.log(`Processed: ${filePath}`);
}

function buildTree(componentGroup) {
    if (componentGroup == null) return;
    var componentGroupMap = new Map(componentGroup.map(x => [x.Id, x]));
    let parent;

    for (const item of componentGroup) {
        if (!item.ParentId) {
            continue;
        }

        if (!componentGroupMap.has(item.ParentId)) {
            continue;
        }

        parent = componentGroupMap.get(item.ParentId);

        if (!parent.Children) {
            parent.Children = [];
        }

        if (!parent.Children.includes(item)) {
            parent.Children.push(item);
        }
    }

    for (const item of componentGroup) {
        if (!item.Children || !item.Children.length) {
            item.Children = [];
            continue;
        }
        if (item.Children) {
            item.Children = item.Children.sort((a, b) => a.Order - b.Order);
        }
    }

    const res = componentGroup.filter(x => !x.ParentId);
    for (const section of componentGroup) {
        delete section.ParentId;
        if (section.Children == 0)
            delete section.Children;
    }

    if (!res.length) {
        console.log("No component group is root component. Wrong feature name or the configuration is wrong");
    }

    return res;
}

function processFolder(folderPath) {
    fs.readdirSync(folderPath).forEach(file => {
        const absPath = path.join(folderPath, file);
        if (fs.statSync(absPath).isDirectory()) {
            processFolder(absPath);
        } else if (file.endsWith('.json')) {
            processFile(absPath);
        }
    });
}

// Usage: node renameIds.js <file|folder>
const target = process.argv[2];
if (!target) {
    console.log('Usage: node renameIds.js <file|folder>');
    process.exit(1);
}
const absTarget = path.resolve(target);
if (fs.existsSync(absTarget)) {
    if (fs.statSync(absTarget).isDirectory()) {
        processFolder(absTarget);
    } else {
        processFile(absTarget);
    }
} else {
    console.error('Target not found:', absTarget);
}