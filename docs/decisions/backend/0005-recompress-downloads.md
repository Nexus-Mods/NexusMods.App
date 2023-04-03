# Recompress Downloads

## Context and Problem Statement

There are many archive formats in use by mods on the Nexus. Some of the most common formats are .zip, .rar, and .7z.
several of these formats are what is known as [SOLID](https://en.wikipedia.org/wiki/Solid_compression) archives,
which means that the entire archive is compressed as
a single unit. This makes it difficult to extract only a few files from the archive, which is a common use case for
modding. In addition, some of these formats default or prefer extremely high compression algorithms such as LZMA2,
which while providing the best compression ratios, can take a long time to decompress. Reducing this complexity gives
us may open up opportunities for future features.

In addition, some formats like 7z are so complex that we must use external programs to decompress them and never
have the opportunity to keep the files in memory when we work with the data. This can lead to a lot of IO waste
when we are only interested in a few files from the archive. We must decompress the entire archive to find a few
files, to only delete the extracted files after we are done.

## Decision Drivers

* Allow users to preview files such as textures and meshes without decompressing the entire archive
* Allow for parallel decompression of multiple files from the same archive
* Allow for faster decompression of single large files
* Reduce IO waste by not decompressing files that are never used

Compression formats are numerous and all involve a lot of tradeoffs. Roughly speaking, there are two main categories: those
optimized for size and those optimized for performance.

* LZMA, LZMA2, XZ, BZIP2, GZIP are all optimized for size, they are all very slow to decompress
* ZSTD, LZ4, DEFLATE are all optimized for performance, they are all very fast to decompress, and have configuration options for time spent compressing vs size

In general the differences in size between these algorithms is in the range of 2x. LZ4 may have a 50% compressed size, while LZMA2 may have a 25% compressed size.
The difference in decompression speed is much larger, with LZ4 being 10x faster than LZMA2. Deciding on the official compression
format is beyond the scope of this ADR, but we should keep in mind that the most common modding compression formats (such as LZMA2) have
a massive performance penalty. There are other options to consider, like pregenerated dictionaries that can be used to greatly reduce file sizes
if we use more modern compression algorithms such as ZSTD. All discussion of compression algorithms is out of scope for this ADR, this
section being only meant to illustrate the tradeoffs that we are making and point out that what is best for server storage or long-term archival
may not be best for modding.

## Considered Options

### Option 1: Keep archives decompressed
Keep files decompressed on disk. This is close to what other mod managers do, this has the disadvantage of requiring
a lot of disk space, as well as disk wastage. On NTFS filesystems the default block size is 4KB, so file that is much
smaller wastes any space that is not a multiple of 4KB. Deleting an archive requires us to find every file that was
extracted from that archive and delete it. This is a lot of IO work for archives that contain many small files.

### Option 2: Keep files in their original format
This is the option currently implemented in the app, it has the advantage of not requiring any extra disk space, and using
high end compression algorithms. It has the disadvantage of requiring us to decompress the entire archive to find a few
files, and the decompression algorithms are often slow and not parallelizable.

### Option 3: Recompress archives
In this option we would recompress archives into a format that is more suitable for modding. We would implement this
at the end of the file analysis phase. When a mod is downloaded it is currently automatically extracted into a temporary
folder and then analyzed. Once the analysis is complete we delete the temporary folder. In this option we would instead
recompress the archive into a format that is more suitable for our use case.

## Decision Outcome

Option 3 was chosen due to the advantages it provides over the other options. It allows us to keep acceptable compression
ratios while also getting a significant boost to decompression performance and "just in time" file extraction.

### Consequences

* Bad, we can no longer hash an archive to determine the hash of the download. This is because the file is now recompressed
  and the hash will be different. We will have to store the original hash of the archive in the database and use that to determine
  what files are already downloaded.
* Bad, users will have to use the app to extract downloads. Not a major problem as we will provide an API for this, and a CLI command, but something
  we'll have to keep in mind.
* Good, we can now extract files from archives without decompressing the entire archive. This will allow us to preview files
  such as textures and meshes without having to decompress the entire archive.
* Good, we can now extract files from archives in parallel if we use a chunked data format (detailed below)
* Good, we can use whatever compression method we want
* Good, built correctly we can write alloc free compression methods that decompress from mmaped memory to mmaped memory
  and don't involve pushing data through internal buffers.
* Good, we can still delete the file with a single OS call, we don't have to find every file that was extracted from the archive
  and delete it.


## Proposed File Format

The file format looks a lot like a zip file, with the TOC (table of contents) at the end of the file. The TOC is a list of
file meta data. To improve compression rates, files are stored in chunks. Each chunk is compressed independently and and the
chunks for a given file need not be contiguous. This allows us to decompress files in parallel as each chunk can be decompressed
by a different thread. The TOC is uncompressed to allow for faster lookups. File ends with a 64 bit unsigned
integer that points to the start of the TOC.

### Overall file format

| Magic Number | Chunk 1 | Chunk 2 | ... | Chunk N | TOC | TOC Offset (absolute) |

* Magic Number: 8 bytes, "NEXUSARC"
* Chunk 1-N: 1 or more chunks. Each chunk is compressed independently via the algorithm specified in the TOC,
  chunks are not required to be contiguous. The size of the chunk is specified in the TOC.
* TOC: The table of contents
  * TOC Header:
    * File Count: 4 bytes, unsigned integer
    * File Entry:
      * File Name Length: 2 bytes, unsigned integer
      * File Name: File name in UTF-8 format, sizeof file name length
      * File Chunk Count: 4 bytes, unsigned integer, the number of chunks
      * File Chunk Size: 4 bytes, unsigned integer, the size of the chunks
      * Compression format: 1 byte
        - 0x00 = uncompressed
        - 0x01 = TBD, see future RFDs
      * Flags: 1byte - Unused
      * File Chunk Entry:
        * Chunk Offset: 8 bytes, unsigned integer, points to the start of the chunk relative to the start of the file
        * Chunk Compressed Size: 4 bytes, unsigned integer, the size of the chunk on disk
        * Chunk Uncompressed Size: 4 bytes, unsigned integer, the size of the chunk in memory
* TOC Offset: 8 bytes, unsigned integer, points to the start of the TOC relative to the start of the file

### Parallel Decompression
Any number of files can be decompressed in parallel via the above format. Since the resulting output sizes of the chunks are known,
the output file can be allocated in advance and `mmap` calls can be used to map both the compressed chunk and the output file into memory, from
there, most compression algorithms support `Span<byte>` inputs and outputs, which means no GC'd memory buffers need be used to decompress the chunks.
Each chunk can be assigned to a separate task or thread, and the output file can be flushed to disk once all chunks have been decompressed.

If a offset to the chunk definition portion of the TOC is stored in the app's metadata, parsing the TOC can be avoided during decompression,
instead the reader can jup directly to the chunk definition portion of the TOC and use that to decompress the file.

### Parallel Compression
Parallel compression is a bit harder to implement. This is because the output size of the compressed chunks is unknown in advance. However, since
the source file is being read in a fixed chunk size, we can allocate each chunk of the source to a different task or thread. The design for this
approach is to assign a tasks or threads to a set of chunks from the source files, these threads compress each chunk, then either lock the output
file or pass it to a single writer thread.

The process for parallel compression is:

* Write the magic number to the output file
* For each file
 * For each chunk
  * Compress the chunk
  * Write the chunk to the output file locking the file so multiple threads can't write to it at the same time
  * Save the chunk offset and sizes in memory
* Write the TOC using the chunk offsets and sizes from the previous step

### Chunk Size
Since the chunk sizes are independent of each other, we can choose whatever chunk size we want even on a per file basis. One option may be to
select smaller or larger chunks of data to improve the "fit" of the file. Some file types such as BSA compressed files are already compressed
so in that case we can use a uncompressed chunk and in not double compress the file. Per chunk compression formats may result in better file
sizes but likely will result in a more complex implementation.
