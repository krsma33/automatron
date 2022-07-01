namespace Automatron

open System.Threading

open PersistorOptionsBuilder
open DispatcherOptionsBuilder
open WorkerOptionsBuilder

open Automatron.Agents
open Automatron.Agents.Coordinator
open Automatron.Agents.Persistor

module AgentBuilder =

    type AgentBuilder<'TInput, 'TOutput, 'TError> =
        { PersistorBuildOptions: PersistorBuildOptions<'TInput, 'TOutput, 'TError> option
          DispatcherBuildOptions: DispatcherBuildOptions<'TInput> option
          WorkerBuildOptions: WorkerBuildOptions<'TInput, 'TOutput, 'TError> option }

    let agentBuilder =
        { PersistorBuildOptions = None
          DispatcherBuildOptions = None
          WorkerBuildOptions = None }

    let configurePersistor
        (pbo: PersistorBuildOptions<'TInput, 'TOutput, 'TError>)
        (opts: AgentBuilder<'TInput, 'TOutput, 'TError>)
        =
        { opts with PersistorBuildOptions = Some pbo }

    let configureDispatchers
        (dbo: DispatcherBuildOptions<'TInput>)
        (opts: AgentBuilder<'TInput, 'TOutput, 'TError>)
        =
        { opts with DispatcherBuildOptions = Some dbo }

    let configureWorkers
        (wbo: WorkerBuildOptions<'TInput, 'TOutput, 'TError>)
        (opts: AgentBuilder<'TInput, 'TOutput, 'TError>)
        =
        { opts with WorkerBuildOptions = Some wbo }

    let private validateOption funcName func =
        match func with
        | Some v -> v
        | None -> failwith $"Could not build agent options. {funcName} not configured."

    let private initPersistor (persistorBuildOptions: PersistorBuildOptions<'TInput, 'TOutput, 'TError>) =
        Persistor.create
            persistorBuildOptions.PersistJobResultFunction
            persistorBuildOptions.RetrieveUnprocessedJobsFunction
            persistorBuildOptions.PersistUnprocessedJobsFunction

    let private initDispatchers
        (coordinator: MailboxProcessor<CoordinatorMessage<'TInput>>)
        (dispatcherBuildOptions: DispatcherBuildOptions<'TInput>)
        =
        [ 1u .. dispatcherBuildOptions.DegreeOfParallelisation ]
        |> List.map (fun n -> Dispatcher.create coordinator n dispatcherBuildOptions.DispatchFunction)

    let private initWorkers
        (coordinator: MailboxProcessor<CoordinatorMessage<'TInput>>)
        (persistor: MailboxProcessor<PersistorMessage<'TInput, 'TOutput, 'TError>>)
        (workerBuildOptions: WorkerBuildOptions<'TInput, 'TOutput, 'TError>)
        =
        [ 1u .. workerBuildOptions.DegreeOfParallelisation ]
        |> List.map (fun n -> Worker.create coordinator persistor n workerBuildOptions.WorkFunction)

    let startAgents (ct: CancellationToken) (opts: AgentBuilder<'TInput, 'TOutput, 'TError>) =
        let persistorBuildOptions =
            opts.PersistorBuildOptions
            |> validateOption "PersistorBuildOptions"

        let dispatcherBuildOptions =
            opts.DispatcherBuildOptions
            |> validateOption "DispatcherBuildOptions"

        let workerBuildOptions =
            opts.WorkerBuildOptions
            |> validateOption "WorkerBuildOptions"

        let persistor = persistorBuildOptions |> initPersistor
        let coordinator = persistor |> Coordinator.create ct

        let _dispatchers =
            dispatcherBuildOptions
            |> initDispatchers coordinator

        let _workers =
            workerBuildOptions
            |> initWorkers coordinator persistor

        coordinator |> waitCompletion
