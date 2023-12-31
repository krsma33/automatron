﻿namespace Automatron.Agents

open AgentTypes
open System

module Dispatcher =

    let private newJob dispatcherId input =
        { Id = JobId <| Guid.NewGuid()
          DispatcherId = dispatcherId
          Dispatched = DateTimeOffset.Now
          Input = input }

    let create (coordinator: MailboxProcessor<CoordinatorMessage<'TInput>>) (DispatchJobs dispatcherFunction) =
        new MailboxProcessor<_>(fun _ ->

            let id = DispatcherId <| Guid.NewGuid()

            coordinator.Post(RegisterDispatcher id |> DispatcherMessage)

            let rec loop () =
                async {
                    let! shouldStop =
                        coordinator.PostAndAsyncReply(fun rc -> ShouldStopDispatcher(id, rc) |> DispatcherMessage)

                    match shouldStop with
                    | true -> coordinator.Post(DispatcherStopped id |> DispatcherMessage)
                    | false ->
                        let! result = dispatcherFunction id |> Async.Catch

                        match result with
                        | Choice1Of2 r ->
                            match r with
                            | Some v ->
                                v
                                |> List.map (fun msg -> newJob id msg)
                                |> List.iter (fun msg -> coordinator.Post(JobRequest(msg) |> DispatcherMessage))
                            | None -> do! Async.Sleep(300)

                            return! loop ()
                        | Choice2Of2 e -> coordinator.Post(DispatcherStopped id |> DispatcherMessage)
                }

            loop ())
