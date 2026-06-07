open Harness.Cli

[<EntryPoint>]
let main args =
    match CliArgs.tryParse args with
    | Error message ->
        eprintfn "%s" message
        eprintfn "Usage: harness [run|validate|list] [--specs-dir <path>] [--spec <file>] [--format text|json] [--output <file>] [--fail-fast] [--target <path>] [--tag <tag>] [--id <id>]"
        2
    | Ok config ->
        match config.Command with
        | "validate" -> ValidateCommand.execute config
        | "list" -> ListCommand.execute config
        | "run" -> RunCommand.execute config
        | other ->
            eprintfn "Unknown command: %s" other
            2
