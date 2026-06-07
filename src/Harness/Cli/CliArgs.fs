namespace Harness.Cli

open System
open System.Collections.Generic

type CliConfig =
    { Command: string
      SpecsDir: string
      SpecFile: string option
      Format: string
      Output: string option
      FailFast: bool
      Target: string option
      Tags: string list
      Ids: string list }

module CliArgs =

    let private isFlag (arg: string) = arg.StartsWith "--"

    let private parse (args: string array) =
        let mutable command = "run"
        let mutable specsDir = "specs"
        let mutable specFile = None
        let mutable format = "text"
        let mutable output = None
        let mutable failFast = false
        let mutable target = None
        let tags = ResizeArray<string>()
        let ids = ResizeArray<string>()

        let commands = Set [ "run"; "validate"; "list" ]

        let mutable i = 0

        if args.Length > 0 && not (isFlag args.[0]) && Set.contains args.[0] commands then
            command <- args.[0]
            i <- 1

        while i < args.Length do
            match args.[i] with
            | "--specs-dir" ->
                i <- i + 1

                if i < args.Length then
                    specsDir <- args.[i]

            | "--spec" ->
                i <- i + 1

                if i < args.Length then
                    specFile <- Some args.[i]

            | "--format" ->
                i <- i + 1

                if i < args.Length then
                    format <- args.[i]

            | "--output" ->
                i <- i + 1

                if i < args.Length then
                    output <- Some args.[i]

            | "--fail-fast" -> failFast <- true
            | "--target" ->
                i <- i + 1

                if i < args.Length then
                    target <- Some args.[i]

            | "--tag" ->
                i <- i + 1

                if i < args.Length then
                    tags.Add args.[i]

            | "--id" ->
                i <- i + 1

                if i < args.Length then
                    ids.Add args.[i]

            | unknown -> failwith $"Unknown argument: {unknown}"

            i <- i + 1

        { Command = command
          SpecsDir = specsDir
          SpecFile = specFile
          Format = format
          Output = output
          FailFast = failFast
          Target = target
          Tags = tags |> Seq.toList
          Ids = ids |> Seq.toList }

    let tryParse (args: string array) =
        try
            Ok(parse args)
        with ex ->
            Error ex.Message
