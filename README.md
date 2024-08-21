# P3DTool
A simple command-line tool for managing Pure3D files.

```
Usage: P3DTool [options]

Options:
  -f   | --force                    Force overwrite the output file.
  -nh  | --no_history               Don't add history chunk.
  -s   | --sort                     Sort chunks.
  -sa  | --sort_alphabetical        When sorting, the chunks will be sorted alphabetically.
  -sis | --sort_include_sections    When sorting, a history chunk will be add to the start if each section.
  -d   | --dedupe                   When adding a file, duplicate chunks will be omitted.
  -c   | --compress                 Compresses the output Pure3D file with LZR compression.
  -p   | --pause                    Pauses at the end of execution.

Example:
  P3DTool -s -i C:\input\file1.p3d -i C:\input\file2.p3d -o C:\output\file.p3d
  P3DTool -c --no_history -i C:\input\file.p3d -o C:\output\file.p3d
```

## Basic Usage
At minimum, the tool requires an input and an output file:

`P3DTool.exe -i "C:\Input\file.p3d" -o "C:\Output\file.p3d"`

Running a command like this would output an almost identical file; however, if the input file is compressed, it will decompress it.

You can specify as many input files as necessary, and their chunks will be merged.

## Optional Arguments
P3DTool's primary functionality comes from optional arguments that trigger specific functions. You can use any combination of the options below to achieve the desired output.

### Force
By default, if the output file exists, you will be prompted to choose an action:
```
Output file "C:\Input\file.p3d" already exists.
Do you want to overwrite? [Yes/No]
```
You can use the `--force` or `-f` argument to automatically overwrite the output file.

### No History
By default, the tool adds a `History` chunk to the start of the output file, containing details on the tool's execution.

You can use the `--no_history` or `-nh` argument to omit this chunk from the output file.

### Sort
By default, chunks are added to the output file in the order they appear in the input files, with each input file appending its chunks to the end of the output.

You can use the `--sort` or `-s` argument to sort chunks by their type, prioritizing them as required by `Simpsons: Hit & Run`. For example, `Texture` chunks will be placed before `Shader` chunks.

#### Sort Alphabetical
By default, when sorting chunks, they will maintain the same order as in the input files, but will be grouped by type.

You can use the `--sort_alphabetical` or `-sa` argument to sort named chunks alphabetically. Chunk types without a name will remain in their original order.

#### Sort Include Sections
By default, when sorting chunks, they are only grouped by type.

You can use the `--sort_include_sections` or `-sis` argument to add a `History` chunk before each type, serving as a section header with the type name.

### Deduplication (Dedupe)
By default, when processing input files, if two identical chunks are present, both will be added to the output.

You can use the `--dedupe` or `-d` argument to omit duplicate chunks.

*Note: This will only check root chunks, not children.*

### Compress
By default, the output file is written as a Pure3D file using your system's default endian format.

You can use the `--compress` or `-c` argument to compress the output with LZR (Lempel-Ziv-Radical) compression.

### Pause
By default, the tool will exit upon completion, provided there are no errors.

You can use the `--pause` or `-p` argument to wait for user input before closing.