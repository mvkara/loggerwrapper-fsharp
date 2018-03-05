# loggerwrapper-fsharp

A small lightweight library to wrap most .NET logging frameworks giving them a functional F# style API.
Contains handly utility functions that are useful if you are in a project that requires you to use a particular logging framework not designed for F# use.

Some features:

- Printf style logging for any logging framework. Note that logging strings aren't built/allocated if logging is turned off to save performance especially around Debug logging.
- Global logging sink as a singleton; meaning logging factories don't have to clutter your app if you prefer this style which is similar to many static logging .NET frameworks (e.g Log4Net, NLog).
- No other package dependencies required to allow logging to function (besides FSharp.Core).
- etc.

## Writing your own logging sink

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

In this example the applicability of whether the log should trigger is decided by the applicablePredicate function. 

Note that the allowRuntime change variable exposes the user to a tradeoff; if logging levels don't need to change at runtime we can move the applicability check to the initialisation of the logger rather than the logger call by setting this to false.

When wrapping most logging frameworks you simply invoke the underlying frameworks IsApplicable method instead for the applicability check.

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

## Composite logging factory

This is provided if you want your logging to log to more than one target for any particular reason.

In addition the module provides a optional singleton composite logging factory that can be used as an alternative to passing around the logger factory/loggers throughout your program.
Simply initialise the logger factory you want to use and register it before creating loggers. Many logging frameworks are static in their settings (e.g. log4net) so in this instance
it may ease the burden of using them in an existing C# context.

# Releases

Releases are made to GitHub and Nuget.
