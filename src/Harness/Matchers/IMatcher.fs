namespace Harness.Matchers

open Harness.Core

type IMatcher =
    abstract Category: SpecCategory
    abstract Evaluate: entry: SpecEntry * repoRoot: string * targetRoot: string -> SpecResult
