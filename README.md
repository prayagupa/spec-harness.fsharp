# codebase-explainer.fsharp

A reference **spec-driven development (SDD)** test harness in F#. Specs in `specs/` describe expected behavior; the harness verifies that `BillingApp` — a minimal **invoicing** library — (and the repo itself) conform.

## Install (macOS Intel)

F# is included in the [.NET SDK](https://dotnet.microsoft.com/download). Pick one option:

**Homebrew** (recommended if you already use brew):

```bash
brew install --cask dotnet-sdk
dotnet --version   # expect 10.x
```

**Microsoft install script** (no Homebrew):

```bash
curl -sSL https://dot.net/v1/dotnet-install.sh | bash /dev/stdin --channel 10.0
export PATH="$HOME/.dotnet:$PATH"
echo 'export PATH="$HOME/.dotnet:$PATH"' >> ~/.bash_profile
dotnet --version
```

## Quick start

Requires .NET 10 SDK (see [Install](#install-macos-intel) above).

```bash
dotnet restore
dotnet build
dotnet test
dotnet run --project src/Harness -- validate
dotnet run --project src/Harness -- run --tag smoke
```

## SDD workflow

1. **Define** — Add or edit a YAML spec under `specs/`.
2. **Implement** — Change `src/BillingApp` (or repo files) to satisfy the spec.
3. **Verify** — Run the harness; failures show expected vs actual.
4. **Evolve** — Commit spec and code changes together.

### Example spec

```yaml
# specs/public-api.yaml
version: "1.0"
target: src/BillingApp
entries:
  - id: grand-total-signature
    description: GrandTotal combines subtotal and tax into a decimal total
    category: public-api
    tags: [smoke]
    assertion:
      type: member-exists
      typeName: BillingApp.InvoiceTotals
      memberName: GrandTotal
      signature: "decimal -> decimal -> decimal"
```

### Example output

```bash
dotnet run --project src/Harness -- run --tag smoke
```

```
Spec Results (6 passed, 0 failed, 0 skipped)

ID                         STATUS   TIME     MESSAGE
readme-exists              PASS     1ms
...
```

### Demo failure

```bash
dotnet run --project src/Harness -- run --tag demo-only
```

Shows a deliberate signature mismatch (`decimal -> decimal -> int` vs `decimal -> decimal -> decimal`).

## CLI

| Command | Description |
|---------|-------------|
| `run` | Execute specs (default) |
| `validate` | Parse and schema-validate specs |
| `list` | List spec ids and categories |

Common flags: `--specs-dir`, `--spec`, `--format text|json`, `--output`, `--fail-fast`, `--target`, `--tag`, `--id`.

Exit codes: `0` pass, `1` spec failure, `2` harness error.

## Project layout

```
specs/              # YAML specs + schema.json
src/Harness/        # F# CLI harness
src/BillingApp/      # Minimal invoicing library under test
tests/Harness.Tests/
```

## Docs

- [PRD](docs/spec-driven-dev-test-harness.md)
- [Tech spec](docs/tech-spec-spec-driven-test-harness.md)
