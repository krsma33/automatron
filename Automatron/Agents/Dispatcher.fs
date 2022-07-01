namespace Automatron.Agents

open Coordinator

module Dispatcher =

    type DispatchJobs<'TInput> = DispatchJobs of (unit -> Async<'TInput list option>)

    let create
        (coordinator: MailboxProcessor<CoordinatorMessage<'TInput>>)
        id
        (DispatchJobs dispatcherFunction: DispatchJobs<'TInput>)
        =
        MailboxProcessor.Start (fun inbox ->

            coordinator.Post(RegisterDispatcher id |> DispatcherMessage)

            let rec loop () =
                async {
                    let! shouldStop =
                        coordinator.PostAndAsyncReply(fun rc -> ShouldStopDispatcher(id, rc) |> DispatcherMessage)

                    match shouldStop with
                    | true -> coordinator.Post(DispatcherStopped id |> DispatcherMessage)
                    | false ->
                        let! result = dispatcherFunction ()

                        match result with
                        | Some v ->
                            v
                            |> List.iter (fun msg -> coordinator.Post(JobRequest(msg) |> DispatcherMessage))
                        | None -> do! Async.Sleep(300)

                        return! loop ()
                }

            loop ())
