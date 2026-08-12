# Security Policy

## Supported versions

uSync ships one release line per Umbraco major. Fixes go to the current line, and to
the previous one where the issue is serious and the fix is practical. Older lines are
kept as `{major}/dev` branches for reference but are not actively maintained.

| Version | Branch | Supported |
| --- | --- | --- |
| 18.x | `v18/main` | Yes |
| 17.x | `v17/main` | Yes |
| 16.x and earlier | `{major}/dev` | No |

## Reporting a vulnerability

Please **do not** open a public issue for a security problem.

Email **info@jumoo.co.uk** with a description of the issue, the version affected, and
steps to reproduce it. We'll acknowledge within a few working days and keep you
updated as we work on it.

uSync reads and writes content from Umbraco's database onto disk, and re-imports disk
files (including on server start), so if the issue involves file paths, archive
extraction, or XML/JSON parsing of a file that isn't fully trusted, say so - those get
sequenced first.
