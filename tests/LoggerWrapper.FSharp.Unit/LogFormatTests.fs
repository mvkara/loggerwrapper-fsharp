module LoggerWrapper.FSharp.Unit.LogFormatTests

open Expecto
open LoggerWrapper.FSharp

let stubLoggerName = "TestLogger"
let stubLoggerLevel = LogLevel.Info

// TODO: What's the point of not always checking if the logLevel reastically will be checked
// every time? (i.e logFormat always takes in a logLevel anyway?)
// Maybe we have over-engineered this thing?

type Log = {
    LogLevel: LogLevel
    Message: string
    Exception: exn option
}

let buildTestLoggerFactory (logLineList: ResizeArray<Log>) applicablePredicate allowRuntimeChange : LoggingFactory = fun loggerName logLevel -> 
        
    let consoleLogLevelLogger : LogLevelLoggerFunc = 
        fun exnOpt message -> logLineList.Add({ LogLevel = logLevel; Message = message; Exception = exnOpt })
    
    let buildLoggerOpt() =
        if applicablePredicate loggerName logLevel
        then Some consoleLogLevelLogger
        else None

    if allowRuntimeChange
    then PotentiallyApplicable buildLoggerOpt
    elif applicablePredicate loggerName logLevel
    then Applicable consoleLogLevelLogger
    else NeverApplicable

let [<GeneralizableValue>] loggingFunc testLoggerFactory = Logger.logFormatRaw (testLoggerFactory stubLoggerName stubLoggerLevel) 

let runTestWithDifferentLogLevelsNoRuntimeChange isApplicable logsToTry expectedLogs = 
    let logSink = ResizeArray<_>()
    let testLoggerFactory = buildTestLoggerFactory logSink isApplicable false

    let logger = testLoggerFactory stubLoggerName 

    for log in logsToTry do 
        let loggerFunc = logger log.LogLevel
        Logger.logFormatRaw loggerFunc log.Exception "%s" log.Message
    
    Expect.sequenceEqual logSink expectedLogs "Log lines aren't equal"

let runDynamic isApplicable allowRuntimeChange logsToTry expectedLogs = 
    let logSink = ResizeArray<_>()
    let testLoggerFactory = buildTestLoggerFactory logSink isApplicable allowRuntimeChange

    let logger = testLoggerFactory stubLoggerName stubLoggerLevel

    for log in logsToTry do 
        Logger.logFormatRaw logger log.Exception "%s" log.Message
    
    Expect.sequenceEqual logSink expectedLogs "Log lines aren't equal"

let ``Log is applicable for Info, log only Info`` = 
    testCase "Log is applicable for Info, log only Info" (fun () -> 
        runTestWithDifferentLogLevelsNoRuntimeChange
            (fun _ level -> level = LogLevel.Info)
            [
                { LogLevel = LogLevel.Info; Message = "1"; Exception = None }
                { LogLevel = LogLevel.Info; Message = "2"; Exception = None }
                { LogLevel = LogLevel.Warn; Message = "3"; Exception = None }
            ]
            [
                { LogLevel = LogLevel.Info; Message = "1"; Exception = None }
                { LogLevel = LogLevel.Info; Message = "2"; Exception = None }
            ])

let testLogConfigChangeAtRuntimeSetting isLogConfigAllowedToChange = 
    testCase (sprintf "Log is allowed to change [%b], log only info then log only error" isLogConfigAllowedToChange) (fun () -> 

        let logs = [
            { LogLevel = stubLoggerLevel; Message = "Message"; Exception = None }
            { LogLevel = stubLoggerLevel; Message = "Message2"; Exception = None }
            { LogLevel = stubLoggerLevel; Message = "Message3"; Exception = None }
            { LogLevel = stubLoggerLevel; Message = "Message4"; Exception = None }
        ]

        // Logging function that over time changes state.
        let mutable count = 0L
        let isApplicableFunc = fun _ _ ->
            let currentCount = count
            count <- count + 1L
            currentCount < 2L
        
        runDynamic
            isApplicableFunc
            isLogConfigAllowedToChange
            logs
            (if isLogConfigAllowedToChange then logs.[ 0 ..  1] else logs))

let [<Tests>] testCases = 
    testList
        "LogFormatTests"
        [ ``Log is applicable for Info, log only Info``
          testLogConfigChangeAtRuntimeSetting true
          testLogConfigChangeAtRuntimeSetting false ]