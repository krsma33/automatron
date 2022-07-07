module Automatron.Client.DispatcherTests

open NUnit.Framework
open Automatron.Client
open System
open Automatron.Agents.AgentTypes

[<SetUp>]
let Setup () =
    ()

[<Test>]
let Dispatcher_dispatcherFunction_functionalTest () =
    
    let dispatcherId = DispatcherId <| Guid.NewGuid()
    
    let result = 
        Dispatcher.dispatcherFunction(dispatcherId)
        |> Async.RunSynchronously

    Assert.AreEqual(true, result.IsSome)
