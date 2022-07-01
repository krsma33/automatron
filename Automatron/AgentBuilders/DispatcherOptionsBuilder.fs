namespace Automatron

open Automatron.Agents.Dispatcher

module DispatcherOptionsBuilder =

    type DispatcherBuildOptions<'TInput> =
        { DispatchFunction: DispatchJobs<'TInput>
          DegreeOfParallelisation: uint }

    type DispatcherOptionsBuilder<'TInput> =
        { DispatchFunction: DispatchJobs<'TInput> option
          DegreeOfParallelisation: uint }

    let dispatcherOptionsBuilder =
        { DispatchFunction = None
          DegreeOfParallelisation = 1u }

    let configureDispatchFunction (func: unit -> Async<'TInput list option>) (opts: DispatcherOptionsBuilder<'TInput>) =
        { opts with DispatchFunction = Some <| DispatchJobs func }

    let configureDispatcherDegreeOfParallelisation (dop: uint) (opts: DispatcherOptionsBuilder<'TInput>) =
        { opts with DegreeOfParallelisation = dop }

    let private validateOption funcName func =
        match func with
        | Some v -> v
        | None -> failwith $"Could not build dispatcher options. {funcName} not configured."

    let buildDispatcherOptions (opts: DispatcherOptionsBuilder<'TInput>) : DispatcherBuildOptions<'TInput> =
        { DispatchFunction =
            opts.DispatchFunction
            |> validateOption "DispatchFunction"
          DegreeOfParallelisation = opts.DegreeOfParallelisation }
