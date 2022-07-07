namespace Automatron

open Automatron.Agents.AgentTypes

module WorkerOptionsBuilder =

    type WorkerBuildOptions<'TInput, 'TOutput, 'TError> =
        { WorkFunction: ProcessJob<'TInput, 'TOutput, 'TError>
          DegreeOfParallelisation: uint }

    type WorkerOptionsBuilder<'TInput, 'TOutput, 'TError> =
        { WorkFunction: ProcessJob<'TInput, 'TOutput, 'TError> option
          DegreeOfParallelisation: uint }

    let workerOptionsBuilder =
        { WorkFunction = None
          DegreeOfParallelisation = 1u }

    let configureWorkFunction
        (func: WorkerId * 'TInput -> Async<Result<'TOutput, 'TError>>)
        (opts: WorkerOptionsBuilder<'TInput, 'TOutput, 'TError>)
        =
        { opts with WorkFunction = Some <| ProcessJob func }

    let configureWorkerDegreeOfParallelisation (dop: uint) (opts: WorkerOptionsBuilder<'TInput, 'TOutput, 'TError>) =
        { opts with DegreeOfParallelisation = dop }

    let private validateOption funcName func =
        match func with
        | Some v -> v
        | None -> failwith $"Could not build worker options. {funcName} not configured."

    let buildWorkerOptions
        (opts: WorkerOptionsBuilder<'TInput, 'TOutput, 'TError>)
        : WorkerBuildOptions<'TInput, 'TOutput, 'TError> =
        { WorkFunction = opts.WorkFunction |> validateOption "WorkFunction"
          DegreeOfParallelisation = opts.DegreeOfParallelisation }
