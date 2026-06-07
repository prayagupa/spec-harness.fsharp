namespace Harness.Core

open System
open System.Collections.Generic
open System.IO
open System.Text.Json
open System.Text.Json.Serialization
open YamlDotNet.Serialization
open YamlDotNet.Serialization.NamingConventions
open NJsonSchema

module SpecLoader =

    type AssertionDto() =
        member val Type = "" with get, set
        member val Path = null with get, set
        member val Substring = null with get, set
        member val TypeName = null with get, set
        member val MemberName = null with get, set
        member val Signature = null with get, set
        member val Symbol = null with get, set

    type EntryDto() =
        member val Id = "" with get, set
        member val Description = "" with get, set
        member val Category = "" with get, set
        member val Tags: string[] = [||] with get, set
        member val Assertion = Unchecked.defaultof<AssertionDto> with get, set

    type DocumentDto() =
        member val Version = "" with get, set
        member val Target = "" with get, set
        member val Entries: EntryDto[] = [||] with get, set

    let private yamlDeserializer =
        DeserializerBuilder()
            .WithNamingConvention(CamelCaseNamingConvention.Instance)
            .IgnoreUnmatchedProperties()
            .Build()

    let private jsonOptions =
        let options = JsonSerializerOptions(PropertyNamingPolicy = JsonNamingPolicy.CamelCase)
        options.DefaultIgnoreCondition <- JsonIgnoreCondition.WhenWritingNull
        options

    let private schemaPath repoRoot =
        let candidates =
            [ Path.Combine(repoRoot, "specs", "schema.json")
              Path.Combine(AppContext.BaseDirectory, "specs", "schema.json") ]

        candidates |> List.tryFind File.Exists

    let private validateAgainstSchema (repoRoot: string) (json: string) (sourcePath: string) =
        match schemaPath repoRoot with
        | None -> Ok()
        | Some path ->
            async {
                let! schema = NJsonSchema.JsonSchema.FromFileAsync(path) |> Async.AwaitTask
                let errors = schema.Validate(json)

                if errors.Count = 0 then
                    return Ok()
                else
                    let message =
                        errors
                        |> Seq.map (fun e -> e.ToString())
                        |> String.concat "; "

                    return Error $"Schema validation failed for {sourcePath}: {message}"
            }
            |> Async.RunSynchronously

    let private mapAssertion (dto: AssertionDto) =
        match dto.Type with
        | "file-exists" ->
            if String.IsNullOrWhiteSpace dto.Path then
                Error "file-exists assertion requires path"
            else
                Ok(FileExists dto.Path)

        | "file-contains" ->
            if String.IsNullOrWhiteSpace dto.Path then
                Error "file-contains assertion requires path"
            elif String.IsNullOrWhiteSpace dto.Substring then
                Error "file-contains assertion requires substring"
            else
                Ok(FileContains(dto.Path, dto.Substring))

        | "type-exists" ->
            if String.IsNullOrWhiteSpace dto.TypeName then
                Error "type-exists assertion requires typeName"
            else
                Ok(TypeExists dto.TypeName)

        | "member-exists" ->
            if String.IsNullOrWhiteSpace dto.TypeName then
                Error "member-exists assertion requires typeName"
            elif String.IsNullOrWhiteSpace dto.MemberName then
                Error "member-exists assertion requires memberName"
            else
                let signature =
                    if String.IsNullOrWhiteSpace dto.Signature then None else Some dto.Signature

                Ok(MemberExists(dto.TypeName, dto.MemberName, signature))

        | "doc-mentions" ->
            if String.IsNullOrWhiteSpace dto.Symbol then
                Error "doc-mentions assertion requires symbol"
            else
                Ok(DocMentions dto.Symbol)

        | other -> Error $"Unknown assertion type '{other}'"

    let private mapEntry (dto: EntryDto) =
        if isNull (box dto.Assertion) then
            Error "Spec entry requires assertion"
        else
            match SpecCategory.ofString dto.Category with
            | Error message -> Error message
            | Ok category ->
                match mapAssertion dto.Assertion with
                | Error message -> Error message
                | Ok assertion ->
                    Ok
                        { Id = dto.Id
                          Description = dto.Description
                          Category = category
                          Tags = dto.Tags |> Array.toList
                          Assertion = assertion }

    let private mapDocument (dto: DocumentDto) =
        if String.IsNullOrWhiteSpace dto.Version then
            Error "Spec document requires version"
        elif String.IsNullOrWhiteSpace dto.Target then
            Error "Spec document requires target"
        elif isNull dto.Entries || dto.Entries.Length = 0 then
            Error "Spec document requires at least one entry"
        else
            dto.Entries
            |> Array.toList
            |> List.map mapEntry
            |> List.fold (fun acc item ->
                match acc, item with
                | Error e, _ -> Error e
                | Ok list, Error e -> Error e
                | Ok list, Ok entry -> Ok(entry :: list)) (Ok [])
            |> Result.map (fun entries ->
                { Version = dto.Version
                  Target = dto.Target
                  Entries = List.rev entries })

    let loadFile (repoRoot: string) (path: string) =
        try
            let yaml = File.ReadAllText path
            let dto = yamlDeserializer.Deserialize<DocumentDto>(yaml)
            let json = JsonSerializer.Serialize(dto, jsonOptions)

            match validateAgainstSchema repoRoot json path with
            | Error message -> Error message
            | Ok() -> mapDocument dto
        with ex ->
            Error $"Failed to parse {path}: {ex.Message}"

    let loadDirectory (repoRoot: string) (specsDir: string) =
        if not (Directory.Exists specsDir) then
            Error $"Specs directory not found: {specsDir}"
        else
            let files =
                Directory.EnumerateFiles(specsDir, "*.yaml", SearchOption.TopDirectoryOnly)
                |> Seq.sort
                |> Seq.toList

            if List.isEmpty files then
                Error $"No .yaml spec files found in {specsDir}"
            else
                let documents =
                    files
                    |> List.map (fun file ->
                        loadFile repoRoot file
                        |> Result.map (fun doc -> file, doc))
                    |> List.fold (fun acc item ->
                        match acc, item with
                        | Error e, _ -> Error e
                        | Ok docs, Error e -> Error e
                        | Ok docs, Ok pair -> Ok(pair :: docs)) (Ok [])

                match documents with
                | Error message -> Error message
                | Ok docs ->
                    let idMap = Dictionary<string, string>()

                    let duplicates =
                        docs
                        |> List.collect (fun (file, doc) ->
                            doc.Entries
                            |> List.choose (fun entry ->
                                if idMap.ContainsKey entry.Id then
                                    Some($"{entry.Id}: {idMap.[entry.Id]} and {file}")
                                else
                                    idMap.[entry.Id] <- file
                                    None))

                    if not (List.isEmpty duplicates) then
                        let joined = String.Join("; ", duplicates)
                        Error $"Duplicate spec ids found: {joined}"
                    else
                        let loaded =
                            docs
                            |> List.collect (fun (file, doc) ->
                                doc.Entries
                                |> List.map (fun entry ->
                                    { Target = doc.Target
                                      Entry = entry
                                      SourceFile = file }))

                        Ok loaded

    let validateOnly (repoRoot: string) (specsDir: string) =
        loadDirectory repoRoot specsDir |> Result.map (fun _ -> ())
