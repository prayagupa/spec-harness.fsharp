namespace Harness.Tests

open System.IO
open Xunit
open Harness.Core

module SpecLoaderTests =

    let private repoRoot () =
        let rec walk (dir: DirectoryInfo) =
            let sln = Path.Combine(dir.FullName, "codebase-explainer.sln")

            if File.Exists sln then
                dir.FullName
            elif isNull dir.Parent then
                failwith "Could not locate repository root"
            else
                walk dir.Parent

        walk (DirectoryInfo(Directory.GetCurrentDirectory()))

    [<Fact>]
    let ``loadDirectory loads all spec files`` () =
        let root = repoRoot ()
        let specsDir = Path.Combine(root, "specs")

        match SpecLoader.loadDirectory root specsDir with
        | Ok entries -> Assert.True(entries.Length >= 5)
        | Error message -> Assert.Fail message

    [<Fact>]
    let ``loadFile rejects invalid assertion`` () =
        let root = repoRoot ()
        let fixture = Path.Combine(root, "tests", "Harness.Tests", "Fixtures", "invalid-spec.yaml")

        match SpecLoader.loadFile root fixture with
        | Ok _ -> Assert.Fail "Expected invalid spec to fail"
        | Error message -> Assert.Contains("path", message)

    [<Fact>]
    let ``validateOnly passes for specs directory`` () =
        let root = repoRoot ()
        let specsDir = Path.Combine(root, "specs")

        match SpecLoader.validateOnly root specsDir with
        | Ok() -> Assert.True true
        | Error message -> Assert.Fail message
