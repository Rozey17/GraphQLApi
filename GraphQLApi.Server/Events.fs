module Events
open Domain
open System

type Event =
|BookAdded of Book * Guid
|BookUpdated of Book *Guid
|BookDeleted of Book * Guid


