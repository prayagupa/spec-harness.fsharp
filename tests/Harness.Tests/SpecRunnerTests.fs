namespace Harness.Tests

open System.IO
open Xunit
open Harness.Core

module SpecRunnerTests =

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
    let ``run filters entries by tag`` () =
        let root = repoRoot ()
        let specsDir = Path.Combine(root, "specs")

        let entries =
            match SpecLoader.loadDirectory root specsDir with
            | Ok loaded -> loaded
            | Error message -> failwith message

        let registry = MatcherRegistry.createDefault root

        let options =
            { TargetOverride = None
              RepoRoot = root
              FailFast = false
              TagFilter = Some [ "demo-only" ]
              IdFilter = None }

        let summary = SpecRunner.run entries registry options
        Assert.Equal(1, summary.Total)
        Assert.Equal(1, summary.Failed)

    [<Fact>]
    let ``run fail-fast stops after first failure`` () =
        let root = repoRoot ()

        let entries =
            [ { Target = "src/BillingApp"
                SourceFile = "inline"
                Entry =
                    { Id = "fail-1"
                      Description = "missing file"
                      Category = FileLayout
                      Tags = []
                      Assertion = FileExists "missing-1.txt" } }
              { Target = "src/BillingApp"
                SourceFile = "inline"
                Entry =
                    { Id = "fail-2"
                      Description = "missing file"
                      Category = FileLayout
                      Tags = []
                      Assertion = FileExists "missing-2.txt" } } ]

        let registry = MatcherRegistry.createDefault root

        let options =
            { TargetOverride = None
              RepoRoot = root
              FailFast = true
              TagFilter = None
              IdFilter = None }

        let summary = SpecRunner.run entries registry options
        Assert.Equal(1, summary.Total)
        Assert.Equal(1, summary.Failed)
