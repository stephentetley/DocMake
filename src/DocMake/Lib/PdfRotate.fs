﻿module DocMake.Lib.PdfRotate

open System.IO

open Fake.Core
open Fake.Core.Process

open DocMake.Base.Common
open DocMake.Builder.BuildMonad
open DocMake.Builder.Basis
open DocMake.Builder.Builders

type PageRotation = int * DocMakePageOrientation


let private makeRotateSpec (rotations: PageRotation list) : string = 
    let rec work inlist start ac = 
        match inlist with
        | [] -> 
            let final = sprintf "A%i-end" start
            List.rev (final::ac)
        | (pageNum,po) :: rest -> 
            if pageNum = start then 
                let thisRotation = sprintf "A%i%s" pageNum (pdftkPageOrientation po)
                work rest (pageNum+1) (thisRotation :: ac)
            else 
                let thisRotation = sprintf "A%i%s" pageNum (pdftkPageOrientation po)
                let rangeAsIs = sprintf "A%i-%i" start (pageNum-1)
                work rest (pageNum+1) (thisRotation :: rangeAsIs :: ac)
    String.concat " " <| work rotations 1 []

let private makeCmd (inputFile:string) (outputFile:string) (rotations: PageRotation list)  : string = 
    //let rotateSpec = "cat A1east A2-end" // temp
    let rotateSpec = makeRotateSpec rotations
    sprintf "A=\"%s\" %s output \"%s\"" inputFile rotateSpec outputFile 


let pdfRotate (rotations: PageRotation list) (pdfDoc:PdfDoc) : PdftkBuild<PdfDoc> =
    buildMonad { 
        let! outDoc = freshDocument ()
        let! _ =  pdftkRunCommand <| makeCmd pdfDoc.DocumentPath outDoc.DocumentPath rotations
        return outDoc
    }
