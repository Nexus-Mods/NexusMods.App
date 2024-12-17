import os
import re
import requests
import hashlib
from PIL import Image
from io import BytesIO

# Updates all the links in the CHANGELOG.md file that point to PNG images to 
# WebP format hashed and stored locally in the 'docs/changelog-assets' directory. 

# Configuration
CHANGELOG_PATH = 'CHANGELOG.md'
ASSETS_DIR = os.path.join('docs', 'changelog-assets')
IMAGE_URL_PATTERN = re.compile(r'\[([^\]]+)\]\((https://[^)]+\.png)\)')

def ensure_assets_dir():
    """Ensure that the assets directory exists."""
    os.makedirs(ASSETS_DIR, exist_ok=True)
    print(f"Assets directory ensured at: {ASSETS_DIR}")

def read_changelog():
    """Read the content of the CHANGELOG.md file."""
    with open(CHANGELOG_PATH, 'r', encoding='utf-8') as file:
        content = file.read()
    print(f"Read {len(content)} characters from {CHANGELOG_PATH}")
    return content

def write_changelog(content):
    """Write the updated content back to the CHANGELOG.md file."""
    with open(CHANGELOG_PATH, 'w', encoding='utf-8') as file:
        file.write(content)
    print(f"Updated {CHANGELOG_PATH}")

def find_image_links(content):
    """Find all markdown links to .png images."""
    matches = IMAGE_URL_PATTERN.findall(content)
    unique_urls = list(set(url for _, url in matches))
    print(f"Found {len(unique_urls)} unique image URLs to process.")
    return unique_urls

def download_image(url):
    """Download image from the given URL."""
    try:
        response = requests.get(url, timeout=10)
        response.raise_for_status()
        print(f"Downloaded image from {url}")
        return response.content
    except requests.RequestException as e:
        print(f"Error downloading {url}: {e}")
        return None

def convert_to_webp(image_data):
    """Convert image data to WebP format."""
    try:
        with Image.open(BytesIO(image_data)) as img:
            with BytesIO() as output:
                img.save(output, format='WEBP', quality=80)
                webp_data = output.getvalue()
        print("Converted image to WebP format.")
        return webp_data
    except Exception as e:
        print(f"Error converting image to WebP: {e}")
        return None

def hash_webp(webp_data):
    """
    Hash the WebP data using BLAKE2b with a digest size of 128 bits (16 bytes)
    to match `b2sum --length=128`.
    """
    blake2b_hash = hashlib.blake2b(webp_data, digest_size=16).hexdigest()
    print(f"Hashed WebP data to {blake2b_hash}")
    return blake2b_hash

def save_webp(webp_data, hash_digest):
    """Save the WebP data to the assets directory with the hash as filename."""
    filename = f"{hash_digest}.webp"
    filepath = os.path.join(ASSETS_DIR, filename)
    if not os.path.exists(filepath):
        with open(filepath, 'wb') as file:
            file.write(webp_data)
        print(f"Saved WebP image to {filepath}")
    else:
        print(f"WebP image already exists at {filepath}")
    return filepath

def process_images(urls):
    """Process all image URLs: download, convert, hash, and save."""
    url_to_new_path = {}
    for url in urls:
        print(f"Processing URL: {url}")
        image_data = download_image(url)
        if not image_data:
            continue

        webp_data = convert_to_webp(image_data)
        if not webp_data:
            continue

        hash_digest = hash_webp(webp_data)
        saved_path = save_webp(webp_data, hash_digest)

        # Store the relative path for replacement
        relative_path = os.path.relpath(saved_path, start=os.path.dirname(CHANGELOG_PATH))
        relative_path = relative_path.replace(os.sep, '/')

        # Ensure the path starts with './'
        if not relative_path.startswith(('.', '/')):
            relative_path = f'./{relative_path}'

        url_to_new_path[url] = relative_path
        print(f"Updated path for {url}: {relative_path}")
    return url_to_new_path

def update_changelog(content, url_mapping):
    """Update the changelog content with new relative WebP paths."""
    def replace_url(match):
        text, url = match.groups()
        new_url = url_mapping.get(url, url)
        return f'[{text}]({new_url})'

    updated_content = IMAGE_URL_PATTERN.sub(replace_url, content)
    print("Changelog content updated with new image paths.")
    return updated_content

def main():
    ensure_assets_dir()
    content = read_changelog()
    image_urls = find_image_links(content)
    url_mapping = process_images(image_urls)
    if not url_mapping:
        print("No images were processed. Exiting.")
        return
    updated_content = update_changelog(content, url_mapping)
    write_changelog(updated_content)
    print("All done!")

if __name__ == "__main__":
    main()

