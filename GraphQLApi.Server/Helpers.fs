namespace GraphQLApi.Server

open System
open System.Text
open Newtonsoft.Json
open Newtonsoft.Json.Linq
open Newtonsoft.Json.Serialization
open System.Collections.Generic
open FSharp.Data.GraphQL.Types

[<AutoOpen>]
module Helpers =
    let tee f x =
        f x
        x

[<AutoOpen>]
module StringHelpers =
    open System

    let utf8String (bytes : byte seq) =
        bytes
        |> Seq.filter (fun i -> i > 0uy)
        |> Array.ofSeq
        |> Encoding.UTF8.GetString

    let utf8Bytes (str : string) = str |> Encoding.UTF8.GetBytes
    let isNullOrWhiteSpace (str : string) = str |> String.IsNullOrWhiteSpace

    let isNotNullOrWhiteSpace (str : string) =
        str
        |> String.IsNullOrWhiteSpace
        |> not

[<AutoOpen>]
module JsonHelpers =
    let tryGetJsonProperty (jobj : JObject) prop =
        match jobj.Property(prop) with
        | null -> None
        | p -> Some(p.Value.ToString())

    let jsonSerializerSettings (converters : JsonConverter seq) =
        JsonSerializerSettings()
        |> tee (fun s ->
               s.Converters <- List<JsonConverter>(converters)
               s.ContractResolver <- CamelCasePropertyNamesContractResolver())

type GraphQLQuery =
    { ExecutionPlan : ExecutionPlan
      Variables : Map<string, obj> }

open System.Reflection
open Microsoft.FSharp.Reflection
open FSharp.Data.GraphQL
open FSharp.Data.GraphQL.Types.Patterns

type OptionConverter() =
    inherit JsonConverter()
    override __.CanConvert(t) =
        t.GetTypeInfo().IsGenericType
        && t.GetGenericTypeDefinition() = typedefof<option<_>>

    override __.WriteJson(writer, value, serializer) =
        let getFields value =
            let _, fields = FSharpValue.GetUnionFields(value, value.GetType())
            fields.[0]

        let value =
            match value with
            | null -> null
            | _ -> getFields value

        serializer.Serialize(writer, value)

    override __.ReadJson(_, _, _, _) = failwith "Not supported"

[<Sealed>]
type GraphQLQueryConverter<'a>(executor : Executor<'a>, replacements : Map<string, obj>) =
    inherit JsonConverter()
    override __.CanConvert(t) = t = typeof<GraphQLQuery>
    override __.WriteJson(_, _, _) = failwith "Not supported"
    override __.ReadJson(reader, _, _, serializer) =
        let jobj = JObject.Load reader
        let plan =
            jobj.Property("query").Value.ToString()
            |> executor.CreateExecutionPlan
        let varDefs = plan.Variables
        match varDefs with
        | [] ->
            upcast { ExecutionPlan = plan
                     Variables = Map.empty }
        | vs ->
            // For multipart requests, we need to replace some variables
            Map.iter
                (fun path rep ->
                jobj.SelectToken(path).Replace(JObject.FromObject(rep)))
                replacements
            let vars =
                JObject.Parse(jobj.Property("variables").Value.ToString())

            let variables =
                vs
                |> List.fold
                       (fun (acc : Map<string, obj>) (vdef : VarDef) ->
                       match vars.TryGetValue(vdef.Name) with
                       | true, jval ->
                           let v =
                               match jval.Type with
                               | JTokenType.Null -> null
                               | JTokenType.String -> jval.ToString() :> obj
                               | _ ->
                                   jval.ToObject(vdef.TypeDef.Type, serializer)
                           Map.add (vdef.Name) v acc
                       | false, _ ->
                           match vdef.DefaultValue, vdef.TypeDef with
                           | Some _, _ -> acc
                           | _, Nullable _ -> acc
                           | None, _ ->
                               failwithf
                                   "Variable %s has no default value and is missing!"
                                   vdef.Name) Map.empty
            upcast { ExecutionPlan = plan
                     Variables = variables }
