# Security Policy

## Reporting a Vulnerability

**Do not open a public issue for security vulnerabilities.**

Please report vulnerabilities privately through
[GitHub Private Vulnerability Reporting](https://github.com/davalillo/mql-language-server/security/advisories/new).

This allows a coordinated disclosure: GitHub notifies the maintainers,
discussions stay confidential, and a fix can be released before the issue
is made public.

### What to include

- Affected version (e.g. `mql-lsp-server --version` output)
- A description of the vulnerability and its impact
- Steps or code to reproduce, if possible
- Any suggested mitigation

### What to expect

- Acknowledgement within **7 days**
- An assessment and, if confirmed, a fix targeted for the next release
- Public disclosure coordinated with the reporter once a fix is available

## Scope

This project is a Language Server for MQL4/MQL5. Understanding its surface
helps define what is in and out of scope:

**In scope:**

- Vulnerabilities in the language server itself: parsing a crafted MQL4/MQL5
  file, processing LSP messages over stdio, or the packaged binaries /
  NuGet package behaving in unexpected ways (crash, arbitrary code
  execution, file access outside the workspace, etc.)
- The distribution artifacts published through GitHub Releases and NuGet

**Out of scope:**

- Vulnerabilities in the editor or LSP client you use with the server
- MQL code written by the user being edited
- Vulnerabilities in transitive dependencies that are not reachable from
  the server's runtime behavior. Known cases are analyzed with full
  justification in
  [`docs/references/SECURITY.md`](docs/references/SECURITY.md)

## Supported Versions

Only the latest release receives security fixes. Pre-release versions
(e.g. `2.0.0-rc.*`) are provided as-is and are not guaranteed to receive
patches.

| Version | Supported |
|---------|-----------|
| latest release | ✅ |
| pre-releases | ⚠️ best-effort |
| older releases | ❌ |

## Security Model

The MQL language server runs as a standalone local process:

- It communicates with the editor exclusively over **stdio** — it does not
  open network ports or make HTTP requests
- It reads MQL4/MQL5 source files from the workspace opened in your editor
- It does not send telemetry or make network calls

The primary trust boundary is therefore the local workspace content the
server parses and indexes.