module LoggerWrapper.FSharp.CompositeLoggingFactory

/// Builds a composite logging factory which when used for logging logs to all factories provided.
let compositeLoggingFactory loggingFactories : LoggingFactory = fun loggerName logLevel ->
    let (listOfAlwaysApplicableLoggers, listOfPotentallyApplicableLoggers) = 
        loggingFactories
        |> List.fold 
            (fun (applicableLoggers, deferredLoggers) loggingFactory -> 
                (match loggingFactory loggerName logLevel with 
                 | NeverApplicable -> (applicableLoggers, deferredLoggers)
                 | Deferred(getLoggerFunc) -> (applicableLoggers, getLoggerFunc :: deferredLoggers)
                 | Applicable(loggerFunc) -> (loggerFunc :: applicableLoggers, deferredLoggers)))
            ([], [])   

    let inline applyLoggers loggerList exOpt message = for logLevelLoggerFunc in loggerList do logLevelLoggerFunc exOpt message

    match (listOfAlwaysApplicableLoggers, listOfPotentallyApplicableLoggers) with
    | ([], []) -> NeverApplicable
    | (_, []) -> Applicable (applyLoggers listOfAlwaysApplicableLoggers)
    | (_, _) -> 
        let logger = 
            fun () -> 
                let currentlyAppliedLoggers = listOfPotentallyApplicableLoggers |> List.collect (fun logApplicableFunc -> logApplicableFunc() |> Option.toList)
                if List.isEmpty currentlyAppliedLoggers && List.isEmpty listOfAlwaysApplicableLoggers 
                then None
                else Some (fun exOpt message -> 
                    applyLoggers currentlyAppliedLoggers exOpt message 
                    applyLoggers listOfAlwaysApplicableLoggers exOpt message )
        Deferred logger

let mutable private globalSinks = []

// Gets a singleton composite logging factory. Note: When creating a logger only logger factories registered at the time will be considered.
let globalCompositeFactory = compositeLoggingFactory globalSinks

// Registers a log factory into the singleton composite logging factory provided in this module.
let registerLogFactoryIntoGlobal (loggingFactory: LoggingFactory) = globalSinks <- loggingFactory :: globalSinks