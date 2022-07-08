# Automatron #

Automatron is a **oppinionated** framework for concurrent transactional processing using producer/consumer model. 
Concurrency is facilitated using actors (mailbox processing agents).  
One of the forseen use cases is RPA web automation using Microsoft.Playwright library, and is covered in a dummy Client project.  

Framework is supposed to support resilient processing by automating persisting of unprocessed items, and loading them on next startup.

## Projects/packages ##

Solution consists of multiple projects
* Automatron - Main framework library
* Automatron.Repository.LiteDB - Out of the box local db persistance using LiteDB.
* Automatron.Web - Helper funcitions for using Microsoft.Playwright correctly
* Automatron.Client - Dummy project example. To be converted to template in future
* Automatron.Client.Tests - Test project containing 2 dummy tests for 2 main functions

## Planned Features ##

Future features in no particular order (in pipeline):
* Automatic reporting
* Retry of failed items
* Work mode Configuration options (currently supports only infinite loop with explicit graceful shutdown)
* General configuration options
* Logging (need to investigate how to do scoped logging in F#, so that we can decorate logs so that we know which worker produced which log)
* Template for use in regular console apps
* Template for use in WebAPI (dispatching using REST endpoint)