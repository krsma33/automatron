namespace Automatron.Agents

open System

module AgentTypes =

    [<AutoOpen>]
    module DispatcherTypes =

        type DispatcherId = DispatcherId of Guid

    [<AutoOpen>]
    module WorkerTypes =

        type WorkerId = WorkerId of Guid

    [<AutoOpen>]
    module CoordinatorTypes =
        
        type JobId = JobId of Guid

        type Job<'TInput> = {
            Id: JobId
            DispatcherId: DispatcherId
            Dispatched: DateTimeOffset
            Input: 'TInput
        }

        type CompletedJob<'TInput, 'TOutput, 'TError> = {
            Id: JobId
            DispatcherId: DispatcherId
            Dispatched: DateTimeOffset
            Input: 'TInput
            WorkerId: WorkerId
            Started: DateTimeOffset
            Completed: DateTimeOffset
            Output: Result<'TOutput,'TError>
        }

        type DispatcherMessage<'TInput> =
            | RegisterDispatcher of DispatcherId
            | JobRequest of Job<'TInput>
            | ShouldStopDispatcher of DispatcherId * shouldStopReply: AsyncReplyChannel<bool>
            | DispatcherStopped of DispatcherId

        type WorkerMessage<'TInput> =
            | RegisterWorker of WorkerId
            | WorkRequest of AsyncReplyChannel<Job<'TInput> option>
            | ShouldStopWorker of WorkerId * shouldStopReply: AsyncReplyChannel<bool>
            | WorkerStopped of WorkerId

        type CoordinatorMessage<'TInput> =
            | DispatcherMessage of DispatcherMessage<'TInput>
            | WorkerMessage of WorkerMessage<'TInput>
            | IsCompleteCheck of AsyncReplyChannel<bool>

    [<AutoOpen>]
    module PersistorTypes =

        type PersistJobResult<'TInput, 'TOutput, 'TError> =
            | PersistJobResult of (CompletedJob<'TInput, 'TOutput, 'TError> -> Async<unit>)

        type RetrieveUnprocessedJobs<'TInput> = RetrieveUnprocessedJobs of (unit -> Async<Job<'TInput> list option>)
        type PersistUnprocessedJobs<'TInput> = PersistUnprocessedJobs of (Job<'TInput> list -> Async<unit>)

        type PersistorMessage<'TInput, 'TOutput, 'TError> =
            | RetrieveNotProcessedJobs of AsyncReplyChannel<Job<'TInput> list option>
            | PersistJobInfo of CompletedJob<'TInput, 'TOutput, 'TError>
            | StopRequest of unprocessedJobs: Job<'TInput>list * isStoppedReply: AsyncReplyChannel<bool>
