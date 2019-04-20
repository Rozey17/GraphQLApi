namespace GraphQLApi.Server

open System
open Microsoft.AspNetCore.Builder
open Microsoft.AspNetCore.Hosting
open Microsoft.AspNetCore.Mvc
open Microsoft.Extensions.Configuration
open Microsoft.Extensions.DependencyInjection
open Giraffe
open Microsoft.Extensions.Logging
open FSharp.Data.GraphQL
open MongoDB.Driver

type Startup private () =

    new(configuration : IConfiguration) as this =
        Startup()
        then this.Configuration <- configuration

    // This method gets called by the runtime. Use this method to add services to the container.
    member this.ConfigureServices(services : IServiceCollection) =
        // Add framework services.
        let connectionString = "mongodb://localhost"
        let mongo = MongoClient(connectionString)
        let db = mongo.GetDatabase "LibraryDB"
        services.AddGiraffe() |> ignore
        //services.AddSingleton<Executor<Root>>(Schema.executor) |> ignore
        services.AddMvc()
                .SetCompatibilityVersion(CompatibilityVersion.Version_2_1)
        |> ignore

    // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
    member this.Configure(app : IApplicationBuilder, env : IHostingEnvironment) =
        if (env.IsDevelopment()) then app.UseDeveloperExceptionPage() |> ignore
        else app.UseHsts() |> ignore
        let errorHandler (ex : Exception) (log : ILogger) =
            log.LogError
                (EventId(), ex,
                 "An unhandled exception has occurred while executing the request.")
            clearResponse >=> setStatusCode 500
        app.UseGiraffeErrorHandler(errorHandler).UseGiraffe HttpHandlers.webApp

    member val Configuration : IConfiguration = null with get, set
