﻿# run-trec-eval

### Name

`benchmark-run-trec-eval` - Runs a TREC evaluation.

### Synopsis

<code>dotnet lucene-cli.dll benchmark run-trec-eval [?|-h|--help]</code>

### Arguments

`INPUT_TOPICS_FILE`

Input file containing queries.

`INPUT_QUERY_RELEVANCE_FILE`

Input file conataining relevance judgements.

`OUTPUT_SUBMISSION_FILE`

Output submission file for TREC evaluation.

`INDEX_DIRECTORY`

The index directory.

### Options

`?|-h|--help`

Prints out a short help for the command.

`-t|--query-on-title`

Use title field in query. This flag will automatically be on if no other field is specified.

`-d|--query-on-description`

Use description field in query.

`-n|--query-on-narrative`

Use narrative field in query.

### Example

Runs a TREC evaluation on the `c:\topics` queries file and the `c:\queries` relevance judgements on the `c:\lucene-index` index using the description and narrative fields and places the output in `c:\output.txt`.

<code>dotnet lucene-cli.dll benchmark run-trec-eval c:\topics.txt c:\queries.txt c:\submissions.txt c:\output.txt c:\lucene-index -d -n</code>
