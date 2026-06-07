namespace Harness.Core

open System
open System.IO

module SpecRunner =

    let private matchesTags (entry: SpecEntry) (tags: string list option) =
        match tags with
        | None -> true
        | Some required ->
            required
            |> List.forall (fun tag -> List.contains tag entry.Tags)

    let private matchesIds (entry: SpecEntry) (ids: string list option) =
        match ids with
        | None -> true
        | Some required -> List.contains entry.Id required

    let private evaluateEntry (registry: MatcherRegistry) (options: RunOptions) (loaded: LoadedEntry) =
        let targetRoot =
            options.TargetOverride
            |> Option.defaultValue loaded.Target
            |> fun path -> Path.Combine(options.RepoRoot, path)

        if not (System.IO.Directory.Exists targetRoot) && loaded.Entry.Category <> FileLayout && loaded.Entry.Category <> Documentation then
            { Id = loaded.Entry.Id
              Status = Failed
              Message = Some $"Target path not found: {targetRoot}"
              Expected = None
              Actual = None
              DurationMs = 0L }
        else
            match MatcherRegistry.resolve registry loaded.Entry.Category with
            | None ->
                { Id = loaded.Entry.Id
                  Status = Failed
                  Message = Some $"No matcher registered for category '{SpecCategory.toString loaded.Entry.Category}'"
                  Expected = None
                  Actual = None
                  DurationMs = 0L }

            | Some matcher ->
                try
                    matcher.Evaluate(loaded.Entry, options.RepoRoot, targetRoot)
                with ex ->
                    { Id = loaded.Entry.Id
                      Status = Failed
                      Message = Some ex.Message
                      Expected = None
                      Actual = None
                      DurationMs = 0L }

    let run (entries: LoadedEntry list) (registry: MatcherRegistry) (options: RunOptions) =
        let filtered =
            entries
            |> List.filter (fun loaded ->
                matchesTags loaded.Entry options.TagFilter
                && matchesIds loaded.Entry options.IdFilter)

        let results = ResizeArray<SpecResult>()
        let mutable stop = false

        for loaded in filtered do
            if not stop then
                let result = evaluateEntry registry options loaded
                results.Add result

                if options.FailFast && result.Status = Failed then
                    stop <- true

        let resultList = results |> Seq.toList

        { Total = resultList.Length
          Passed = resultList |> List.filter (fun r -> r.Status = Passed) |> List.length
          Failed = resultList |> List.filter (fun r -> r.Status = Failed) |> List.length
          Skipped = resultList |> List.filter (fun r -> r.Status = Skipped) |> List.length
          Results = resultList }
