open Automatron.Repository.LiteDB
open Automatron.DispatcherOptionsBuilder
open Automatron.WorkerOptionsBuilder
open Automatron.AgentBuilder
open System.Threading
open Automatron.Helpers
open Automatron.Client.Dispatcher
open Automatron.Client.Worker
open Automatron.Web

let cts = new CancellationTokenSource()

Microsoft.Playwright.Program.Main([|"install"|]) |> ignore

GracefulShutdown.register(cts)

let persistorOptions = LiteDbPersistor.initDefaultOptions()

let dispatcherOptions =
    dispatcherOptionsBuilder
    |> configureDispatchFunction dispatcherFunction
    |> configureDispatcherDegreeOfParallelisation 1u
    |> buildDispatcherOptions

let workerOptions =
    workerOptionsBuilder
    |> configureWorkFunction workerFunction
    |> configureWorkerDegreeOfParallelisation 3u
    |> buildWorkerOptions

let agents =
    agentBuilder
    |> configurePersistor persistorOptions
    |> configureDispatchers dispatcherOptions
    |> configureWorkers workerOptions
    |> buildAgents cts.Token

agents
|> startAgents
|> Async.RunSynchronously

MsPlaywright.cleanup()
|> Async.AwaitTask
|> Async.RunSynchronously