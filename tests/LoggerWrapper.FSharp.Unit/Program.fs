module LoggerWrapper.FSharp.Unit.Program

open Expecto

[<EntryPoint>]
let main argv = Tests.runTestsInAssembly defaultConfig argv
