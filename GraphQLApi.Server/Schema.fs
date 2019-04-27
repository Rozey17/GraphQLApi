namespace GraphQLApi.Server

open System
open FSharp.Data.GraphQL
open FSharp.Data.GraphQL.Types

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

type Root =
    { ClientId : System.Guid }

module Db =
 open System.Collections.Generic

 let private bookStorage = new Dictionary<int, Book>()
 let saveBook () =
  bookStorage.Values :> seq<Book>

module Schema =
      
    let mutable books =
        [ { Id = System.Guid.NewGuid()
            Name = "The Richest Man In Babylon"
            Genre = "Philosophy" }
          { Id = System.Guid.NewGuid()
            Name = "As A Man Thinketh"
            Genre = "Philosophy" } ]

    let mutable authors =
        [ { Id = System.Guid.NewGuid()
            Name = "Ferrad"
            Age = 28 }
          { Id = System.Guid.NewGuid()
            Name = "Mael"
            Age = 25 } ]

    let getBook id = books |> List.tryFind (fun book -> book.Id = id)
    let getAuthor id = authors |> List.tryFind (fun author -> author.Id = id)

    let AddBookInputType =
        Define.InputObject<AddBookInput>("AddBookInput",
                                         [ Define.Input("name", String)
                                           Define.Input("genre", String) ])
    let AddAuthorInputType =
        Define.InputObject<AddBookInput>("AddAuthorInput",
                                         [ Define.Input("name", String)
                                           Define.Input("age", Int) ])
    let BookType =
        Define.Object<Book>
            (name = "Book", description = "A particular book",
             isTypeOf = (fun x -> x :? Book),
             fieldsFn = fun () ->
                 [ Define.Field
                       ("id", Guid, "The ID of the book.",
                        fun _ (x : Book) -> x.Id)

                   Define.Field
                       ("name", String, "The name of the book.",
                        fun _ (x : Book) -> x.Name)

                   Define.Field
                       ("genre", String, "The genre of the book.",
                        fun _ (x : Book) -> x.Genre) ])

    let AuthorType =
        Define.Object<Author>
            (name = "Author", description = "A particular author",
             isTypeOf = (fun x -> x :? Author),
             fieldsFn = fun () ->
                 [ Define.Field
                       ("id", Guid, "The ID of the author.",
                        fun _ (x : Author) -> x.Id)

                   Define.Field
                       ("name", String, "The name of the author.",
                        fun _ (x : Author) -> x.Name)

                   Define.Field
                       ("age", Int, "The age of the author.",
                        fun _ (x : Author) -> x.Age) ]) // les types sont toujours en majuscules

    let RootType =
        Define.Object<Root>
            (name = "Root",
             description = "The Root type to be passed to all our resolvers",
             isTypeOf = (fun o -> o :? Root),
             fieldsFn = fun () ->
                 [ Define.Field
                       ("clientid", Guid, "The ID of the client",
                        fun _ r -> r.ClientId) ])

    let QueryType =
        Define.Object<Root>
            (name = "Query",
             fields = [ Define.Field
                            ("book", Nullable BookType, "Gets a particular book",
                             [ Define.Input("id", Guid) ],
                             fun ctx _ -> getBook (ctx.Arg("id")))

                        Define.Field
                            ("author", Nullable AuthorType, "Gets an author",
                             [ Define.Input("id", Guid) ],
                             fun ctx _ -> getAuthor (ctx.Arg("id")))

                        Define.Field("books", ListOf BookType, "Gets all the books", fun _ _ -> books)])

    let MutationType =
        Define.Object<Root>(name = "Mutation",
                            fields = [ Define.AsyncField("addNewBook", ID,
                                                         "Add a new book",
                                                         [ Define.Input
                                                               ("input",
                                                                AddBookInputType) ],
                                                         fun ctx _ ->
                                                             async {
                                                                 let input =
                                                                     ctx.Arg<AddBookInput>
                                                                         ("input")
                                                                 let id =
                                                                     System.Guid.NewGuid
                                                                         ()
                                                                 match books
                                                                       |> List.exists
                                                                              (fun book ->
                                                                              book.Name.ToLower
                                                                                  () = input.Name.ToLower
                                                                                           ()) with
                                                                 | false ->
                                                                     books <- { Id = id
                                                                                Name = input.Name
                                                                                Genre = input.Genre } :: books
                                                                 | true ->
                                                                     
                                                                     failwith
                                                                         (sprintf
                                                                              "A book with name %s already exists"
                                                                              input.Name)
                                                                 return id
                                                             })                                                        
                                                        ])
                                                          
    let executor =
        Schema(query = QueryType, mutation = MutationType) :> ISchema<Root>
        |> Executor
    let root = { ClientId = System.Guid.NewGuid() }
