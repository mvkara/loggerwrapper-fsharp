module LoggerWrapper.FSharp.CompositeLoggingFactory

/// Builds a composite logging factory which when used for logging logs to all factories provided.
let compositeLoggingFactory loggingFactories : LoggingFactory = fun loggerName logLevel ->
    let (listOfAlwaysApplicableLoggers, listOfPotentallyApplicableLoggers) = 
        loggingFactories
        |> List.fold 
            (fun (aal, pal) loggingFactory -> 
                (match loggingFactory loggerName logLevel with 
                 | NeverApplicable -> (aal, pal)
                 | PotentiallyApplicable(getLoggerFunc) -> (aal, getLoggerFunc :: pal)
                 | Applicable(loggerFunc) -> (loggerFunc :: aal, pal)))
            ([], [])   

    if List.isEmpty listOfAlwaysApplicableLoggers && List.isEmpty listOfPotentallyApplicableLoggers
    then Logger.NeverApplicable
    elif List.isEmpty listOfPotentallyApplicableLoggers
    then Applicable (fun exnOpt message -> for l in listOfAlwaysApplicableLoggers do l exnOpt message)
    else 
        (fun () -> 
            fun exOpt message -> 
                let currentlyAppliedLoggers = 
                    listOfPotentallyApplicableLoggers |> List.collect (fun logApplicableFunc -> logApplicableFunc() |> Option.toList)

                let loggers = listOfAlwaysApplicableLoggers |> Seq.append currentlyAppliedLoggers

                for logLevelLoggerFunc in loggers do logLevelLoggerFunc exOpt message
            |> Some)
        |> PotentiallyApplicable

let mutable private globalSinks = []

// Gets a singleton composite logging factory. Note: When creating a logger only logger factories registered at the time will be considered.
let globalCompositeFactory = compositeLoggingFactory globalSinks

// Registers a log factory into the singleton composite logging factory provided in this module.
let registerLogFactoryIntoGlobal (loggingFactory: LoggingFactory) = globalSinks <- loggingFactory :: globalSinks