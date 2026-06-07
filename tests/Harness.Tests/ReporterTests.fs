namespace Harness.Tests

open Xunit
open Harness.Core

module ReporterTests =

    [<Fact>]
    let ``formatJson includes failed result details`` () =
        let summary =
            { Total = 1
              Passed = 0
              Failed = 1
              Skipped = 0
              Results =
                [ { Id = "demo"
                    Status = Failed
                    Message = Some "Signature mismatch"
                    Expected = Some "int -> int -> int"
                    Actual = Some "int -> int -> int64"
                    DurationMs = 12L } ] }

        let json = Reporter.formatJson summary

        Assert.Contains("demo", json)
        Assert.Contains("failed", json)
        Assert.Contains("int64", json)
        Assert.Contains("Signature mismatch", json)

    [<Fact>]
    let ``formatText renders summary header`` () =
        let summary =
            { Total = 2
              Passed = 2
              Failed = 0
              Skipped = 0
              Results =
                [ { Id = "a"
                    Status = Passed
                    Message = None
                    Expected = None
                    Actual = None
                    DurationMs = 1L }
                  { Id = "b"
                    Status = Passed
                    Message = None
                    Expected = None
                    Actual = None
                    DurationMs = 2L } ] }

        let text = Reporter.formatText summary false
        Assert.Contains("2 passed", text)
        Assert.Contains("a", text)
