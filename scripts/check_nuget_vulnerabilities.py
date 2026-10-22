"""Fail CI when NuGet's machine-readable package report contains advisories."""

from __future__ import annotations

import json
import sys
from pathlib import Path
from typing import Any


# ImageSharp 3.2.0's signed release notes link PR #3208, "Backport v3 security
# fixes and license enforcement". Six Labors says the five NuGet advisory
# records below are awaiting correction. Keep this exception narrow to package
# ID, exact version, and exact advisory IDs; tests guard each boundary.
# References:
# https://github.com/SixLabors/ImageSharp/releases/tag/v3.2.0
# https://github.com/SixLabors/ImageSharp/pull/3208
# https://sixlabors.com/posts/security-support-and-license-enforcement/
PATCHED_IMAGESHARP_ADVISORIES = {
    "ghsa-gwg2-r3hj-4w44",
    "ghsa-j3p4-wp97-rph4",
    "ghsa-j9gm-c75j-xc9q",
    "ghsa-jjfr-hcj7-qf5w",
    "ghsa-wmxv-xphr-5c9g",
}


def find_vulnerabilities(value: Any, location: str = "report") -> list[str]:
    findings: list[str] = []
    if isinstance(value, dict):
        for key, child in value.items():
            current = f"{location}.{key}"
            if key.casefold() == "vulnerabilities" and child:
                if isinstance(child, list):
                    package_id = str(value.get("id", ""))
                    version = str(value.get("resolvedVersion", ""))
                    is_verified_imagesharp_patch = (
                        package_id.casefold() == "sixlabors.imagesharp"
                        and version == "3.2.0"
                    )
                    for item in child:
                        advisory_url = item.get("advisoryurl", "") if isinstance(item, dict) else ""
                        advisory = str(advisory_url).rstrip("/").rsplit("/", 1)[-1].casefold()
                        if is_verified_imagesharp_patch and advisory in PATCHED_IMAGESHARP_ADVISORIES:
                            continue
                        findings.append(f"{current}: {item}")
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
