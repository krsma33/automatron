namespace Automatron.Agents

open Persistor
open Coordinator

module Worker =

    type ProcessJob<'TInput, 'TOutput, 'TError> = ProcessJob of ('TInput -> Async<Result<'TOutput, 'TError>>)

    let create
        (coordinator: MailboxProcessor<CoordinatorMessage<'TInput>>)
        (persistor: MailboxProcessor<PersistorMessage<'TInput, 'TOutput, 'TError>>)
        id
        (ProcessJob workerFunction: ProcessJob<'TInput, 'TOutput, 'TError>)
        =
        MailboxProcessor.Start (fun inbox ->

            coordinator.Post(RegisterWorker id |> WorkerMessage)

            let rec loop () =
                async {
                    let! shouldStop = coordinator.PostAndAsyncReply(fun rc -> ShouldStopWorker (id,rc) |> WorkerMessage)

                    match shouldStop with
                    | true -> coordinator.Post(WorkerStopped id |> WorkerMessage)
                    | false ->
                        let! response = coordinator.PostAndAsyncReply(fun rc -> WorkRequest rc |> WorkerMessage)

                        match response with
                        | Some input ->
                            let! output = workerFunction input
                            persistor.Post(PersistJobInfo(id, input, output))
                        | None -> do! Async.Sleep(300)

                        return! loop ()
                }

            loop ())