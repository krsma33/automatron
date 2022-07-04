﻿open System
open System.Threading
open Automatron
open Automatron.PersistorOptionsBuilder
open Automatron.DispatcherOptionsBuilder
open Automatron.WorkerOptionsBuilder
open Automatron.AgentBuilder
open Automatron.Agents.AgentTypes

let cts = new CancellationTokenSource()

AppDomain.CurrentDomain.ProcessExit.Add (fun _ ->
    if not cts.Token.IsCancellationRequested then
        Console.info "Graceful stop requested..."
        cts.Cancel())

System.Console.CancelKeyPress.Add (fun e ->
    if not cts.Token.IsCancellationRequested then
        Console.info "Graceful stop requested..."
        e.Cancel <- true
        cts.Cancel()
    else
        Console.info "Force stop requested..."
        e.Cancel <- false)

type Error =
    | BusinessError of string
    | RuntimeError of string

let retrieveUnprocessedJobs () =
    async {
        Console.info "Retrieving unprocessed..."
        do! Async.Sleep(500)

        return
            [ "Robo"
              "Bobo"
              "Hobo"
              "Sobo"
              "Mobo"
              "Shobo"
              "Iobo"
              "Jobo" ]
            |> List.map (fun i ->
                { Id = JobId <| Guid.NewGuid()
                  DispatcherId = DispatcherId <| Guid.NewGuid()
                  Dispatched = DateTimeOffset.Now
                  Input = i })
            |> Some
    }

let persistUnprocessedJobs (input: Job<string> list) =
    async {
        Console.warn "Persisting unprocessed..."

        input
        |> List.iter (fun j -> Console.warn (sprintf "%A" j))

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

        let n = rnd.Next(100)

        if n % 7 = 0 then
            Console.error $"Some random error"
            return Error(BusinessError "Some random error")
        elif n % 13 = 0 then
            failwith "Hoho Haha forced"
            return Error(RuntimeError "Hoho Haha forced")
        else
            Console.info $"Success: {input}"
            return Ok($"Success: {input}")
    }

let persistJobResult (job: CompletedJob<_, _, _>) =
    async {
        Console.info "Archiving output..."

        match job.Output with
        | Success o -> Console.info (sprintf "%A" job)
        | BusinessRuleFailiure e -> Console.warn (sprintf "%A" job)
        | RuntimeFailiure e -> Console.error (sprintf "%A" job)
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
