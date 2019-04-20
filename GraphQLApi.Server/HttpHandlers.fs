module GraphQLApi.Server.HttpHandlers

open Giraffe
open Newtonsoft.Json
open Microsoft.AspNetCore.Http
open System.IO
open FSharp.Data.GraphQL
open FSharp.Data.GraphQL.Execution
open FSharp.Control.Tasks.V2.ContextSensitive
open Newtonsoft.Json.Linq

let private okWithStr str : HttpHandler = (text str) |> Successful.ok
//let private badRequestWithStr str : HttpHandler = (text str) |> RequestErrors.badRequest
let private setCorsHeaders : HttpHandler =
    setHttpHeader "Access-Control-Allow-Origin" "*"
    >=> setHttpHeader "Access-Control-Allow-Headers" "content-type"
let private setContentTypeAsJson : HttpHandler =
    setHttpHeader "Content-Type" "application/json"

type GraphQLParameters() =
    member val Query : string = "" with get, set
    member val OperationName : string = "" with get, set
    member val Variables : JObject = null with get, set

let private jsonSettings =
    JsonSerializerSettings()
    |> tee
           (fun s ->
           s.Converters <- [| OptionConverter() :> JsonConverter |]
           s.ContractResolver <- Newtonsoft.Json.Serialization.CamelCasePropertyNamesContractResolver
                                     ())

let private json =
    function
    | Direct(data, _) -> JsonConvert.SerializeObject(data, jsonSettings)
    | Deferred(data, _, deferred) ->
        deferred
        |> Observable.add
               (fun d ->
               printfn "Deferred: %s"
                   (JsonConvert.SerializeObject(d, jsonSettings)))
        JsonConvert.SerializeObject(data, jsonSettings)
    | Stream _ -> "{}"

let private removeSpacesAndNewLines (str : string) =
    str.Trim().Replace("\r\n", " ")

let private mapStringParameter value =
    if value |> isNullOrWhiteSpace then None
    else Some(value |> removeSpacesAndNewLines)

let private mapJObjectParameter (value : JObject) =
    if value
       |> isNotNull
       && value.HasValues then Some value
    else None

let private graphQL (next : HttpFunc) (ctx : HttpContext) =
    task {
        use sr = new StreamReader(ctx.Request.Body)
        let! body = sr.ReadToEndAsync()
        let executor = GraphQLApi.Server.Schema.executor //ctx.GetService<Executor<Root>>()
        match body |> isNullOrWhiteSpace with
        | true ->
            let result =
                executor.AsyncExecute(Introspection.introspectionQuery)
                |> Async.RunSynchronously
            return! okWithStr (json result) next ctx
        | false ->
            let parameters =
                JsonConvert.DeserializeObject<GraphQLParameters>(body)
            // GraphQL HTTP only supports GET and POST methods.
            //let canProcessRequest = match request.Method with
            //                        | value when value = "GET" || value = "POST" -> true
            //                        | _ -> false
            match parameters.Query |> mapStringParameter,
                  parameters.Variables |> mapJObjectParameter with
            | Some query, Some variables ->
                let variables = variables.ToObject<Map<string, obj>>()
                printfn "Received query: %s" query
                printfn "Received variables: %A" variables
                let result =
                    executor.AsyncExecute
                        (query, variables = variables,
                         operationName = parameters.OperationName,
                         data = Schema.root) |> Async.RunSynchronously
                return! okWithStr (json result) next ctx
            | Some query, None ->
                printfn "Received query: %s" query
                let result =
                    executor.AsyncExecute
                        (query, operationName = parameters.OperationName,
                         data = Schema.root) |> Async.RunSynchronously
                return! okWithStr (json result) next ctx
            | None, _ ->
                let result =
                    executor.AsyncExecute(Introspection.introspectionQuery)
                    |> Async.RunSynchronously
                return! okWithStr (json result) next ctx
    }

// ---------------------------------
//  Web app
// ---------------------------------
let webApp : HttpHandler = setCorsHeaders >=> graphQL >=> setContentTypeAsJson
