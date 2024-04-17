# Changelog Assets

This directory contains all assets used in the [CHANGELOG](../../CHANGELOG.md).

## Adding Assets

### Naming

Files should be hashed and named after their hash (`{hash}.{ext}`). This has multiple benefits:

- Prevents duplicate files.
- Prevents duplicate file names.
- Prevents uninformative file names.

The files in this directory aren't going to be looked up manually. They will be referenced by the CHANGELOG. As such, craming information into the file name is useless and will just lead to weird names and a complete mess.

For hashing, we'll use Blake2b with a length of 128. This hash can be computed using the [`cksum`](https://www.gnu.org/software/coreutils/manual/html_node/cksum-invocation.html#cksum-invocation) coreutils program (requires coreutils 9 or greater):

```bash
cksum -a blake2b --untagged --length=128 filename
```

Alternatively, `b2sum` can be used directly for distros that have coreutils 8:

```bash
b2sum --length=128 filename
```

### Images

- Images should be cropped to the contents, don't upload full screenshots.
- Images should be uploaded in the WebP format with the following options:
    - Image quality: 80%
    - Alpha quality: 100%
    - No Exif data
    - No IPTC
    - No XMP data
    - With color profile
    - No thumbnail

You can use any image manipulation program for this, like GIMP. Otherwise, if the image has already been cropped and you want to convert it to a WebP, use ffmpeg:

```bash
ffmpeg -i input.png -pix_fmt yuv420p -c:v libwebp -quality 80 -compression_level 6 output.webp
```

### Videos

- Videos should be cropped to contents, don't record your entire desktop.

TODO: decide on a format, probably WebM for longer videos and animated WebP for shorter videos. Definitely won't be using GIFs.

