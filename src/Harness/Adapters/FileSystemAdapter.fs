namespace Harness.Adapters

open System.IO

type FileSystemAdapter() =
    interface ITargetAdapter with
        member _.Name = "filesystem"

        member _.Read targetRoot =
            if Directory.Exists targetRoot then
                Ok (box targetRoot)
            else
                Error $"Target directory not found: {targetRoot}"
