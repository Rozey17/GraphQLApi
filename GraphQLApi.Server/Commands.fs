module Commands

open System

type Command = 
|AddBook of Guid * string * string
|UpdateBook of Guid * string
|DeleteBook of Guid
