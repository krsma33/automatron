namespace Automatron.Agents

open System

module AgentTypes =

    [<AutoOpen>]
    module GeneralTypes =

        type RuntimeFailiure =
            { ExceptionType: string
              ExceptionMessage: string
              StackTrace: string }

        [<Struct>]
        type OutputResult<'TOutput, 'TError> =
            | Success of SuccessValue: 'TOutput
            | BusinessRuleFailiure of BusinessRuleFailiureValue: 'TError
            | RuntimeFailiure of RuntimeFailiure

    [<AutoOpen>]
    module DispatcherTypes =

        type DispatcherId = DispatcherId of Guid
        type DispatchJobs<'TInput> = DispatchJobs of (DispatcherId -> Async<'TInput list option>)

    [<AutoOpen>]
    module WorkerTypes =

        type WorkerId = WorkerId of Guid
        type ProcessJob<'TInput, 'TOutput, 'TError> = ProcessJob of (WorkerId * 'TInput -> Async<Result<'TOutput, 'TError>>)

    [<AutoOpen>]
    module CoordinatorTypes =

        type JobId = JobId of Guid

        type Job<'TInput> =
            { Id: JobId
              DispatcherId: DispatcherId
              Dispatched: DateTimeOffset
              Input: 'TInput }

        type CompletedJob<'TInput, 'TOutput, 'TError> =
            { Id: JobId
              DispatcherId: DispatcherId
              WorkerId: WorkerId
              Dispatched: DateTimeOffset
              Started: DateTimeOffset
              Completed: DateTimeOffset
              Input: 'TInput
              Output: OutputResult<'TOutput, 'TError> }

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

        type RetrieveCompletedJobs<'TInput, 'TOutput, 'TError> =
            | RetrieveCompletedJobs of (DateTimeOffset -> DateTimeOffset -> Async<CompletedJob<'TInput, 'TOutput, 'TError> list>)

        type RetrieveUnprocessedJobs<'TInput> = RetrieveUnprocessedJobs of (unit -> Async<Job<'TInput> list>)
        type PersistUnprocessedJobs<'TInput> = PersistUnprocessedJobs of (Job<'TInput> list -> Async<unit>)

        type PersistorMessage<'TInput, 'TOutput, 'TError> =
            | RetrieveNotProcessedJobs of AsyncReplyChannel<Job<'TInput> list>
            | RetrieveCompletedJobsInfo of completedFrom:DateTimeOffset * completedTo:DateTimeOffset * AsyncReplyChannel<CompletedJob<'TInput, 'TOutput, 'TError> list>
            | PersistJobInfo of CompletedJob<'TInput, 'TOutput, 'TError>
            | StopRequest of unprocessedJobs: Job<'TInput> list * isStoppedReply: AsyncReplyChannel<bool>
