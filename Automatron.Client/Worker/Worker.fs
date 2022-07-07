namespace Automatron.Client

open System
open Automatron
open Microsoft.Playwright
open Automatron.Web.MsPlaywright
open Automatron.Agents.AgentTypes
open System.Threading.Tasks

module Worker =

    type Error =
        | BusinessError of string
        | RuntimeError of string

    let browseGoogle (id: WorkerId) (input: string) (browserContext: IBrowserContext) =
        task {

            let! page = browserContext.NewPageAsync()
            let! _ = page.GotoAsync("https://www.google.com/")

            do! page.TypeAsync("[name='q']", input)

            do! page.ClickAsync("[name='btnK']")

            let! _ = page.WaitForNavigationAsync()

            let! elements = page.QuerySelectorAllAsync(".yuRUbf > a")

            let scrapeOne (handle: IElementHandle) =
                task {
                    let! link = handle.GetAttributeAsync("href")
                    let! titleElement = handle.QuerySelectorAsync("h3")
                    let! title = titleElement.InnerTextAsync()
                    return title, link
                }

            let! results =
                elements
                |> Seq.map (fun e -> scrapeOne e)
                |> Task.WhenAll

            let results = Array.distinct results

            let rnd = new Random()

            let n = rnd.Next(100)

            if n % 7 = 0 then
                Console.error $"{id} Some random error"
                return Error(BusinessError "Some random error")
            elif n % 13 = 0 then
                Console.error $"{id} Hoho Haha forced"
                raise (new ArgumentNullException("Some Param", "Hoho Haha forced"))
                return Error(RuntimeError "Hoho Haha forced")
            else
                Console.info $"{id} Success: {input} - results number: {results.Length}"
                return Ok(results)
        }

    let workerFunction (id: WorkerId, input: string) =
        task {
            return!
                getBrowser Chromium
                |> getBrowserContext
                |> useBrowserContext (browseGoogle id input)
        }
        |> Async.AwaitTask
