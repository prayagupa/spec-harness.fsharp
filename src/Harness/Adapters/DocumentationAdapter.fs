namespace Harness.Adapters

open System.IO

type DocumentationAdapter(repoRoot: string) =
    interface ITargetAdapter with
        member _.Name = "documentation"

        member _.Read _targetRoot =
            let readme = Path.Combine(repoRoot, "README.md")

            if File.Exists readme then
                Ok (box (File.ReadAllText readme))
            else
                Error $"README.md not found at {readme}"
