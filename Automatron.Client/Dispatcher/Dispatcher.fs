namespace Automatron.Client

open Automatron
open Automatron.Agents.AgentTypes

module Dispatcher =

    let mutable private dispatched = false

    /// Function which is supposed to dispatch items (e.g. rea'd email, parse it and return a result)
    let dispatcherFunction (id: DispatcherId) =
        async {
            Console.info $"{id} Trying to dispatch..."
            do! Async.Sleep(5000)

            match dispatched with
            | true ->
                Console.info $"{id} Nothing to dispatch"
                return None
            | false ->
                dispatched <- true
                Console.info $"{id} Dispatching..."
                return
                    Some [ "Dota2"
                           "World of Warcraft TBC Classic"
                           "Final Fantasy VII"
                           "Tomb Raider"
                           "Command & Conquer"
                           "Chrono Cross"
                           "Bauldur's Gate"
                           "Gothic 2" ]
        }
