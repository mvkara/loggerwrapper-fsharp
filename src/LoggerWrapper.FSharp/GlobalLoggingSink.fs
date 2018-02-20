module LoggerWrapper.FSharp.GlobalSink

open Logger

let mutable private globalSinks = []

let registerLogFactory (loggingFactory: LoggingFactory) = globalSinks <- loggingFactory :: globalSinks

// Gets a global sink logging factory. Note that the only logging sinks/factories considered 
// are the sources registered when the logging factory is used to create a logger.
let globalSinkLoggingFactory : LoggingFactory = fun loggerName logLevel ->
    let (listOfAlwaysApplicableLoggers, listOfPotentallyApplicableLoggers) = 
        globalSinks
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