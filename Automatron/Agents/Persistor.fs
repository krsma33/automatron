namespace Automatron.Agents

module Persistor =

    type PersistJobResult<'TInput, 'TOutput, 'TError> =
        | PersistJobResult of (uint * 'TInput * Result<'TOutput, 'TError> -> Async<unit>)

    type RetrieveUnprocessedJobs<'TInput> = RetrieveUnprocessedJobs of (unit -> Async<'TInput list option>)
    type PersistUnprocessedJobs<'TInput> = PersistUnprocessedJobs of ('TInput list -> Async<unit>)

    type PersistorMessage<'TInput, 'TOutput, 'TError> =
        | RetrieveNotProcessedJobs of AsyncReplyChannel<'TInput list option>
        | PersistJobInfo of jobResult: uint * 'TInput * Result<'TOutput, 'TError>
        | StopRequest of unprocessedJobs: 'TInput list * isStoppedReply: AsyncReplyChannel<bool>

    let create
        (PersistJobResult persistJobResult: PersistJobResult<'TInput, 'TOutput, 'TError>)
        (RetrieveUnprocessedJobs retrieveUnprocessedJobs: RetrieveUnprocessedJobs<'TInput>)
        (PersistUnprocessedJobs persistUnprocessedJobs: PersistUnprocessedJobs<'TInput> )
        =
        MailboxProcessor<PersistorMessage<'TInput, 'TOutput, 'TError>>.Start
            (fun inbox ->
                let rec loop () =
                    async {
                        let! msg = inbox.Receive()

                        match msg with
                        | RetrieveNotProcessedJobs rc ->
                            let! jobs = retrieveUnprocessedJobs ()
                            rc.Reply(jobs)
                            return! loop ()
                        | PersistJobInfo (id, input, output) ->
                            do! persistJobResult (id, input, output)
                            return! loop ()
                        | StopRequest (jobs, rc) ->
                            do! persistUnprocessedJobs jobs

                            match inbox.CurrentQueueLength = 0 with
                            | true -> rc.Reply(true)
                            | false ->
                                rc.Reply(false)
                                return! loop ()
                    }

                loop ())