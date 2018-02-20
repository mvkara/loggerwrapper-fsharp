module LoggerWrapper.FSharp.DoNothingFunctionBuilder

open Microsoft.FSharp.Reflection
open System
open System.Collections.Concurrent

let private doNothingCache = ConcurrentDictionary<Type, obj>()

type BuildNothing =
    static member BuildNothingFunc<'tin>() = (fun (_: 'tin) -> ())
    static member BuildNothingFuncChained<'tdomain, 'trange>(range: 'trange) : ('tdomain -> 'trange) = (fun _ -> range)

let rec private buildDoNothingFunctionRec (t: Type) =
   let (domain, range) = FSharpType.GetFunctionElements(t)
   if range |> FSharpType.IsFunction
   then 
       let rangeFunction = buildDoNothingFunctionRec range
       let genericMethod = typeof<BuildNothing>.GetMethod("BuildNothingFuncChained").MakeGenericMethod([|domain; range|])
       genericMethod.Invoke(null, [| rangeFunction |])
    else 
       let genericMethod = typeof<BuildNothing>.GetMethod("BuildNothingFunc")
       let actualMethod = genericMethod.MakeGenericMethod([| domain |])
       actualMethod.Invoke(null, [||])       

type private BackingField<'tFunc> = 
    static member IgnoreFunc = buildDoNothingFunctionRec typeof<'tFunc> :?> 'tFunc


/// For any type signature that ends in unit returns a curried ignore function.
/// Similar to ignore but works for multiple arguments.
let [<GeneralizableValue>] ignoreFunc<'tin, 'tout> : 'tin -> 'tout =
    BackingField<'tin -> 'tout>.IgnoreFunc
    