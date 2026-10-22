"""Regression tests for the narrowly scoped ImageSharp advisory exception."""

import unittest

from check_nuget_vulnerabilities import find_vulnerabilities


def report(package_id: str, version: str, advisory: str) -> dict:
    return {
        "projects": [
            {
                "frameworks": [
                    {
                        "libraries": [
                            {
                                "id": package_id,
                                "resolvedVersion": version,
                                "vulnerabilities": [
                                    {"advisoryurl": f"https://github.com/advisories/{advisory}"}
                                ],
                            }
                        ]
                    }
                ]
            }
        ]
    }


class VulnerabilityPolicyTests(unittest.TestCase):
    def test_only_known_image_sharp_320_advisories_are_suppressed(self) -> None:
        known = [
            "GHSA-gwg2-r3hj-4w44",
            "GHSA-j3p4-wp97-rph4",
            "GHSA-j9gm-c75j-xc9q",
            "GHSA-jjfr-hcj7-qf5w",
            "GHSA-wmxv-xphr-5c9g",
        ]
        for advisory in known:
            with self.subTest(advisory=advisory):
                self.assertEqual([], find_vulnerabilities(report("SixLabors.ImageSharp", "3.2.0", advisory)))

    def test_different_image_sharp_version_is_not_suppressed(self) -> None:
        self.assertTrue(find_vulnerabilities(report("SixLabors.ImageSharp", "3.1.12", "GHSA-j3p4-wp97-rph4")))

    def test_different_package_is_not_suppressed(self) -> None:
        self.assertTrue(find_vulnerabilities(report("Other.Package", "3.2.0", "GHSA-j3p4-wp97-rph4")))

    def test_new_advisory_for_patched_version_is_not_suppressed(self) -> None:
        self.assertTrue(find_vulnerabilities(report("SixLabors.ImageSharp", "3.2.0", "GHSA-new-advisory")))


if __name__ == "__main__":
    unittest.main()
