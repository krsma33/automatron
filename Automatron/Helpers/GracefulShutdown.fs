namespace Automatron.Helpers

open System
open System.Threading
open Automatron

module GracefulShutdown =
    let register (cts: CancellationTokenSource) =
        AppDomain.CurrentDomain.ProcessExit.Add (fun _ ->
        if not cts.Token.IsCancellationRequested then
            Console.info "Graceful stop requested..."
            cts.Cancel())

        System.Console.CancelKeyPress.Add (fun e ->
            if not cts.Token.IsCancellationRequested then
                Console.info "Graceful stop requested..."
                e.Cancel <- true
                cts.Cancel()
            else
                Console.info "Force stop requested..."
                e.Cancel <- false)

