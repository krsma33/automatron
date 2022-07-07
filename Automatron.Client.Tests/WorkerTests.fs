module Automatron.Client.WorkerTests

open NUnit.Framework
open Automatron.Client
open System
open Automatron.Agents.AgentTypes

[<SetUp>]
let Setup () =
    ()

[<Test>]
let Worker_workerFunction_functionalTest () =
    
    let workerId = WorkerId <| Guid.NewGuid()
    
    let result = 
        Worker.workerFunction(workerId, "dota 2")
        |> Async.RunSynchronously
    Assert.Pass()
