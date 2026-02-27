const fs = require('fs');
const path = require('path');

function deepMerge(src, target) {
  if (typeof src !== 'object' || src === null) return;
  if (typeof target !== 'object' || target === null) return;
  Object.keys(src).forEach(key => {
    const sv = src[key];
    const tv = target[key];
    if (typeof sv === 'object' && sv !== null && !Array.isArray(sv)) {
      if (typeof tv !== 'object' || tv === null || Array.isArray(tv)) {
        target[key] = {};
      }
      deepMerge(sv, target[key]);
    } else {
      if (!(key in target)) {
        target[key] = sv;
      }
    }
  });
}

function findEnFiles(root) {
  const results = [];
  function walk(dir) {
    const entries = fs.readdirSync(dir, { withFileTypes: true });
    for (const e of entries) {
      const full = path.join(dir, e.name);
      if (e.isDirectory()) walk(full);
      else if (e.isFile() && e.name === 'en.json') results.push(full);
    }
  }
  walk(root);
  return results;
}

const repoRoot = path.join(__dirname, '..');
const modulesRoot = path.join(repoRoot, 'src', 'modules');
const enFiles = findEnFiles(modulesRoot);
const changed = [];

for (const enPath of enFiles) {
  const frPath = path.join(path.dirname(enPath), 'fr.json');
  let enJson; try { enJson = JSON.parse(fs.readFileSync(enPath, 'utf8')); } catch(e) { console.error('Failed parse', enPath, e); continue; }
  let frJson = {};
  if (fs.existsSync(frPath)) {
    try { frJson = JSON.parse(fs.readFileSync(frPath, 'utf8')); } catch(e) { console.error('Failed parse FR', frPath, e); }
  }
  const before = JSON.stringify(frJson);
  deepMerge(enJson, frJson);
  const after = JSON.stringify(frJson);
  if (before !== after) {
    fs.writeFileSync(frPath, JSON.stringify(frJson, null, 2) + '\n', 'utf8');
    changed.push(frPath);
  }
}

console.log('Merged missing keys into FR files. Updated:', changed.length);
changed.forEach(f => console.log('  ', f));

if (changed.length === 0) console.log('No changes needed.');
