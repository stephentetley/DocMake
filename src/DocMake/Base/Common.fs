﻿namespace DocMake.Base

open System.IO
open System.Text
open Fake.Core.Globbing.Operators

module Common = 
    
    type PrintQuality = PqScreen | PqPrint

    let ghostscriptPrintQuality (quality:PrintQuality) : string = 
        match quality with
        | PqScreen -> @"/screen"
        | PqPrint -> @"/preprint"


    let doubleQuote (s:string) : string = "\"" + s + "\""

    let safeName (input:string) : string = 
        let bads = ['\\'; '/'; ':']
        List.fold (fun s c -> s.Replace(c,'_')) input bads

    let zeroPad (width:int) (value:int) = 
        let ss = value.ToString ()
        let diff = width - ss.Length
        String.replicate diff "0" + ss

    let maybeCreateDirectory (dirpath:string) : unit = 
        if not <| Directory.Exists(dirpath) then 
            ignore <| Directory.CreateDirectory(dirpath)
        else ()

    let unique (xs:seq<'a>) : 'a = 
        let next zs = match zs with
                      | [] -> failwith "unique - no matches."
                      | [z] -> z
                      | _ -> failwithf "unique - %i matches" zs.Length

        Seq.toList xs |> next
