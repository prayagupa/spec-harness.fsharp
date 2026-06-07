namespace Harness.Matchers

open System
open System.Diagnostics
open System.Reflection
open Microsoft.FSharp.Reflection
open Harness.Adapters
open Harness.Core

type PublicApiMatcher() =

    let formatSimpleType (t: Type) =
        if t = typeof<int> then "int"
        elif t = typeof<string> then "string"
        elif t = typeof<bool> then "bool"
        elif t = typeof<int64> then "int64"
        elif t = typeof<float> then "float"
        elif t = typeof<decimal> then "decimal"
        elif t = typeof<unit> then "unit"
        else t.Name

    let rec formatFunctionType (t: Type) =
        if FSharpType.IsFunction t then
            let domain, range = FSharpType.GetFunctionElements t
            $"{formatSimpleType domain} -> {formatFunctionType range}"
        else
            formatSimpleType t

    let formatMethodSignature (method: MethodInfo) =
        let parameters = method.GetParameters()

        if parameters.Length > 0 && not (FSharpType.IsFunction method.ReturnType) then
            let parts =
                parameters
                |> Array.map (fun p -> formatSimpleType p.ParameterType)
                |> Array.toList

            (parts @ [ formatSimpleType method.ReturnType ]) |> String.concat " -> "
        elif FSharpType.IsFunction method.ReturnType then
            formatFunctionType method.ReturnType
        else
            formatSimpleType method.ReturnType

    let normalizeSignature (signature: string) =
        signature.Trim().Replace(" ", "")

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
        member _.Category = PublicApi

        member _.Evaluate(entry, _repoRoot, targetRoot) =
            let sw = Stopwatch.StartNew()

            let result =
                match ReflectionAdapter.loadAssembly targetRoot with
                | Error message ->
                    { Id = entry.Id
                      Status = Failed
                      Message = Some message
                      Expected = None
                      Actual = None
                      DurationMs = sw.ElapsedMilliseconds }

                | Ok (assembly: Assembly) ->
                    match entry.Assertion with
                    | TypeExists typeName ->
                        let typ = assembly.GetType typeName

                        if isNull typ then
                            fail entry.Id sw.ElapsedMilliseconds "Type not found" typeName "missing"
                        else
                            pass entry.Id sw.ElapsedMilliseconds

                    | MemberExists(typeName, memberName, expectedSignature) ->
                        let typ = assembly.GetType typeName

                        if isNull typ then
                            fail entry.Id sw.ElapsedMilliseconds "Type not found" typeName "missing"
                        else
                            let flags = BindingFlags.Static ||| BindingFlags.Public
                            let method = typ.GetMethod(memberName, flags)

                            if isNull method then
                                fail entry.Id sw.ElapsedMilliseconds "Member not found" memberName "missing"
                            else
                                match expectedSignature with
                                | None -> pass entry.Id sw.ElapsedMilliseconds
                                | Some expected ->
                                    let actualSignature = formatMethodSignature method

                                    if normalizeSignature actualSignature = normalizeSignature expected then
                                        pass entry.Id sw.ElapsedMilliseconds
                                    else
                                        fail
                                            entry.Id
                                            sw.ElapsedMilliseconds
                                            "Signature mismatch"
                                            expected
                                            actualSignature

                    | _ ->
                        { Id = entry.Id
                          Status = Failed
                          Message = Some "Assertion not supported by PublicApiMatcher"
                          Expected = None
                          Actual = None
                          DurationMs = sw.ElapsedMilliseconds }

            sw.Stop()
            result
