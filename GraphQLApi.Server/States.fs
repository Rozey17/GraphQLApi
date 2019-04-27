module States
open Domain
open System

type State = 
|CreatedBook of Book
|DeletedBook of Book * Guid
|UpdatedBook of Book * Guid

let apply state event =
 match state, event with
 |
 |