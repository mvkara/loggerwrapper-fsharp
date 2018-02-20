module LoggerWrapper.FSharp.Unit.QuotationHelpersTest

open Expecto
open LoggerWrapper.FSharp

let rec nameInsideNamespacedModule = Logger.getNameFromQuotation <@ nameInsideNamespacedModule @>
let rec namespacedModuleName = Logger.getNameOfEnclosingType <@ namespacedModuleName @>

module ExampleModule = 
    let rec enclosingTypeLoggerName = Logger.getNameOfEnclosingType <@ enclosingTypeLoggerName @>
    let rec sampleLogger = Logger.getNameFromQuotation <@ sampleLogger @>

let [<Tests>] runTests = 
    testList "QuotationHelpersTest"
        [ testCase "NamespacedModule - Name from Quotation" (fun () -> Expect.equal "nameInsideNamespacedModule" nameInsideNamespacedModule "NamespacedModule - Name from Quotation")
          testCase "NamespacedModule - Name from Enclosing Type" (fun () -> Expect.equal "nameInsideNamespacedModule" nameInsideNamespacedModule "NamespacedModule - Name from Enclosing Type")
          testCase "ExampleModule - Name from Quotation" (fun () -> Expect.equal "nameInsideNamespacedModule" nameInsideNamespacedModule "ExampleModule - Name from Quotation")
          testCase "ExampleModule - Name from Enclosing Type" (fun () -> Expect.equal "nameInsideNamespacedModule" nameInsideNamespacedModule "ExampleModule - Name from Enclosing Type") ]