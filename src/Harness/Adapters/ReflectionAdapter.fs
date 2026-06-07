namespace Harness.Adapters

open System
open System.Diagnostics
open System.IO
open System.Reflection

module ReflectionAdapter =

    let private findProjectFile (targetRoot: string) =
        let candidates =
            Directory.EnumerateFiles(targetRoot, "*.fsproj", SearchOption.TopDirectoryOnly)
            |> Seq.toList

        match candidates with
        | [ project ] -> Ok project
        | [] -> Error $"No .fsproj found in {targetRoot}"
        | many ->
            let joined = String.Join(", ", many)
            Error $"Multiple .fsproj files found in {targetRoot}: {joined}"

    let private dllPath (targetRoot: string) =
        Path.Combine(targetRoot, "bin", "Debug", "net10.0", $"{Path.GetFileName(targetRoot)}.dll")

    let private needsBuild (targetRoot: string) (projectFile: string) =
        let dll = dllPath targetRoot

        if not (File.Exists dll) then
            true
        else
            let dllTime = File.GetLastWriteTimeUtc dll
            let sourceFiles =
                Directory.EnumerateFiles(targetRoot, "*.*", SearchOption.AllDirectories)
                |> Seq.filter (fun (f: string) ->
                    let ext = Path.GetExtension f
                    ext = ".fs" || ext = ".fsproj")

            Seq.exists (fun (f: string) -> File.GetLastWriteTimeUtc f > dllTime) sourceFiles
            || File.GetLastWriteTimeUtc projectFile > dllTime

    let buildTarget (targetRoot: string) =
        match findProjectFile targetRoot with
        | Error message -> Error message
        | Ok projectFile ->
            if needsBuild targetRoot projectFile then
                let psi =
                    ProcessStartInfo(
                        FileName = "dotnet",
                        Arguments = $"build \"{projectFile}\" -v q",
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        UseShellExecute = false
                    )

                use proc = Process.Start psi

                if isNull proc then
                    Error "Failed to start dotnet build"
                else
                    let stdout = proc.StandardOutput.ReadToEnd()
                    let stderr = proc.StandardError.ReadToEnd()
                    proc.WaitForExit()

                    if proc.ExitCode <> 0 then
                        Error $"dotnet build failed (exit {proc.ExitCode}): {stderr}{stdout}"
                    else
                        Ok()
            else
                Ok()

    let loadAssembly (targetRoot: string) =
        match buildTarget targetRoot with
        | Error message -> Error message
        | Ok() ->
            let dll = dllPath targetRoot

            if not (File.Exists dll) then
                Error $"Built assembly not found: {dll}"
            else
                Ok(Assembly.LoadFrom dll)

type ReflectionAdapter() =
    interface ITargetAdapter with
        member _.Name = "reflection"

        member _.Read targetRoot =
            ReflectionAdapter.loadAssembly targetRoot |> Result.map box
