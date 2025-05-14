#!/usr/bin/python3
import sys
import requests
import xml.etree.ElementTree as ET
import xml.dom.minidom
from datetime import datetime

OWNER = "Nexus-Mods"
REPO = "NexusMods.App"
GITHUB_API = f"https://api.github.com/repos/{OWNER}/{REPO}/releases"

def fetch_releases():
    releases = []
    page = 1

    while True:
        response = requests.get(GITHUB_API, params={"per_page": 100, "page": page})
        if response.status_code != 200:
            raise Exception(f"GitHub API error: {response.status_code} - {response.text}")

        page_releases = response.json()
        if not page_releases:
            break

        releases.extend(page_releases)
        page += 1

    return releases

def generate_appstream_xml(releases):
    root = ET.Element("releases")
    for release in releases:
        version = release["tag_name"].lstrip("v")  # Remove leading 'v' if present
        date = release["published_at"][:10]  # YYYY-MM-DD
        release_type = "development" if release.get("prerelease") else "stable"
        release_url = release["html_url"]

        rel = ET.SubElement(root, "release", version=version, date=date, type=release_type)
        url_elem = ET.SubElement(rel, "url", type="details")
        url_elem.text = release_url

    return ET.ElementTree(root)

def save_xml(tree, output_file):
    # Convert ElementTree to a byte string
    rough_string = ET.tostring(tree.getroot(), encoding="utf-8")
    # Parse and pretty-print with minidom
    reparsed = xml.dom.minidom.parseString(rough_string)
    pretty_xml = reparsed.toprettyxml(indent="\t")

    lines = pretty_xml.splitlines()
    lines.insert(1, "<!-- auto generated, don't edit manually -->")
    modified = "\n".join(lines)

    # Write to file
    with open(output_file, "w", encoding="utf-8") as f:
        f.write(modified)

    print(f"Formatted AppStream XML saved to {output_file}")

def main():
    if len(sys.argv) != 2:
        print("Missing arg <output>")
        sys.exit(1)

    output_file = sys.argv[1]
    releases = fetch_releases()
    tree = generate_appstream_xml(releases)
    save_xml(tree, output_file)

if __name__ == "__main__":
    main()
