namespace Harness.Core

open Harness.Matchers

type MatcherRegistry =
    private { Matchers: Map<SpecCategory, IMatcher>
              RepoRoot: string }

module MatcherRegistry =

    let createDefault (repoRoot: string) =
        let matchers =
            [ FileLayoutMatcher() :> IMatcher
              PublicApiMatcher() :> IMatcher
              DocumentationMatcher(repoRoot) :> IMatcher ]
            |> List.map (fun matcher -> matcher.Category, matcher)
            |> Map.ofList

        { Matchers = matchers; RepoRoot = repoRoot }

    let resolve (registry: MatcherRegistry) (category: SpecCategory) =
        registry.Matchers |> Map.tryFind category
