"""Static local GUID inventory; package GUIDs need Unity's resolved PackageCache."""
import json
import re
from pathlib import Path

root = Path(__file__).resolve().parents[1] / 'WesternShooter'
assets = root / 'Assets'
known = set()
for path in assets.rglob('*.meta'):
    match = re.search(r'^guid: ([0-9a-f]{32})$', path.read_text(errors='replace'), re.M)
    if match:
        known.add(match[1])
missing = {}
for path in assets.rglob('*'):
    if path.suffix not in {'.prefab', '.controller', '.anim', '.asset', '.mixer', '.unity'}:
        continue
    refs = set(re.findall(r'\{fileID: -?\d+,\s*guid: ([0-9a-f]{32})', path.read_text(errors='replace')))
    unresolved = sorted(g for g in refs if g not in known and not g.startswith('0000000000000000'))
    if unresolved:
        missing[str(path.relative_to(root))] = unresolved
print(json.dumps(missing, indent=2))
