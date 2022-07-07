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

    let private tryWorkerFunction (id, input) (ProcessJob workerFunction) =
        async {
            let! output = workerFunction (id, input) |> Async.Catch

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

    let private processJob workerFunction (id, job:Job<'TInput>) =
        async {
            let started = DateTimeOffset.Now

            let! output = workerFunction |> tryWorkerFunction (id, job.Input)

            let completed = DateTimeOffset.Now

            return job |> completedJob id started completed output
        }

    let create
        (coordinator: MailboxProcessor<CoordinatorMessage<'TInput>>)
        (persistor: MailboxProcessor<PersistorMessage<'TInput, 'TOutput, 'TError>>)
        workerFunction
        =
        new MailboxProcessor<_>(fun _ ->

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
                            let! completedJob = processJob workerFunction (id, job)
                            persistor.Post(PersistJobInfo(completedJob))
                        | None -> do! Async.Sleep(300)

                        return! loop ()
                }

            loop ())
