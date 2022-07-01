namespace Automatron.Agents

open AgentTypes
open System

module Worker =

    type ProcessJob<'TInput, 'TOutput, 'TError> = ProcessJob of ('TInput -> Async<Result<'TOutput, 'TError>>)

    let completedJob workerId started completed output (job: Job<'TInput>) =
        { Id = job.Id
          DispatcherId = job.DispatcherId
          Dispatched = job.Dispatched
          Input = job.Input
          WorkerId = workerId
          Started = started
          Completed = completed
          Output = output }

    let create
        (coordinator: MailboxProcessor<CoordinatorMessage<'TInput>>)
        (persistor: MailboxProcessor<PersistorMessage<'TInput, 'TOutput, 'TError>>)
        (ProcessJob workerFunction)
        =
        MailboxProcessor.Start (fun inbox ->

            let id = WorkerId <| Guid.NewGuid()

            coordinator.Post(RegisterWorker id |> WorkerMessage)

            let rec loop () =
                async {
                    let! shouldStop = coordinator.PostAndAsyncReply(fun rc -> ShouldStopWorker(id, rc) |> WorkerMessage)

                    match shouldStop with
                    | true -> coordinator.Post(WorkerStopped id |> WorkerMessage)
                    | false ->
                        let! response = coordinator.PostAndAsyncReply(fun rc -> WorkRequest rc |> WorkerMessage)

                        match response with
                        | Some job ->
                            let started = DateTimeOffset.Now
                            let! output = workerFunction job.Input
                            let completed = DateTimeOffset.Now
                            let finishedJob = job |> completedJob id started completed output
                            persistor.Post(PersistJobInfo(finishedJob))
                        | None -> do! Async.Sleep(300)

                        return! loop ()
                }

            loop ())
