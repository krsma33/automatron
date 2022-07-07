namespace Automatron.Web

open Microsoft.Playwright
open System.Threading.Tasks

module MsPlaywright =

    type Browser =
        | Chromium
        | Chrome
        | Edge
        | Firefox
        | Webkit

    let mutable private playwright = None
    let mutable private chromiumBrowser = None
    let mutable private chromeBrowser = None
    let mutable private edgeBrowser = None
    let mutable private firefoxBrowser = None
    let mutable private webkitBrowser = None

    let private getPlaywright () =
        task {
            match playwright with
            | None ->
                let! plwr = Playwright.CreateAsync()
                playwright <- Some plwr
                return plwr
            | Some p -> return p
        }

    let private initBrowser (kind: Browser) =
        task {
            let! playwright = getPlaywright ()

            let opts = BrowserTypeLaunchOptions()
            opts.Headless <- false

            return!
                match kind with
                | Chromium -> playwright.Chromium.LaunchAsync(opts)
                | Chrome ->
                    opts.Channel <- "chrome"
                    playwright.Chromium.LaunchAsync(opts)
                | Edge ->
                    opts.Channel <- "msedge"
                    playwright.Chromium.LaunchAsync(opts)
                | Firefox -> playwright.Firefox.LaunchAsync(opts)
                | Webkit -> playwright.Webkit.LaunchAsync(opts)
        }

    let getBrowser (kind: Browser) =
        task {
            match kind with
            | Chromium ->
                match chromiumBrowser with
                | None ->
                    let! brws = initBrowser kind
                    chromiumBrowser <- Some brws
                    return brws
                | Some b -> return b
            | Chrome ->
                match chromeBrowser with
                | None ->
                    let! brws = initBrowser kind
                    chromeBrowser <- Some brws
                    return brws
                | Some b -> return b
            | Edge ->
                match edgeBrowser with
                | None ->
                    let! brws = initBrowser kind
                    edgeBrowser <- Some brws
                    return brws
                | Some b -> return b
            | Firefox ->
                match firefoxBrowser with
                | None ->
                    let! brws = initBrowser kind
                    firefoxBrowser <- Some brws
                    return brws
                | Some b -> return b
            | Webkit ->
                match webkitBrowser with
                | None ->
                    let! brws = initBrowser kind
                    webkitBrowser <- Some brws
                    return brws
                | Some b -> return b
        }

    let getBrowserContext (getBrowser: Task<IBrowser>) =
        task {
            let! browser = getBrowser
            return! browser.NewContextAsync()
        }

    let useBrowserContext (func: IBrowserContext -> Task<_>) (getBrowserContext: Task<IBrowserContext>) =
        task {
            use! browserContext = getBrowserContext
            return! func browserContext
        }

    let getPage (getBrowserContext: Task<IBrowserContext>) =
        task {
            let! browserContext = getBrowserContext
            return! browserContext.NewPageAsync()
        }

    let cleanup () =
        task {
            match chromiumBrowser with
            | None -> ()
            | Some b -> do! b.DisposeAsync()

            match chromeBrowser with
            | None -> ()
            | Some b -> do! b.DisposeAsync()

            match edgeBrowser with
            | None -> ()
            | Some b -> do! b.DisposeAsync()

            match firefoxBrowser with
            | None -> ()
            | Some b -> do! b.DisposeAsync()

            match webkitBrowser with
            | None -> ()
            | Some b -> do! b.DisposeAsync()

            match playwright with
            | None -> ()
            | Some b -> b.Dispose()
        }
