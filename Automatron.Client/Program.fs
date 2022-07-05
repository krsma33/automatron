open System
open System.Threading
open Automatron
open Automatron.Repository.LiteDB
open Automatron.DispatcherOptionsBuilder
open Automatron.WorkerOptionsBuilder
open Automatron.AgentBuilder

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

let dispatcherFunction () =
    async {
        Console.info "Dispatching..."
        do! Async.Sleep(5000)

(*        return Some 
                 [ "F"
                   "A"
                   "V"
                   "O"
                   "R"
                   "I"
                   "T"
                   "O" ]*)

        return None
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
            Console.error $"Hoho Haha forced"
            raise (new ArgumentNullException("Some Param","Hoho Haha forced"))
            return Error(RuntimeError "Hoho Haha forced")
        else
            Console.info $"Success: {input}"
            return Ok($"Success: {input}")
    }

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

let _ =
    agentBuilder
    |> configurePersistor persistorOptions
    |> configureDispatchers dispatcherOptions
    |> configureWorkers workerOptions
    |> startAgents cts.Token
    |> Async.RunSynchronously
