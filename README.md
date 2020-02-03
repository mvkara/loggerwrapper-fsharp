# loggerwrapper-fsharp

A small lightweight library to wrap most .NET logging frameworks giving them a functional F# style API.
Contains handly utility functions that are useful if you are in a project that requires you to use a particular logging framework not designed for F# use.

Some features:

- Printf style logging for any logging framework. Note that logging strings aren't built/allocated if logging is turned off to save performance especially around Debug logging.
- Global logging sink as a singleton; meaning logging factories don't have to clutter your app if you prefer this style which is similar to many static logging .NET frameworks (e.g Log4Net, NLog).
- No other package dependencies required to allow logging to function (besides FSharp.Core).
- etc.

## Writing your own logging sink

### Console Logger Example ###

An example of how to wrap a console logger is below:

```
let consoleLoggingSink applicablePredicate allowRuntimeChange : LoggingFactory = fun loggerName logLevel -> 
    
    let consoleLogLevelLogger = 
        fun exnOpt message ->
            match exnOpt with
            | Some(e) -> printfn "%A %s [Error: %s]" logLevel message e.StackTrace
            | None -> printfn "%A %s" logLevel message
    
    let buildLoggerOpt() =  
        if applicablePredicate loggerName logLevel
        then Some consoleLogLevelLogger
        else None

    if allowRuntimeChange
    then Deferred buildLoggerOpt
    elif applicablePredicate loggerName logLevel
    then Applicable consoleLogLevelLogger
    else NeverApplicable
```

In this example the applicability of whether the log should trigger is decided by the applicablePredicate function. Note that the allowRuntime change variable exposes the user to a tradeoff; if logging levels don't need to change at runtime we can move the applicability check to the initialisation of the logger rather than the logger call by setting this to false. When wrapping most logging frameworks you simply invoke the underlying frameworks IsApplicable method instead for the applicability check.

### ASP.NET/Microsoft.Extensions.Logging Example ###
Another example wrapping ASP.NET's ILoggerFactory (Microsoft.Extensions.Logging) that uses the underlying framework's check:

```
open Microsoft.Extensions.Logging
open LoggerWrapper.FSharp

let createLoggerFactory (loggerFactory: ILoggerFactory) : LoggingFactory = fun loggerName loggerLevel -> 

    let logger = loggerFactory.CreateLogger(loggerName)
    
    let inline convertLoggerLevels (loggerLevel: LoggerWrapper.FSharp.LogLevel) = 
        match loggerLevel with 
        | LogLevel.Info -> Microsoft.Extensions.Logging.LogLevel.Information
        | LogLevel.Debug -> Microsoft.Extensions.Logging.LogLevel.Debug
        | LogLevel.Error -> Microsoft.Extensions.Logging.LogLevel.Error
        | LogLevel.Fatal -> Microsoft.Extensions.Logging.LogLevel.Critical
        | LogLevel.Verbose -> Microsoft.Extensions.Logging.LogLevel.Trace
        | LogLevel.Warn -> Microsoft.Extensions.Logging.LogLevel.Warning
        | LogLevel.Trace -> Microsoft.Extensions.Logging.LogLevel.Trace

    let frameworkLoggingLevel = convertLoggerLevels loggerLevel

    let loggerFunc = fun exOpt message -> 
        match exOpt with 
        | Some(ex) -> logger.Log<_>(frameworkLoggingLevel, EventId(0), message, ex, fun s _ -> s)
        | None -> logger.Log<_>(frameworkLoggingLevel, EventId(0), message, null, fun s _ -> s)
    
    if logger.IsEnabled(frameworkLoggingLevel) then Applicable loggerFunc else NeverApplicable
```

## Creating a logger

To create a logger inside your module there are a number of ways.

### Using a string name

You can either call the "LoggingFactory" func directly with the name or use the convienence wrapper inside the Logger module.

```
Logger.createLoggerByName loggingFactory "TestLogger"
```

### Using a function instance to determine logger name

Alternatively for type safety you can use either a member name or its enclosing type name (a module or class name) as the name of your logger. Example code is below.

e.g.

```
module ModuleToLog = 
  // Logger name will be "ModuleToLog"
  let rec logger = Logger.createLoggerFromMemberType staticLoggingFactory <@ logger @>

  let rec doSomethingFunc loggerFactory () = 
    let logger = Logger.createLoggerFromMemberName <@ doSomethingFunc @>
    // Rest of method code below...
    ()
```

## Logging

Logging is done via the Logger.logFormat* functions. Example is below

```
Logger.logFormat (logger LogLevel.Info) "Example typesafe log %s %A" "1" (2, 3)
```

Note: If runtime logging applicability check is disabled and performance is a concern it may be worth creating LoggerFunc's per level in advance especially around Debug or Trace logging.
This amortizes the cost of checking if the logger is applicable to only when the logging function is built. As an example:

```
let debugLoggerFunc = logFactory "LoggerName" LogLevel.Debug // Note I applied the LogLevel as well
Logger.logFormat debugLoggerFunc "Example typesafe log %s %A" "1" (2, 3)
```

## Composite logging factory/global logging

This is provided if you want your logging to log to more than one target for any particular reason. It's also useful as a way to avoid passing multiple logging factories through your program stack and/or outputting logs to more than one source.

In addition the module provides a optional static/singleton composite logging factory instance that can be used as an alternative to passing around the logger factory/loggers throughout your program. Simply initialise the logger factory you want to use by calling ```CompositeLoggingFactory.registerLogFactoryIntoGlobal``` with the logger factories you want to use before creating loggers.

An example showing both is below:

```
module SetupProgram = 
    let registerLoggingFactoryCreatedElsewhere (loggingFactory: LoggingFactory) = 
        // This registers the logging factory provided as  a sink to the globalLoggingFactory used for logger above.
        CompositeLoggingFactory.registerLogFactoryIntoGlobal loggingFactory

// Any module in the program
module ModuleToLog = 
    let rec logger = Logger.createLoggerFromMemberType CompositeLoggingFactory.globalCompositeFactory <@ logger @>

    let exampleFunction() = 
        Logger.logFormat (logger LogLevel.Info) "This will use the ModuleToLog logger with the log factories registered in the global composite factory"   
```


# Releases

Releases are made to GitHub and Nuget.
