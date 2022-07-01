﻿open System
open System.Threading
open Automatron
open Automatron.PersistorOptionsBuilder
open Automatron.DispatcherOptionsBuilder
open Automatron.WorkerOptionsBuilder
open Automatron.AgentBuilder

let cts = new CancellationTokenSource()

AppDomain.CurrentDomain.ProcessExit.Add(fun _ -> cts.Cancel())

System.Console.CancelKeyPress.Add (fun e ->
    e.Cancel <- true
    cts.Cancel())

type Error =
    | BusinessError of string
    | RuntimeError of string

let retrieveUnprocessedJobs () =
    async {
        Console.info "Retrieving unprocessed..."
        do! Async.Sleep(500)

        return
            Some [ "Robo"
                   "Bobo"
                   "Hobo"
                   "Sobo"
                   "Mobo"
                   "Shobo"
                   "Iobo"
                   "Jobo" ]
    }

let persistUnprocessedJobs (input: string list) =
    async {
        Console.warn "Persisting unprocessed..."
        input |> List.iter Console.warn
        do! Async.Sleep(1000)
        Console.warn "Persisting complete"
    }

let dispatcherFunction () =
    async {
        Console.info "Dispatching..."
        do! Async.Sleep(5000)

        return
            Some [ "F"
                   "A"
                   "V"
                   "O"
                   "R"
                   "I"
                   "T"
                   "O" ]
    }

let workerFunction (input: string) =
    async {
        let rnd = new Random()
        do! Async.Sleep(1000)

        if rnd.Next(100) % 7 = 0 then
            Console.error $"Some random error"
            return Error(BusinessError "Some random error")
        else
            Console.info $"Success: {input}"
            return Ok($"Success: {input}")
    }

let persistJobResult (input: uint * string * Result<string, Error>) =
    async {
        Console.info "Archiving output..."

        match input with
        | id, i, Ok o -> Console.info (sprintf "%i: %s - %s" id i o)
        | id, i, Error e -> Console.error (sprintf "%i: %s - %A" id i e)
    }

let persistorOptions =
    persistorOptionsBuilder
    |> configurePersistJobResult persistJobResult
    |> configureRetrieveUnprocessedJobs retrieveUnprocessedJobs
    |> configurePersistUnprocessedJobs persistUnprocessedJobs
    |> buildPersistorOptions

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

let _ =
    agentBuilder
    |> configurePersistor persistorOptions
    |> configureDispatchers dispatcherOptions
    |> configureWorkers workerOptions
    |> startAgents cts.Token
    |> Async.RunSynchronously