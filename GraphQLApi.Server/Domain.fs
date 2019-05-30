module Domain 
open System
open Events

type CreateType = LoadHistory | New

[<AbstractClass>]
type AggregateRoot(id) =
 let mutable changes = []
 member x.Id=id
 member x.GetUncommittedChanges = changes
 member x.MarkChangesAsCommitted = changes <- []
 abstract member Apply : Event -> unit
 member x.ApplyChange (evt:Event) =
  x.Apply evt 
  changes <- evt :: changes

let LoadsFromHistory<'t when 't :> AggregateRoot> (history:Event list) (item:'t) = 
        history |> List.map item.Apply |> ignore
        item

type Book(id:Guid, creation:CreateType, name:string, genre:string) = 
 inherit AggregateRoot(id)

 let constructorEvent = 
   if(creation = CreateType.New) then base.ApplyChange(BookAdded(id, name))
 do constructorEvent |> ignore
 
 internal new(id:Guid) = Book(id, CreateType.LoadHistory, String.Empty, String.Empty)

 member x.Id = id
 member x.Genre = genre
 member val Activated = true with get, set

 override x.Apply (evt:Event) =
            match evt with
            | BookAdded(i,n) ->  x.Activated <- true
            | _ -> ignore()

 member x.ChangeName (name:string) = 
            if String.IsNullOrEmpty(name) then failwith("must give a name")
            BookUpdated(x.Id, name) |> x.ApplyChange

 //member x.Remove count = 
 //           if count <= 0 then failwith("cant remove negative count from inventory")
 //           BookDeleted(x.Id) |> x.ApplyChange

type Author =
    { Id : Guid
      Name : string
      Age : int }

