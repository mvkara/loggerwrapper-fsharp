# loggerwrapper-fsharp

A small lightweight library to wrap most .NET logging frameworks giving them a functional F# style API.
Contains handly utility functions that are useful if you are in a project that requires you to use a particular logging framework not designed for F# use.

Some features:

- Printf style logging to any logging framework. Note that logging strings aren't built/allocated if logging is turned off to save performance especially around Debug logging.
- Global logging sink as a singleton; meaning logging factories don't have to clutter your app if you prefer this style which is similar to many .NET logging frameworks (e.g Log4Net, NLog).
- etc.

## Writing your own logging sink

An example of how to wrap a console logger is below:

```
let consoleLoggingSink applicablePredicate allowRuntimeChange : LoggingFactory = fun loggerName logLevel -> 
    
    let consoleLogLevelLogger : LogLevelLogger = 
        fun exnOpt message ->
            match exnOpt with
            | Some(e) -> printfn "%A %s [Error: %s]" logLevel message e.StackTrace
            | None -> printfn "%A %s" logLevel message
    
    let buildLoggerOpt() =  
        if applicablePredicate loggerName logLevel
        then Some consoleLogLevelLogger
        else None

    if allowRuntimeChange
    then PotentiallyApplicable buildLoggerOpt
    elif applicablePredicate loggerName logLevel
    then Applicable consoleLogLevelLogger
    else NeverApplicable
```

In this example the applicability of whether the log should trigger is decided by the applicablePredicate function. Note that the allowRuntime change variable exposes the user to a tradeoff - if logging levels don't need to change at runtime we can move the applicability check to the initialisation of the logger rather than the logger call by setting this to false.

When wrapping most logging frameworks you simply invoke the underlying frameworks IsApplicable method instead.

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
```

# Logging

Logging is done via the Logger.logFormat* functions. Example is below

```
Logger.logFormat (logFactory LogLevel.Info) "Example typesafe log %s %A" "1" (2, 3)
```

Note: If runtime logging applicability check is disabled it may be worth creating loggers per level separately if performance is concern
especially around Debug or Trace logging. As an example:

```
let logger = logFactory "LoggerName" LogLevel.Info
Logger.logFormat logger "Example typesafe log %s %A" "1" (2, 3)
```

# Releases

Releases are made to GitHub and Nuget.
