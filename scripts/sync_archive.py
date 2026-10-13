#!/usr/bin/env python3
"""archive 备份仓库同步脚本（无 rsync 环境替代品）。

用法：
  python3 scripts/sync_archive.py [主仓库 references 路径] [archive references 路径]

默认：
  src = ../references（主仓库，脚本位于 scripts/ 下）
  dst = 环境变量 ARCHIVE_REFERENCES 或 <仓库父目录>/slay-the-spire-2-mod-skill-archive/references

行为：缺的复制、内容差异覆盖、--delete 语义（archive 有而主仓库无的文件删除）；
⚠️ 自动跳过 v*/ 归档目录（历史教训：9dbbda8 误删 v1-v4）。
"""
import os
import re
import shutil
import sys

V_PAT = re.compile(r"^v\d+$")

HERE = os.path.dirname(os.path.abspath(__file__))
SRC = sys.argv[1] if len(sys.argv) > 1 else os.path.normpath(os.path.join(HERE, "../references"))
DST = sys.argv[2] if len(sys.argv) > 2 else os.environ.get(
    "ARCHIVE_REFERENCES",
    os.path.normpath(os.path.join(HERE, "../../../tmp/slay-the-spire-2-mod-skill-archive/references")))

copied = updated = deleted = 0

# 1. 从主仓库复制缺失/差异文件
for root, dirs, files in os.walk(SRC):
    rel = os.path.relpath(root, SRC)
    dst_dir = os.path.join(DST, rel) if rel != "." else DST
    os.makedirs(dst_dir, exist_ok=True)
    for f in files:
        s = os.path.join(root, f)
        d = os.path.join(dst_dir, f)
        if not os.path.exists(d):
            shutil.copy2(s, d); copied += 1
        elif os.path.getsize(s) != os.path.getsize(d) or open(s, "rb").read() != open(d, "rb").read():
            shutil.copy2(s, d); updated += 1

# 2. --delete 语义（保留 v*/）：删 archive 有而主仓库无的文件
for root, dirs, files in os.walk(DST, topdown=False):
    rel = os.path.relpath(root, DST)
    parts = rel.split(os.sep)
    if any(V_PAT.match(p) for p in parts):
        continue
    for f in files:
        s = os.path.join(SRC, rel, f) if rel != "." else os.path.join(SRC, f)
        if not os.path.exists(s):
            os.remove(os.path.join(root, f)); deleted += 1
    if rel != "." and not any(V_PAT.match(p) for p in parts) and not os.listdir(root):
        os.rmdir(root)

print(f"copied={copied} updated={updated} deleted={deleted}")
