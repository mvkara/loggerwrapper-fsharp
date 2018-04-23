module LoggerWrapper.FSharp.Unit.QuotationHelpersTest

open Expecto
open LoggerWrapper.FSharp

let rec nameInsideNamespacedModule = Logger.getNameFromQuotation <@ nameInsideNamespacedModule @>
let rec namespacedModuleName = Logger.getNameOfEnclosingType <@ namespacedModuleName @>

module ExampleModule = 
    let rec enclosingTypeLoggerName = Logger.getNameOfEnclosingType <@ enclosingTypeLoggerName @>
    let rec sampleLogger = Logger.getNameFromQuotation <@ sampleLogger @>

    let sampleFunction() = 
        let s x y = x
        let rec name  = s "" (<@ name @>) // Logger.getNameOfEnclosingType <@ name @>
        name

let [<Tests>] runTests = 
    testList "QuotationHelpersTest"
        [ testCase "NamespacedModule - Name from Quotation" (fun () -> Expect.equal "nameInsideNamespacedModule" nameInsideNamespacedModule "NamespacedModule - Name from Quotation")
          testCase "NamespacedModule - Name from Enclosing Type" (fun () -> Expect.equal "nameInsideNamespacedModule" nameInsideNamespacedModule "NamespacedModule - Name from Enclosing Type")
          testCase "ExampleModule - Name from Quotation" (fun () -> Expect.equal "sampleLogger" ExampleModule.sampleLogger "ExampleModule - Name from Quotation")
          testCase "ExampleModule - Name from Enclosing Type" (fun () -> Expect.equal "ExampleModule" ExampleModule.enclosingTypeLoggerName "ExampleModule - Name from Enclosing Type")
          testCase "ExampleModule - Name from enclosing type referring to own variable" (fun () -> Expect.equal (ExampleModule.sampleFunction()) "" "Not equal" )]