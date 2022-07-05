namespace Automatron.Agents

open AgentTypes
open System

module Worker =

    let private completedJob workerId started completed output (job: Job<'TInput>) =
        { Id = job.Id
          DispatcherId = job.DispatcherId
          Dispatched = job.Dispatched
          Input = job.Input
          WorkerId = workerId
          Started = started
          Completed = completed
          Output = output }

    let private tryWorkerFunction (job: Job<'TInput>) (ProcessJob workerFunction) =
        async {
            let! output = workerFunction job.Input |> Async.Catch

            match output with
            | Choice1Of2 r ->
                match r with
                | Ok v -> return Success v
                | Error e -> return BusinessRuleFailiure e
            | Choice2Of2 e ->
                return
                    RuntimeFailiure
                        { ExceptionMessage = e.Message
                          ExceptionType = e.GetType().FullName
                          StackTrace = e.StackTrace }
        }

    let private processJob id workerFunction (job: Job<'TInput>) =
        async {
            let started = DateTimeOffset.Now

            let! output = workerFunction |> tryWorkerFunction job

            let completed = DateTimeOffset.Now

            return job |> completedJob id started completed output
        }

    let create
        (coordinator: MailboxProcessor<CoordinatorMessage<'TInput>>)
        (persistor: MailboxProcessor<PersistorMessage<'TInput, 'TOutput, 'TError>>)
        workerFunction
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
                            let! completedJob = processJob id workerFunction job
                            persistor.Post(PersistJobInfo(completedJob))
                        | None -> do! Async.Sleep(300)

                        return! loop ()
                }

            loop ())
