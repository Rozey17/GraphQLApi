module Domain 
open System

type Book =
    { Id : Guid
      Name : string
      Genre : string }

type Author =
    { Id : Guid
      Name : string
      Age : int }

type AddBookInput =
    { Name : string
      Genre : string }

type AddAuthorInput =
    { Name : string
      Age : int }