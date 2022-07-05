namespace Automatron.Repository.LiteDB

open Automatron.PersistorOptionsBuilder
open Mappers
open Repository

module LiteDbPersistor =
    
    let initDefaultOptions<'TInput,'TOutput,'TError>() =
        registerJobMapper<'TInput>()
        registerCompletedJobMapper<'TInput,'TOutput,'TError>()
        createRepositoryFolder()

        persistorOptionsBuilder
        |> configureRetrieveCompletedJobs retrieveJobResult
        |> configurePersistJobResult persistJobResult<'TInput,'TOutput,'TError>
        |> configureRetrieveUnprocessedJobs retrieveUnprocessedJobs<'TInput>
        |> configurePersistUnprocessedJobs persistUnprocessedJobs<'TInput>
        |> buildPersistorOptions