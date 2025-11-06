"""Sanity checks for the version computation helper."""

from __future__ import annotations

import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parent.parent
if str(ROOT) not in sys.path:
    sys.path.insert(0, str(ROOT))

from scripts.versioning import compute_next_version


def _assert(expected: str, current: str, increment: str, label: str) -> None:
    result = compute_next_version(current, increment, label)
    assert (
        result == expected
    ), f"Expected {expected!r} from ({current!r}, {increment!r}, {label!r}), got {result!r}"


def main() -> None:
    _assert("1.3.0", "1.3.0-beta.1", "patch", "")
    _assert("1.3.1", "1.3.0", "patch", "")
    _assert("2.0.0", "1.3.0", "major", "")
    print("versioning_regressions: all checks passed")


if __name__ == "__main__":
    main()
