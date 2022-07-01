namespace Automatron

open Automatron.Agents.Persistor

module PersistorOptionsBuilder =

    type PersistorBuildOptions<'TInput, 'TOutput, 'TError> =
        { PersistJobResultFunction: PersistJobResult<'TInput, 'TOutput, 'TError>
          RetrieveUnprocessedJobsFunction: RetrieveUnprocessedJobs<'TInput>
          PersistUnprocessedJobsFunction: PersistUnprocessedJobs<'TInput> }

    type PersistorOptionsBuilder<'TInput, 'TOutput, 'TError> =
        { PersistJobResultFunction: PersistJobResult<'TInput, 'TOutput, 'TError> option
          RetrieveUnprocessedJobsFunction: RetrieveUnprocessedJobs<'TInput> option
          PersistUnprocessedJobsFunction: PersistUnprocessedJobs<'TInput> option }

    let persistorOptionsBuilder =
        { PersistJobResultFunction = None
          RetrieveUnprocessedJobsFunction = None
          PersistUnprocessedJobsFunction = None }

    let configurePersistJobResult
        (func: uint * 'TInput * Result<'TOutput, 'TError> -> Async<unit>)
        (opts: PersistorOptionsBuilder<'TInput, 'TOutput, 'TError>)
        =
        { opts with PersistJobResultFunction = Some <| PersistJobResult func }

    let configureRetrieveUnprocessedJobs
        (func: unit -> Async<'TInput list option>)
        (opts: PersistorOptionsBuilder<'TInput, 'TOutput, 'TError>)
        =
        { opts with RetrieveUnprocessedJobsFunction = Some <| RetrieveUnprocessedJobs func }

    let configurePersistUnprocessedJobs
        (func: 'TInput list -> Async<unit>)
        (opts: PersistorOptionsBuilder<'TInput, 'TOutput, 'TError>)
        =
        { opts with PersistUnprocessedJobsFunction = Some <| PersistUnprocessedJobs func }

    let private validateOption funcName func =
        match func with
        | Some v -> v
        | None -> failwith $"Could not build persistor options. {funcName} not configured."

    let buildPersistorOptions
        (opts: PersistorOptionsBuilder<'TInput, 'TOutput, 'TError>)
        : PersistorBuildOptions<'TInput, 'TOutput, 'TError> =
        { PersistJobResultFunction =
            opts.PersistJobResultFunction
            |> validateOption "PersistJobResultFunction"
          RetrieveUnprocessedJobsFunction =
            opts.RetrieveUnprocessedJobsFunction
            |> validateOption "RetrieveUnprocessedJobsFunction"
          PersistUnprocessedJobsFunction =
            opts.PersistUnprocessedJobsFunction
            |> validateOption "PersistUnprocessedJobsFunction" }
