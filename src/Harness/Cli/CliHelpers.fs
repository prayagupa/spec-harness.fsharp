namespace Harness.Cli

open System
open System.IO
open Harness.Core

module CliHelpers =

    let findRepoRoot () =
        let rec walk (dir: DirectoryInfo) =
            let sln = Path.Combine(dir.FullName, "codebase-explainer.sln")

            if File.Exists sln then
                Some dir.FullName
            elif isNull dir.Parent then
                None
            else
                walk dir.Parent

        walk (DirectoryInfo(Directory.GetCurrentDirectory()))

    let resolveSpecsDir (repoRoot: string) (specsDir: string option) (specFile: string option) =
        match specFile with
        | Some file when Path.IsPathRooted file -> file
        | Some file -> Path.Combine(repoRoot, file)
        | None ->
            specsDir
            |> Option.defaultValue "specs"
            |> fun dir ->
                if Path.IsPathRooted dir then dir else Path.Combine(repoRoot, dir)

    let loadEntries (repoRoot: string) (specsDir: string) (specFile: string option) =
        match specFile with
        | Some file ->
            SpecLoader.loadFile repoRoot file
            |> Result.map (fun doc ->
                doc.Entries
                |> List.map (fun entry ->
                    { Target = doc.Target
                      Entry = entry
                      SourceFile = file }))

        | None -> SpecLoader.loadDirectory repoRoot specsDir

    let exitCodeForSummary (summary: RunSummary) =
        if summary.Failed > 0 then 1 else 0
