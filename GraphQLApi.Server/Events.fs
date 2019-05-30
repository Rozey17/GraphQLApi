module Events
open System

type Event =
|BookAdded of Guid * string
|BookUpdated of Guid * string
|BookDeleted of Guid
with 
  override x.ToString() =
   match x with
   |BookAdded(i,n) -> "New Book " + n + "added (id : "+ i.ToString() + ")"
   |BookUpdated(i,n) -> "Book updated with new name" + n + "(Id: " + i.ToString() +")"
   |BookDeleted(i) -> "Book deleted (id: " + i.ToString() + ")"
   


