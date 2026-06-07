namespace Harness.Tests

open System.IO
open Xunit
open Harness.Core
open Harness.Matchers

module MatcherTests =

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
    let ``FileLayoutMatcher detects existing file`` () =
        let root = repoRoot ()
        let matcher = FileLayoutMatcher() :> IMatcher

        let entry =
            { Id = "test-readme"
              Description = "test"
              Category = FileLayout
              Tags = []
              Assertion = FileExists "README.md" }

        let result = matcher.Evaluate(entry, root, Path.Combine(root, "src", "BillingApp"))
        Assert.Equal(Passed, result.Status)

    [<Fact>]
    let ``FileLayoutMatcher reports missing file`` () =
        let root = repoRoot ()
        let matcher = FileLayoutMatcher() :> IMatcher

        let entry =
            { Id = "test-missing"
              Description = "test"
              Category = FileLayout
              Tags = []
              Assertion = FileExists "does-not-exist.txt" }

        let result = matcher.Evaluate(entry, root, Path.Combine(root, "src", "BillingApp"))
        Assert.Equal(Failed, result.Status)
        Assert.Equal(Some "missing", result.Actual)

    [<Fact>]
    let ``PublicApiMatcher validates InvoiceTotals GrandTotal signature`` () =
        let root = repoRoot ()
        let matcher = PublicApiMatcher() :> IMatcher
        let target = Path.Combine(root, "src", "BillingApp")

        let entry =
            { Id = "grand-total"
              Description = "test"
              Category = PublicApi
              Tags = []
              Assertion =
                MemberExists("BillingApp.InvoiceTotals", "GrandTotal", Some "decimal -> decimal -> decimal") }

        let result = matcher.Evaluate(entry, root, target)
        Assert.Equal(Passed, result.Status)

    [<Fact>]
    let ``DocumentationMatcher finds symbol in README`` () =
        let root = repoRoot ()
        let matcher = DocumentationMatcher(root) :> IMatcher

        let entry =
            { Id = "readme-invoicing"
              Description = "test"
              Category = Documentation
              Tags = []
              Assertion = DocMentions "InvoiceTotals" }

        let result = matcher.Evaluate(entry, root, Path.Combine(root, "src", "BillingApp"))
        Assert.Equal(Passed, result.Status)
