namespace Harness.Cli

open System.IO
open Harness.Core

module ValidateCommand =

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

            match specFile with
            | Some file ->
                match SpecLoader.loadFile repoRoot file with
                | Error message ->
                    eprintfn "%s" message
                    2
                | Ok _ ->
                    printfn "Spec file is valid: %s" file
                    0
            | None ->
                match SpecLoader.validateOnly repoRoot specsDir with
                | Error message ->
                    eprintfn "%s" message
                    2
                | Ok() ->
                    printfn "All specs in %s are valid" specsDir
                    0
