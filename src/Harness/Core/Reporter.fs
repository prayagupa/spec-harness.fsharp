namespace Harness.Core

open System
open System.IO
open System.Text
open System.Text.Json
open System.Text.Json.Serialization

module Reporter =

    type JsonResult =
        { id: string
          status: string
          message: string option
          expected: string option
          actual: string option
          durationMs: int64 }

    type JsonReport =
        { total: int
          passed: int
          failed: int
          skipped: int
          results: JsonResult list }

    let private jsonOptions =
        let options = JsonSerializerOptions(WriteIndented = true)
        options.DefaultIgnoreCondition <- JsonIgnoreCondition.WhenWritingNull
        options

    let formatText (summary: RunSummary) (useColor: bool) =
        let sb = StringBuilder()

        sb
            .AppendLine(
                $"Spec Results ({summary.Passed} passed, {summary.Failed} failed, {summary.Skipped} skipped)"
            )
            .AppendLine()
            |> ignore

        sb.AppendLine(sprintf "%-26s %-8s %-8s %s" "ID" "STATUS" "TIME" "MESSAGE") |> ignore

        for result in summary.Results do
            let status = SpecStatus.toDisplayString result.Status
            let time = $"{result.DurationMs}ms"
            let message = result.Message |> Option.defaultValue ""

            sb.AppendLine(sprintf "%-26s %-8s %-8s %s" result.Id status time message)
            |> ignore

            match result.Expected, result.Actual with
            | Some expected, Some actual ->
                sb.AppendLine($"  expected: {expected}") |> ignore
                sb.AppendLine($"  actual:   {actual}") |> ignore
            | _ -> ()

        if useColor then
            sb.ToString()
        else
            sb.ToString()

    let formatJson (summary: RunSummary) =
        let results =
            summary.Results
            |> List.map (fun result ->
                { id = result.Id
                  status = SpecStatus.toReportString result.Status
                  message = result.Message
                  expected = result.Expected
                  actual = result.Actual
                  durationMs = result.DurationMs })

        let report =
            { total = summary.Total
              passed = summary.Passed
              failed = summary.Failed
              skipped = summary.Skipped
              results = results }

        JsonSerializer.Serialize(report, jsonOptions)

    let writeReport (summary: RunSummary) (format: string) (outputPath: string option) =
        let useColor =
            outputPath.IsNone
            && Console.IsOutputRedirected |> not

        let content =
            match format.ToLowerInvariant() with
            | "json" -> formatJson summary
            | _ -> formatText summary useColor

        match outputPath with
        | Some path -> File.WriteAllText(path, content)
        | None -> Console.Write(content)
