"""Helpers for computing the next semantic version for CI workflows."""

from __future__ import annotations

import re
from typing import Tuple

_VALID_INCREMENTS = {"major", "minor", "patch"}


def sanitize_prerelease_label(label: str) -> str:
    """Return a GitHub Actions-friendly prerelease label."""
    cleaned = re.sub(r"[^0-9A-Za-z.-]+", "-", label or "").strip("-")
    return cleaned


def _increment_segments(major: int, minor: int, patch: int, increment: str) -> Tuple[int, int, int]:
    if increment == "major":
        return major + 1, 0, 0
    if increment == "minor":
        return major, minor + 1, 0
    return major, minor, patch + 1


def compute_next_version(current_version: str, increment: str, prerelease_label: str) -> str:
    """Compute the next semantic version string.

    Args:
        current_version: The version stored in project metadata.
        increment: One of "major", "minor", or "patch".
        prerelease_label: Optional prerelease suffix (e.g. "beta.1").

    Returns:
        The SemVer string for the requested bump.
    """

    normalized_increment = (increment or "").strip().lower()
    if normalized_increment not in _VALID_INCREMENTS:
        raise ValueError(f"Unsupported increment: {increment!r}")

    sanitized_label = sanitize_prerelease_label(prerelease_label)

    base_version = current_version.split("-", 1)[0]
    try:
        major, minor, patch = [int(part) for part in base_version.split(".")]
    except ValueError as exc:
        raise ValueError(f"Current version is not a valid semver: {current_version!r}") from exc

    has_prerelease = "-" in current_version
    if has_prerelease and not sanitized_label:
        new_major, new_minor, new_patch = major, minor, patch
    else:
        new_major, new_minor, new_patch = _increment_segments(major, minor, patch, normalized_increment)

    if sanitized_label:
        return f"{new_major}.{new_minor}.{new_patch}-{sanitized_label}"
    return f"{new_major}.{new_minor}.{new_patch}"


__all__ = ["compute_next_version", "sanitize_prerelease_label"]
