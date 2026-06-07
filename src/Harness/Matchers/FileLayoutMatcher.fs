namespace Harness.Matchers

open System
open System.Diagnostics
open System.IO
open Harness.Core

type FileLayoutMatcher() =
    let pass id duration message =
        { Id = id
          Status = Passed
          Message = message
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
        member _.Category = FileLayout

        member _.Evaluate(entry, repoRoot, _targetRoot) =
            let sw = Stopwatch.StartNew()

            let result =
                match entry.Assertion with
                | FileExists path ->
                    let fullPath = Path.Combine(repoRoot, path)

                    if File.Exists fullPath || Directory.Exists fullPath then
                        pass entry.Id sw.ElapsedMilliseconds None
                    else
                        fail entry.Id sw.ElapsedMilliseconds "Path does not exist" path "missing"

                | FileContains(path, substring) ->
                    let fullPath = Path.Combine(repoRoot, path)

                    if not (File.Exists fullPath) then
                        fail entry.Id sw.ElapsedMilliseconds "File does not exist" path "missing"
                    else
                        let content = File.ReadAllText fullPath

                        if content.Contains substring then
                            pass entry.Id sw.ElapsedMilliseconds None
                        else
                            fail entry.Id sw.ElapsedMilliseconds "Substring not found" substring "not found"

                | _ ->
                    { Id = entry.Id
                      Status = Failed
                      Message = Some "Assertion not supported by FileLayoutMatcher"
                      Expected = None
                      Actual = None
                      DurationMs = sw.ElapsedMilliseconds }

            sw.Stop()
            result
