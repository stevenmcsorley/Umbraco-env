"""Fail CI when NuGet's machine-readable package report contains advisories."""

from __future__ import annotations

import json
import sys
from pathlib import Path
from typing import Any


def find_vulnerabilities(value: Any, location: str = "report") -> list[str]:
    findings: list[str] = []
    if isinstance(value, dict):
        for key, child in value.items():
            current = f"{location}.{key}"
            if key.casefold() in {"vulnerabilities", "vulnerability"} and child:
                if isinstance(child, list):
                    findings.extend(f"{current}: {item}" for item in child)
                else:
                    findings.append(f"{current}: {child}")
            else:
                findings.extend(find_vulnerabilities(child, current))
    elif isinstance(value, list):
        for index, child in enumerate(value):
            findings.extend(find_vulnerabilities(child, f"{location}[{index}]"))
    return findings


def main() -> int:
    if len(sys.argv) != 2:
        print("Usage: check_nuget_vulnerabilities.py <dotnet-list-json>", file=sys.stderr)
        return 2
    report_path = Path(sys.argv[1])
    try:
        report = json.loads(report_path.read_text(encoding="utf-8"))
    except (OSError, json.JSONDecodeError) as error:
        print(f"Could not read NuGet vulnerability report: {error}", file=sys.stderr)
        return 2
    findings = find_vulnerabilities(report)
    if findings:
        print("NuGet reported vulnerable dependencies:", file=sys.stderr)
        print("\n".join(findings), file=sys.stderr)
        return 1
    print("NuGet vulnerability report is clean.")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
