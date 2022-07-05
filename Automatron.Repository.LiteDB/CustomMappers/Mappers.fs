namespace Automatron.Repository.LiteDB

open System
open System.Text.Json
open System.Text.Json.Serialization
open LiteDB
open Automatron.Agents.AgentTypes

module internal Mappers =
    let options = JsonSerializerOptions()
    options.Converters.Add(JsonFSharpConverter())

    let registerJobMapper<'TInput> () =
        BsonMapper.Global.RegisterType<Job<'TInput>>(
            (fun j ->
                let (JobId id) = j.Id
                let (DispatcherId dispatcherId) = j.DispatcherId
                let dispatched = DateTime <| j.Dispatched.Ticks
                let input = System.Text.Json.JsonSerializer.Serialize(j.Input, options)

                let doc = new BsonDocument()
                doc.Add("_id", id)
                doc.Add("DispatcherId", dispatcherId)
                doc.Add("Dispatched", dispatched)
                doc.Add("Input", input)
                doc),
            (fun bson ->
                let input =
                    System.Text.Json.JsonSerializer.Deserialize<'TInput>(bson["Input"].AsString, options)

                let id = JobId <| bson["_id"].AsGuid
                let dispatcherId = DispatcherId <| bson["DispatcherId"].AsGuid
                let dispatched = DateTimeOffset <| bson["Dispatched"].AsDateTime

                { Id = id
                  DispatcherId = dispatcherId
                  Dispatched = dispatched
                  Input = input })
        )

    let registerCompletedJobMapper<'TInput,'TOutput,'TError> () =
        BsonMapper.Global.RegisterType<CompletedJob<'TInput,'TOutput,'TError>>(
            (fun j ->
                let (JobId id) = j.Id
                let (DispatcherId dispatcherId) = j.DispatcherId
                let (WorkerId workerId) = j.WorkerId
                let dispatched = j.Dispatched.UtcDateTime
                let started = j.Started.UtcDateTime
                let completed = j.Completed.UtcDateTime
                let input = System.Text.Json.JsonSerializer.Serialize(j.Input, options)
                let output = System.Text.Json.JsonSerializer.Serialize(j.Output, options)

                let doc = new BsonDocument()
                doc.Add("_id", id)
                doc.Add("DispatcherId", dispatcherId)
                doc.Add("WorkerId", workerId)
                doc.Add("Dispatched", dispatched)
                doc.Add("Started", started)
                doc.Add("Completed", completed)
                doc.Add("Input", input)
                doc.Add("Output", output)
                doc),
            (fun bson ->
                let input =
                    System.Text.Json.JsonSerializer.Deserialize<'TInput>(bson["Input"].AsString, options)

                let output =
                    System.Text.Json.JsonSerializer.Deserialize<OutputResult<'TOutput,'TError>>(bson["Output"].AsString, options)

                let id = JobId <| bson["_id"].AsGuid
                let dispatcherId = DispatcherId <| bson["DispatcherId"].AsGuid
                let workerId = WorkerId <| bson["WorkerId"].AsGuid
                let dispatched = DateTimeOffset <| bson["Dispatched"].AsDateTime
                let started = DateTimeOffset <| bson["Started"].AsDateTime
                let completed = DateTimeOffset <| bson["Completed"].AsDateTime

                { Id = id
                  DispatcherId = dispatcherId
                  WorkerId = workerId
                  Dispatched = dispatched
                  Started = started
                  Completed = completed
                  Input = input
                  Output = output})
        )   

