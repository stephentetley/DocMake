﻿// Copyright (c) Stephen Tetley 2018
// License: BSD 3 Clause

[<RequireQualifiedAccess>]
module DocMake.Tasks.MdToDoc



open DocMake.Base.FakeLike
open DocMake.Builder.BuildMonad
open DocMake.Builder.Document
open DocMake.Builder.Basis
open DocMake.Builder.ShellHooks

    


let makeCmd (infile:string) (outfile:string) : string = 
    sprintf "%s -f markdown -t docx -s -o %s" infile outfile 


let private docToPdfImpl (getHandle:'res-> PandocHandle) (mdDoc:MarkdownDoc) : BuildMonad<'res,WordDoc> =
    buildMonad { 
        match mdDoc.GetPath with
        | None -> return zeroDocument
        | Some mdPath -> 
            let name1 = System.IO.FileInfo(mdPath).Name
            let! path1 = askWorkingDirectory () |>> (fun cwd -> cwd </> name1)
            let outPath = System.IO.Path.ChangeExtension(path1, "pdf") 
            let _ =  pandocRunCommand getHandle <| makeCmd mdPath outPath
            return (makeDocument outPath)
    }




    
type MdToDocApi<'res> = 
    { MdToPdf : MarkdownDoc -> BuildMonad<'res, WordDoc> }

let makeAPI (getHandle:'res-> PandocHandle) : MdToDocApi<'res> = 
    { MdToPdf = docToPdfImpl getHandle }