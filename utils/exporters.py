import csv
import json
from io import StringIO


def _normalise_rows(rows):
    if rows is None:
        return []
    if isinstance(rows, dict):
        return [rows]
    return list(rows)


def export_to_excel(rows, fields=None):
    rows = _normalise_rows(rows)
    if not rows:
        return b""

    keys = fields or sorted({key for row in rows if isinstance(row, dict) for key in row.keys()})
    output = StringIO()
    writer = csv.DictWriter(output, fieldnames=keys, extrasaction="ignore")
    writer.writeheader()
    for row in rows:
        writer.writerow({
            key: json.dumps(value, default=str) if isinstance(value, (dict, list, tuple)) else value
            for key, value in row.items()
        })
    return output.getvalue().encode("utf-8-sig")


def export_to_pdf(content):
    if isinstance(content, bytes):
        return content
    return str(content or "").encode("utf-8")
