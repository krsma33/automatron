namespace Automatron.Agents

open AgentTypes

module Persistor =

    let create
        (PersistJobResult persistJobResult: PersistJobResult<'TInput, 'TOutput, 'TError>)
        (RetrieveCompletedJobs retrieveCompletedJobs: RetrieveCompletedJobs<'TInput, 'TOutput, 'TError>)
        (RetrieveUnprocessedJobs retrieveUnprocessedJobs: RetrieveUnprocessedJobs<'TInput>)
        (PersistUnprocessedJobs persistUnprocessedJobs: PersistUnprocessedJobs<'TInput>)
        =
        new MailboxProcessor<PersistorMessage<'TInput, 'TOutput, 'TError>>(fun inbox ->
            let rec loop () =
                async {
                    let! msg = inbox.Receive()

                    match msg with
                    | RetrieveNotProcessedJobs rc ->
                        let! jobs = retrieveUnprocessedJobs ()
                        rc.Reply(jobs)
                        return! loop ()
                    | RetrieveCompletedJobsInfo (completedFrom, completedTo, rc) ->
                        let! completedJobs = retrieveCompletedJobs completedFrom completedTo
                        rc.Reply(completedJobs)
                        return! loop ()
                    | PersistJobInfo completedJob ->
                        do! persistJobResult completedJob
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
