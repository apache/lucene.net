﻿# extract-wikipedia

### Name

`benchmark-extract-wikipedia` - Extracts a downloaded Wikipedia dump into separate files for indexing.

### Synopsis

<code>dotnet lucene-cli.dll benchmark extract-wikipedia [?|-h|--help]</code>

### Arguments

`INPUT_WIKIPEDIA_FILE`

Input path to a Wikipedia XML file.

`OUTPUT_DIRECTORY`

Path to a directory where the output files will be written.

### Options

`?|-h|--help`

Prints out a short help for the command.

`-d|--discard-image-only-docs`

Tells the extractor to skip WIKI docs that contain only images.

### Example

Extracts the `c:\wiki.xml` file into the `c:\out` directory, skipping any docs that only contain images.

<code>dotnet lucene-cli.dll benchmark extract-wikipedia c:\wiki.xml c:\out -d</code>
