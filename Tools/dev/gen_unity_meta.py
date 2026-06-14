#!/usr/bin/env python3
"""
Generate Unity .meta files (with stable random GUIDs) for the hand-authored project
assets this headless workflow creates: C# scripts, assembly definitions, and the
folders under Assets/. Idempotent — existing .meta files are never touched, so GUIDs
stay stable across runs.

Binary/imported art assets (fbx, glb, png, mat, ...) are intentionally skipped:
Unity creates the correct importer metas for those on first import.

Run from the repo root:  python3 Tools/dev/gen_unity_meta.py
"""
import os
import uuid

REPO = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", ".."))
ASSETS = os.path.join(REPO, "Assets")

FOLDER_META = """fileFormatVersion: 2
guid: {guid}
folderAsset: yes
DefaultImporter:
  externalObjects: {{}}
  userData:
  assetBundleName:
  assetBundleVariant:
"""

SCRIPT_META = """fileFormatVersion: 2
guid: {guid}
MonoImporter:
  externalObjects: {{}}
  serializedVersion: 2
  defaultReferences: []
  executionOrder: 0
  icon: {{instanceID: 0}}
  userData:
  assetBundleName:
  assetBundleVariant:
"""

ASMDEF_META = """fileFormatVersion: 2
guid: {guid}
AssemblyDefinitionImporter:
  externalObjects: {{}}
  userData:
  assetBundleName:
  assetBundleVariant:
"""

SIMPLE_EXT = {".cs": SCRIPT_META, ".asmdef": ASMDEF_META, ".asmref": ASMDEF_META}


def ensure_meta(path, template):
    meta = path + ".meta"
    if os.path.exists(meta):
        return False
    with open(meta, "w") as f:
        f.write(template.format(guid=uuid.uuid4().hex))
    return True


def main():
    if not os.path.isdir(ASSETS):
        print("no Assets/ folder found")
        return
    created = 0
    for root, dirs, files in os.walk(ASSETS):
        for d in dirs:
            if ensure_meta(os.path.join(root, d), FOLDER_META):
                created += 1
        for fn in files:
            if fn.endswith(".meta"):
                continue
            tmpl = SIMPLE_EXT.get(os.path.splitext(fn)[1].lower())
            if tmpl and ensure_meta(os.path.join(root, fn), tmpl):
                created += 1
    print(f"created {created} meta file(s)")


if __name__ == "__main__":
    main()
