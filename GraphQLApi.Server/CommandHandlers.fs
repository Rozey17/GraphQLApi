module CommandHandlers

open Domain
    open Events
    open EventStorage
    open Commands

    open System
    open System.Collections.Generic

    let Storage = new EventStorage() :> IRepository

     let AsyncHandle (msg:Command) = 
        async {
            let action = 
                let fetchitem id = new Book(id) |> Storage.GetHistoryById id
                match msg with
                | AddBook(id, name, genre) ->
            
                    let item = new Book(id, New, name, genre)
                    Storage.Save item

                | UpdateBook(id, newName) ->  

                    let itm = fetchitem id
                    itm.ChangeName newName
                    itm |> Storage.Save
                
                //| DeleteBook(id) -> 

                //    let itm = fetchitem id
                //    itm.Remove id
                //    itm |> Storage.Save
                
                
            action |> ignore
        }
        
     let Handle (msg:Command) =  AsyncHandle msg |> Async.RunSynchronously