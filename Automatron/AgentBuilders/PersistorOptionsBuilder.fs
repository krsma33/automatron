namespace Automatron

open System
open Automatron.Agents.AgentTypes

module PersistorOptionsBuilder =

    type PersistorBuildOptions<'TInput, 'TOutput, 'TError> =
        { PersistJobResultFunction: PersistJobResult<'TInput, 'TOutput, 'TError>
          RetrieveCompletedJobs: RetrieveCompletedJobs<'TInput, 'TOutput, 'TError>
          RetrieveUnprocessedJobsFunction: RetrieveUnprocessedJobs<'TInput>
          PersistUnprocessedJobsFunction: PersistUnprocessedJobs<'TInput> }

    type PersistorOptionsBuilder<'TInput, 'TOutput, 'TError> =
        { PersistJobResultFunction: PersistJobResult<'TInput, 'TOutput, 'TError> option
          RetrieveCompletedJobs: RetrieveCompletedJobs<'TInput, 'TOutput, 'TError> option
          RetrieveUnprocessedJobsFunction: RetrieveUnprocessedJobs<'TInput> option
          PersistUnprocessedJobsFunction: PersistUnprocessedJobs<'TInput> option }

    let persistorOptionsBuilder =
        { PersistJobResultFunction = None
          RetrieveCompletedJobs = None
          RetrieveUnprocessedJobsFunction = None
          PersistUnprocessedJobsFunction = None }

    let configureRetrieveCompletedJobs
        (func: DateTimeOffset -> DateTimeOffset -> Async<CompletedJob<'TInput, 'TOutput, 'TError> list>)
        (opts: PersistorOptionsBuilder<'TInput, 'TOutput, 'TError>)
        =
        { opts with RetrieveCompletedJobs = Some <| RetrieveCompletedJobs func }

    let configurePersistJobResult
        (func: CompletedJob<'TInput, 'TOutput, 'TError> -> Async<unit>)
        (opts: PersistorOptionsBuilder<'TInput, 'TOutput, 'TError>)
        =
        { opts with PersistJobResultFunction = Some <| PersistJobResult func }

    let configureRetrieveUnprocessedJobs
        (func: unit -> Async<Job<'TInput> list>)
        (opts: PersistorOptionsBuilder<'TInput, 'TOutput, 'TError>)
        =
        { opts with RetrieveUnprocessedJobsFunction = Some <| RetrieveUnprocessedJobs func }

    let configurePersistUnprocessedJobs
        (func: Job<'TInput> list -> Async<unit>)
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
        { RetrieveCompletedJobs =
            opts.RetrieveCompletedJobs
            |> validateOption "RetrieveCompletedJobs"
          PersistJobResultFunction =
            opts.PersistJobResultFunction
            |> validateOption "PersistJobResultFunction"
          RetrieveUnprocessedJobsFunction =
            opts.RetrieveUnprocessedJobsFunction
            |> validateOption "RetrieveUnprocessedJobsFunction"
          PersistUnprocessedJobsFunction =
            opts.PersistUnprocessedJobsFunction
            |> validateOption "PersistUnprocessedJobsFunction" }
