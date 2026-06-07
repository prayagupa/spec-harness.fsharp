namespace Harness.Adapters

type ITargetAdapter =
    abstract Name: string
    abstract Read: targetRoot: string -> Result<obj, string>
