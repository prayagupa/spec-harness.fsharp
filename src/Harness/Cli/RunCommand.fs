namespace Harness.Cli

open System
open System.IO
open Harness.Core

module RunCommand =

    let execute (config: CliConfig) =
        match CliHelpers.findRepoRoot () with
        | None ->
            eprintfn "Could not find repository root (codebase-explainer.sln)"
            2
        | Some repoRoot ->
            let specsPath =
                CliHelpers.resolveSpecsDir repoRoot (Some config.SpecsDir) config.SpecFile

            let specFile =
                if specsPath.EndsWith(".yaml") then Some specsPath else None

            let specsDir =
                if specsPath.EndsWith(".yaml") then
                    Path.GetDirectoryName specsPath
                else
                    specsPath

            match CliHelpers.loadEntries repoRoot specsDir specFile with
            | Error message ->
                eprintfn "%s" message
                2
            | Ok entries ->
                let options =
                    { TargetOverride = config.Target
                      RepoRoot = repoRoot
                      FailFast = config.FailFast
                      TagFilter =
                        if List.isEmpty config.Tags then None else Some config.Tags
                      IdFilter = if List.isEmpty config.Ids then None else Some config.Ids }

                let registry = MatcherRegistry.createDefault repoRoot
                let summary = SpecRunner.run entries registry options

                Reporter.writeReport summary config.Format config.Output
                CliHelpers.exitCodeForSummary summary
