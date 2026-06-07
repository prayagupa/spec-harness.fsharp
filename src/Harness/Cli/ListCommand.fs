namespace Harness.Cli

open System.IO
open Harness.Core

module ListCommand =

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
                printfn "%-26s %-16s %s" "ID" "CATEGORY" "SOURCE"

                entries
                |> List.iter (fun loaded ->
                    printfn
                        "%-26s %-16s %s"
                        loaded.Entry.Id
                        (SpecCategory.toString loaded.Entry.Category)
                        loaded.SourceFile)

                0
