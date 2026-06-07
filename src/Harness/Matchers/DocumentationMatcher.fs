namespace Harness.Matchers

open System
open System.Diagnostics
open Harness.Adapters
open Harness.Core

type DocumentationMatcher(repoRoot: string) =

    let pass id duration =
        { Id = id
          Status = Passed
          Message = None
          Expected = None
          Actual = None
          DurationMs = duration }

    let fail id duration message expected actual =
        { Id = id
          Status = Failed
          Message = Some message
          Expected = Some expected
          Actual = Some actual
          DurationMs = duration }

    interface IMatcher with
        member _.Category = Documentation

        member _.Evaluate(entry, _repoRoot, _targetRoot) =
            let sw = Stopwatch.StartNew()
            let adapter = DocumentationAdapter(repoRoot) :> ITargetAdapter

            let result =
                match entry.Assertion with
                | DocMentions symbol ->
                    match adapter.Read "" with
                    | Error message ->
                        { Id = entry.Id
                          Status = Failed
                          Message = Some message
                          Expected = None
                          Actual = None
                          DurationMs = sw.ElapsedMilliseconds }

                    | Ok content ->
                        let text = string content

                        if text.IndexOf(symbol, StringComparison.OrdinalIgnoreCase) >= 0 then
                            pass entry.Id sw.ElapsedMilliseconds
                        else
                            fail entry.Id sw.ElapsedMilliseconds "Symbol not mentioned in README" symbol "not found"

                | _ ->
                    { Id = entry.Id
                      Status = Failed
                      Message = Some "Assertion not supported by DocumentationMatcher"
                      Expected = None
                      Actual = None
                      DurationMs = sw.ElapsedMilliseconds }

            sw.Stop()
            result
