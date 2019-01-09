﻿// Copyright (c) Stephen Tetley 2018,2019
// License: BSD 3 Clause


namespace DocBuild.Document



// This should support extraction / rotation via Pdftk...

[<AutoOpen>]
module Pdf = 
    
    open System

    open DocBuild.Base
    open DocBuild.Base.Shell
    open DocBuild.Base.Monad
    open DocBuild.Raw.Ghostscript
    open DocBuild.Raw.Pdftk
    open DocBuild.Raw.PdftkRotate
    
    
    [<Struct>]
    type PdfFile = 
        | PdfFile of Document

        member x.Path 
            with get () : FilePath =
                match x with | PdfFile(p) -> p.Path

        /// ActiveFile is a mutable working copy of the original file.
        /// The original file is untouched.
        member x.NextTempName
            with get() : FilePath = 
                match x with | PdfFile(p) -> p.NextTempName


    
            
        //member x.RotateEmbed( options:ProcessOptions
        //                    , rotations: Rotation list)  : unit = 
        //    match pdfRotateEmbed options rotations x.PdfDoc.ActiveFile x.PdfDoc.ActiveFile with
        //    | ProcSuccess _ -> ()
        //    | ProcErrorCode i -> 
        //        failwithf "PdfDoc.RotateEmbed - error code %i" i
        //    | ProcErrorMessage msg -> 
        //        failwithf "PdfDoc.RotateEmbed - '%s'" msg
                
        //member x.RotateExtract( options:ProcessOptions
        //                      , rotations: Rotation list)  : unit = 
        //    match pdfRotateExtract options rotations x.PdfDoc.ActiveFile x.PdfDoc.ActiveFile with
        //    | ProcSuccess _ -> ()
        //    | ProcErrorCode i -> 
        //        failwithf "PdfDoc.RotateEmbed - error code %i" i
        //    | ProcErrorMessage msg -> 
        //        failwithf "PdfDoc.RotateEmbed - '%s'" msg




    let pdfFile (path:string) : DocBuild<PdfFile> = 
        getDocument ".pdf" path |>> PdfFile

    

    
    type GsQuality = 
        | GsScreen 
        | GsEbook
        | GsPrinter
        | GsPrepress
        | GsDefault
        | GsNone
        member internal v.QualityArgs
            with get() : CommandArgs = 
                match v with
                | GsScreen ->  reqArg "-dPDFSETTINGS" @"/screen"
                | GsEbook -> reqArg "-dPDFSETTINGS" @"/ebook"
                | GsPrinter -> reqArg "-dPDFSETTINGS" @"/printer"
                | GsPrepress -> reqArg "-dPDFSETTINGS" @"/prepress"
                | GsDefault -> reqArg "-dPDFSETTINGS" @"/default"
                | GsNone -> emptyArgs






    let ghostscriptConcat (inputfiles:PdfFile list)
                            (quality:GsQuality)
                            (outputFile:string) : DocBuild<string> = 
            let inputs = inputfiles |> List.map (fun d -> d.Path)
            let cmd = makeGsConcatCommand quality.QualityArgs outputFile inputs
            execGhostscript cmd