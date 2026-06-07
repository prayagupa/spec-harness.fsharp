namespace Harness.Core

type SpecVersion = string

type SpecCategory =
    | FileLayout
    | PublicApi
    | Documentation
    | HttpContract

type Assertion =
    | FileExists of path: string
    | FileContains of path: string * substring: string
    | TypeExists of typeName: string
    | MemberExists of typeName: string * memberName: string * signature: string option
    | DocMentions of symbol: string

type SpecEntry =
    { Id: string
      Description: string
      Category: SpecCategory
      Tags: string list
      Assertion: Assertion }

type SpecDocument =
    { Version: SpecVersion
      Target: string
      Entries: SpecEntry list }

type LoadedEntry =
    { Target: string
      Entry: SpecEntry
      SourceFile: string }

type SpecStatus =
    | Passed
    | Failed
    | Skipped

type SpecResult =
    { Id: string
      Status: SpecStatus
      Message: string option
      Expected: string option
      Actual: string option
      DurationMs: int64 }

type RunSummary =
    { Total: int
      Passed: int
      Failed: int
      Skipped: int
      Results: SpecResult list }

type RunOptions =
    { TargetOverride: string option
      RepoRoot: string
      FailFast: bool
      TagFilter: string list option
      IdFilter: string list option }

module SpecCategory =
    let ofString (value: string) =
        match value with
        | "file-layout" -> Ok FileLayout
        | "public-api" -> Ok PublicApi
        | "documentation" -> Ok Documentation
        | "http-contract" -> Ok HttpContract
        | other -> Error $"Unknown category '{other}'"

    let toString category =
        match category with
        | FileLayout -> "file-layout"
        | PublicApi -> "public-api"
        | Documentation -> "documentation"
        | HttpContract -> "http-contract"

module SpecStatus =
    let toReportString status =
        match status with
        | Passed -> "passed"
        | Failed -> "failed"
        | Skipped -> "skipped"

    let toDisplayString status =
        match status with
        | Passed -> "PASS"
        | Failed -> "FAIL"
        | Skipped -> "SKIP"
