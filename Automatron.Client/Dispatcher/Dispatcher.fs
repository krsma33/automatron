namespace Automatron.Client

open Automatron
open Automatron.Agents.AgentTypes

module Dispatcher =
    
    // if using only 1 dispatcher this is valid code for dispatching only once, for multiple dispatchers other solution may be required e.g SemaphoreSlim
    let mutable private dispatched = false

    /// Function which is supposed to dispatch items (e.g. read email, parse it and return a result)
    let dispatcherFunction (id: DispatcherId) =
        async {
            Console.info $"{id} Trying to dispatch..."

            // Faking long running operation
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
