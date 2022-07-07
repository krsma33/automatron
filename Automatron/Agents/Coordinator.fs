namespace Automatron.Agents

open System.Threading
open System.Collections.Generic
open AgentTypes

module Coordinator =

    let private register id (list: ResizeArray<_>) = list.Add(id)
    let private unregister id (list: ResizeArray<_>) = list.Remove(id) |> ignore

    let private enqueue input (queue: Queue<_>) = queue.Enqueue(input)
    let private tryDequeue (queue: Queue<_>) = queue.TryDequeue()

    let private requestWork (rc: AsyncReplyChannel<_>) (queue: Queue<_>) =
        let (itemFound, job) = queue |> tryDequeue

        match itemFound with
        | true -> rc.Reply(Some(job))
        | false -> rc.Reply(None)

    let create (ct: CancellationToken) (persistor: MailboxProcessor<PersistorMessage<'TInput, 'TOutput, 'TError>>) =
        new MailboxProcessor<CoordinatorMessage<'TInput>>(fun inbox ->
            let jobsQueue = Queue<_>()
            let dispatchers = ResizeArray()
            let workers = ResizeArray()

            let rec enqueueUnprocessedMessages () =
                async {
                    let scanFunc m =
                        match m with
                        | DispatcherMessage (JobRequest input) -> Some(async { return input })
                        | _ -> None

                    let! msg = inbox.TryScan(scanFunc, 300)

                    match msg with
                    | Some input ->
                        jobsQueue |> enqueue input
                        return! enqueueUnprocessedMessages ()
                    | None -> ()
                }

            let rec retrieveUnprocessedMessages jobs =
                let (itemFound, job) = jobsQueue |> tryDequeue

                match itemFound with
                | true ->
                    let jobs = job :: jobs
                    retrieveUnprocessedMessages jobs
                | false -> jobs |> List.rev

            let rec waitPersistorToStop jobs =
                async {
                    let! isStopped = persistor.PostAndAsyncReply(fun rc -> StopRequest(jobs, rc))

                    match isStopped with
                    | true -> ()
                    | false -> do! waitPersistorToStop []
                }

            let initialize () =
                async {
                    let! jobs = persistor.PostAndAsyncReply(fun rc -> RetrieveNotProcessedJobs rc)

                    jobs
                    |> List.iter (fun j -> jobsQueue |> enqueue j)
                }

            let finalize () =
                async {
                    do! enqueueUnprocessedMessages ()
                    let jobs = retrieveUnprocessedMessages []
                    do! waitPersistorToStop jobs
                }

            let completeCheck (rc: AsyncReplyChannel<_>) =
                async {
                    let isWorkComplete =
                        ct.IsCancellationRequested
                        && dispatchers.Count = 0
                        && workers.Count = 0

                    match isWorkComplete with
                    | true ->
                        do! finalize ()
                        rc.Reply(true)
                    | false -> rc.Reply(false)
                }

            let rec loop () =
                async {
                    let! msg = inbox.Receive()

                    match msg with
                    | DispatcherMessage (RegisterDispatcher id) -> dispatchers |> register id
                    | DispatcherMessage (JobRequest input) -> jobsQueue |> enqueue input
                    | DispatcherMessage (ShouldStopDispatcher (id, rc)) -> rc.Reply(ct.IsCancellationRequested)
                    | DispatcherMessage (DispatcherStopped id) -> dispatchers |> unregister id
                    | WorkerMessage (RegisterWorker id) -> workers |> register id
                    | WorkerMessage (WorkRequest rc) -> jobsQueue |> requestWork rc
                    | WorkerMessage (ShouldStopWorker (id, rc)) -> rc.Reply(ct.IsCancellationRequested)
                    | WorkerMessage (WorkerStopped id) -> workers |> unregister id
                    | IsCompleteCheck rc -> do! completeCheck rc

                    return! loop ()
                }

            async {
                do! initialize ()
                return! loop ()
            })

    let waitCompletion (coordinator: MailboxProcessor<CoordinatorMessage<'TInput>>) =

        let rec loop () =
            async {
                let! b = coordinator.PostAndAsyncReply((fun rc -> IsCompleteCheck rc), 30000)

                match b with
                | true -> ()
                | false ->
                    do! Async.Sleep(300)
                    return! loop ()
            }

        loop ()
