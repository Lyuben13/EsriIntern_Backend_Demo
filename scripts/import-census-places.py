"""Build the city search dataset from the Census Bureau SUB-EST2025 CSV."""

import argparse
import csv
import json
from pathlib import Path


def import_places(source: Path, output: Path) -> None:
    with source.open(encoding="cp1252", newline="") as file:
        records = list(csv.DictReader(file))

    counties = {
        (row["STATE"], row["COUNTY"]): row["NAME"]
        for row in records
        if row["SUMLEV"] == "050"
    }
    places = []
    for row in records:
        level = row["SUMLEV"]
        name = row["NAME"]
        is_town = level == "061" and row["FUNCSTAT"] == "A" and name.endswith(" town")
        if level not in {"162", "170", "172"} and not is_town:
            continue

        estimate = int(row["POPESTIMATE2025"])
        county = counties.get((row["STATE"], row["COUNTY"])) if is_town else None
        places.append([name, row["STNAME"], estimate if estimate >= 0 else None, county])

    places.sort(key=lambda place: (place[0].casefold(), place[1], place[3] or ""))
    output.write_text(json.dumps(places, ensure_ascii=False, separators=(",", ":")) + "\n", encoding="utf-8")
    print(f"Imported {len(places):,} places.")


if __name__ == "__main__":
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("source", type=Path, help="Path to sub-est2025.csv")
    parser.add_argument("output", type=Path, help="Path to census-places-2025.json")
    args = parser.parse_args()
    import_places(args.source, args.output)
