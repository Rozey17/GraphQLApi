module Commands

open Domain
open System

type Command = 
|AddBook of Book
|UpdateBook of Book * Guid
|DeleteBook of Book * Guid
