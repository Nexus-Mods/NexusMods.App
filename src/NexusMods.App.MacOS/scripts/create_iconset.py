from PIL import Image
import os

def create_iconset(input_file, output_dir):
    # Define the icon sizes and filenames for the macOS .iconset
    sizes = [
        (16, "Icon16.png"),
        (32, "Icon32.png"),
        (64, "Icon64.png"),
        (128, "Icon128.png"),
        (256, "Icon256.png"),
        (512, "Icon512.png"),
        (1024, "Icon1024.png")
    ]

    # Create the output directory if it doesn't exist
    if not os.path.exists(output_dir):
        os.makedirs(output_dir)

    # Open the input image
    with Image.open(input_file) as img:
        for size, filename in sizes:
            resized_img = img.resize((size, size), Image.LANCZOS)
            resized_img.save(os.path.join(output_dir, filename))
            print(f"Created {filename} with size {size}x{size}")

if __name__ == "__main__":
    import argparse

    # Set up argument parser
    parser = argparse.ArgumentParser(description="Generate a macOS .iconset from a 512x512 PNG.")
    parser.add_argument("input_file", help="Path to the input 512x512 PNG file.")
    parser.add_argument("output_dir", help="Path to the output .iconset directory.")

    args = parser.parse_args()

    # Generate the iconset
    create_iconset(args.input_file, args.output_dir)

    print(f"\nIconset created at {args.output_dir}")
