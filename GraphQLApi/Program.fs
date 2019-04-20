open FSharp.Data.GraphQL
open FSharp.Data.GraphQL.Types
open FSharp.Data.GraphQL.Execution
open Model

type Person =
    { FirstName : string
      LastName : string }

// Define GraphQL type
let PersonType =
    Define.Object
        (name = "Person",
         fields = [ // Property resolver will be auto-generated
                    Define.AutoField("firstName", String)

                    // Asynchronous explicit member resolver
                    Define.AsyncField
                        ("lastName", String,
                         resolve = fun context person ->
                             async { return person.LastName }) ])

// Include person as a root query of a schema
let schema = Schema(query = PersonType)
// Create an Exector for the schema
let executor = Executor(schema)

// Retrieve person data
let johnSnow =
    { FirstName = "John"
      LastName = "Snow" }

let reply =
    executor.AsyncExecute
        (FSharp.Data.GraphQL.Parser.parse "{ firstName, lastName }", johnSnow)
    |> Async.RunSynchronously
