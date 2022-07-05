namespace Automatron.Repository.LiteDB

open System
open System.IO
open LiteDB
open Automatron.Agents.AgentTypes

module internal Repository =

    let internal createRepositoryFolder () =
        Directory.CreateDirectory("LiteDB") |> ignore

    let persistUnprocessedJobs<'TInput> (input: Job<'TInput> list) =
        async {
            use db = new LiteDatabase("LiteDB/UnprocessedJobs.db")
            let jobs = db.GetCollection<Job<'TInput>>("Jobs")

            jobs.InsertBulk(input) |> ignore
        }

    let retrieveUnprocessedJobs<'TInput> () =
        async {
            use db = new LiteDatabase("LiteDB/UnprocessedJobs.db")
            let jobs = db.GetCollection<Job<'TInput>>("Jobs")

            let unprocessedJobs = jobs.FindAll() |> List.ofSeq

            jobs.DeleteAll() |> ignore

            return unprocessedJobs
        }

    let persistJobResult<'TInput, 'TOutput, 'TError> (job: CompletedJob<'TInput, 'TOutput, 'TError>) =
        async {
            use db = new LiteDatabase("LiteDB/CompletedJobs.db")
            let jobs = db.GetCollection<CompletedJob<'TInput, 'TOutput, 'TError>>("Jobs")

            jobs.Insert(job) |> ignore
        }

    let retrieveJobResult<'TInput, 'TOutput, 'TError> (fromTime: DateTimeOffset) (toTime: DateTimeOffset) =
        async {
            use db = new LiteDatabase("LiteDB/CompletedJobs.db")
            let jobs = db.GetCollection<CompletedJob<'TInput, 'TOutput, 'TError>>("Jobs")

            let completedJobs =
                jobs.Find(fun j -> j.Completed >= fromTime && j.Completed <= toTime)
                |> List.ofSeq

            return completedJobs
        }
