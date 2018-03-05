namespace LoggerWrapper.FSharp

open FSharp.Core.Printf
open Microsoft.FSharp.Quotations.Patterns

type LoggerName = string

type LoggerFunc = exn option -> string -> unit

/// The logger for a given LoggerName and LogLevel.
type [<Struct>] Logger = 
    | Applicable of applicableLogger: LoggerFunc
    | Deferred of potentiallyApplicable: (unit -> LoggerFunc option)
    | NeverApplicable

/// LogLevels available to the LoggerWrapper.
type [<RequireQualifiedAccess>] LogLevel = 
    | Info
    | Debug
    | Error
    | Fatal
    | Warn
    | Trace
    | Verbose

/// A logger created from the logger factory with an assigned logger name.
type NamedLogger = LogLevel -> Logger

/// The factory function used to created named loggers.
type LoggingFactory = LoggerName -> NamedLogger

/// Contains functions for working with logging factories and any loggers built from them.
module Logger = 

    let logFormatRaw (logger: Logger)  =
        let logFormatFunc loggingFunc = (fun exnOpt -> kprintf (fun s -> loggingFunc exnOpt s))
        match logger with
        | Applicable(loggingFunc) -> logFormatFunc loggingFunc
        | Deferred(potentiallyApplicable) ->
            (fun exnOpt ->
                match potentiallyApplicable() with
                | Some(loggingFunc) -> logFormatFunc loggingFunc exnOpt
                | None -> DoNothingFunctionBuilder.ignoreFunc)
        | NeverApplicable -> DoNothingFunctionBuilder.ignoreFunc

    let logFormat (logger: Logger) f = logFormatRaw logger None f

    let logFormatError (logger: Logger) ex f = logFormatRaw logger (Some ex) f

    /// Simply a wrapper around the loggingFactory function passed in for discovery of the API.
    let createLogger (loggingFactory: LoggingFactory) loggerName = loggingFactory loggerName

    /// Attempts to get the name of the member/value/function referred to in the quotation.
    let rec internal getNameFromQuotation q = 
        match q with 
        | PropertyGet(_, name, _) -> name.Name
        | Lambda (_, Lambda (_, Call (_, name, _))) -> name.Name
        | FieldGet (Some (ValueWithName(_)), name) -> name.Name
        | ValueWithName (_, _, name) -> name
        | Let (_, _, exp2) -> getNameFromQuotation exp2
        | _ -> failwithf "Quotation not supported [%A]" q

    /// Gets the enclosing type of the member/value/function referred to in the quotation.
    let rec internal getNameOfEnclosingType q = 
        match q with 
        | PropertyGet(_, name, _) -> name.DeclaringType.Name
        | Lambda (_, Lambda (_, Call (_, name, _))) -> name.DeclaringType.Name
        | FieldGet (Some (ValueWithName (_, fieldType, _)), _) -> fieldType.Name
        | ValueWithName (_, _, name) -> name
        | Let (_, _, exp2) -> getNameOfEnclosingType exp2
        | _ -> failwithf "Quotation not supported [%A]" q

    /// Creates a logger with the logger name set to the name of the value quoted.
    /// e.g <@ Module.ModuleLogger @> will create a logger with the name of "ModuleLogger".
    /// Example usage: let rec exampleLogger = Logger.createLoggerFromMemberName <@ exampleLogger @> 
    let createLoggerFromMemberName (loggingFactory: LoggingFactory) memberQuotation = loggingFactory (getNameFromQuotation memberQuotation)

    /// Creates a logger with the logger name set to the type enclosing the value quoted.
    /// e.g <@ Module.ModuleLogger @> will create a logger with the name of "Module".
    /// Example usage: let rec logger = Logger.createLoggerFromMemberType <@ logger @> 
    let createLoggerFromMemberType (loggingFactory: LoggingFactory) memberQuotation = loggingFactory (getNameOfEnclosingType memberQuotation)