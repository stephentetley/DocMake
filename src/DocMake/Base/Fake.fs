﻿namespace DocMake.Base

open Fake
open Fake.Core
open Fake.Core.Globbing.Operators

module Fake = 
    
    let private tryOneOrMore (input:'a list) : option<'a list> = 
        match input with
        | [] -> None
        | _ -> Some input

    let private tryExactlyOne (input:'a list) : option<'a> = 
        match input with
        | [x] -> Some x
        | _ -> None

    //let private tryAtMostOne (input:'a list) : option<'a> = 
    //    match input with
    //    | [] -> ???
    //    | [x] -> Some x
    //    | _ -> None

    // Zero or more matches.
    // No need for a try variant (empty list is no matches)
    // Note - pattern is a glob, not a regex.
    let findAllMatchingFiles (pattern:string) (dir:string) : string list = 
        !! (dir @@ pattern) |> Seq.toList
    
    // One or more matches. 
    // Note - pattern is a glob, not a regex.
    let tryFindSomeMatchingFiles (pattern:string) (dir:string) : option<string list> = 
        !! (dir @@ pattern) |> Seq.toList |> tryOneOrMore

    // Exactly one matches.
    // Note - pattern is a glob, not a regex.
    let tryFindExactlyOneMatchingFile (pattern:string) (dir:string) : option<string> = 
        !! (dir @@ pattern) |> Seq.toList |> tryExactlyOne
