﻿// Copyright (c) Stephen Tetley 2018
// License: BSD 3 Clause

// Office deps
#I @"C:\WINDOWS\assembly\GAC_MSIL\Microsoft.Office.Interop.Word\15.0.0.0__71e9bce111e9429c"
#r "Microsoft.Office.Interop.Word"
#I @"C:\WINDOWS\assembly\GAC_MSIL\Microsoft.Office.Interop.Excel\15.0.0.0__71e9bce111e9429c"
#r "Microsoft.Office.Interop.Excel"
#I @"C:\WINDOWS\assembly\GAC_MSIL\Microsoft.Office.Interop.PowerPoint\15.0.0.0__71e9bce111e9429c"
#r "Microsoft.Office.Interop.PowerPoint"
#I @"C:\Windows\assembly\GAC_MSIL\office\15.0.0.0__71e9bce111e9429c"
#r "office"

// Use FSharp.Data for CSV output
#I @"..\packages\FSharp.Data.3.0.0-beta3\lib\net45"
#r @"FSharp.Data.dll"


#I @"..\packages\ExcelProvider.1.0.1\lib\net45"
#r "ExcelProvider.Runtime.dll"

#I @"..\packages\ExcelProvider.1.0.1\typeproviders\fsharp41\net45"
#r "ExcelDataReader.DataSet.dll"
#r "ExcelDataReader.dll"
#r "ExcelProvider.DesignTime.dll"
open FSharp.Interop.Excel


#I @"..\packages\Magick.NET-Q8-AnyCPU.7.8.0\lib\net40"
#r @"Magick.NET-Q8-AnyCPU.dll"

open System.IO



#load "..\DocMake\DocMake\Base\Common.fs"
#load "..\DocMake\DocMake\Base\FakeLike.fs"
#load "..\DocMake\DocMake\Base\ImageMagickUtils.fs"
#load "..\DocMake\DocMake\Base\OfficeUtils.fs"
#load "..\DocMake\DocMake\Base\SimpleDocOutput.fs"
#load "..\DocMake\DocMake\Builder\BuildMonad.fs"
#load "..\DocMake\DocMake\Builder\Document.fs"
#load "..\DocMake\DocMake\Builder\Basis.fs"
#load "..\DocMake\DocMake\Builder\ShellHooks.fs"
open DocMake.Base.Common
open DocMake.Base.FakeLike
open DocMake.Builder.BuildMonad
open DocMake.Builder.Document
open DocMake.Builder.Basis

#load "..\DocMake\DocMake\Tasks\IOActions.fs"
#load "..\DocMake\DocMake\Tasks\DocFindReplace.fs"
#load "..\DocMake\DocMake\Tasks\XlsFindReplace.fs"
#load "..\DocMake\DocMake\Tasks\DocToPdf.fs"
#load "..\DocMake\DocMake\Tasks\XlsToPdf.fs"
#load "..\DocMake\DocMake\Tasks\PptToPdf.fs"
#load "..\DocMake\DocMake\Tasks\MdToDoc.fs"
#load "..\DocMake\DocMake\Tasks\PdfConcat.fs"
#load "..\DocMake\DocMake\Tasks\PdfRotate.fs"
#load "..\DocMake\DocMake\Tasks\DocPhotos.fs"
#load "..\DocMake\DocMake\FullBuilder.fs"
open DocMake.FullBuilder
open DocMake.Tasks

#load "Proprietry.fs" 
open Proprietry


// Output is just "Site Works" doc and the collected "Photo doc"

let _inputRoot      = @"G:\work\Projects\rtu\IS_barriers\final-docs\input\Batch03_June"
let _outputRoot     = @"G:\work\Projects\rtu\IS_barriers\final-docs\output\Batch03"


// No cover needed - but "Site Works" sheet must be converted to Pdf.
let siteWorks (siteInputDir:string) : FullBuild<PdfDoc> = 
    match tryFindExactlyOneMatchingFile "*Site Works*.doc*" siteInputDir with
    | Some source -> getDocument source >>= docToPdf
    | None -> throwError "No Site Works"


let photosDoc (docTitle:string) (jpegSrcPath:string) : FullBuild<PdfDoc> =
    let photoOpts:DocPhotos.DocPhotosOptions = 
        { DocTitle = Some docTitle; ShowFileName = true; CopyToSubDirectory = "Photos" } 

    docPhotos photoOpts [jpegSrcPath] >>= docToPdf
    




// *******************************************************


let buildScript1 (siteInputDir:string) : FullBuild<PdfDoc> = 
    let underscoreName      = DirectoryInfo(siteInputDir).Name
    printfn "underscoreName: %s"  underscoreName
    let properSiteName      = slashName underscoreName
    let jpegsSrcPath        = siteInputDir </> "PHOTOS"
    let finalName           = sprintf "%s S3953 IS Barrier Replacement.pdf" underscoreName
    localSubDirectory underscoreName <| 
        buildMonad { 
            do! IOActions.clean () 
            do! IOActions.createOutputDirectory ()
            let! p1 = makePdf "site-works.pdf"          <| siteWorks siteInputDir
            let! p2 = makePdf "site-work-photos.pdf"    <| photosDoc "Site Work Photos" jpegsSrcPath 
            let pdfs = [p1;p2]
            let! (final:PdfDoc) = makePdf finalName     <| pdfConcat pdfs
            return final            
        }


let buildScript () : FullBuild<unit> = 
    let inputs = 
        System.IO.Directory.GetDirectories(_inputRoot)  |> Array.toList 
    forMz inputs buildScript1


let main () : unit = 
    let env = 
        { WorkingDirectory = _outputRoot
          PrintQuality = PrintQuality.PqScreen
          PdfQuality = PdfPrintQuality.PdfScreen }

    let appConfig : FullBuildConfig = 
        { GhostscriptPath = @"C:\programs\gs\gs9.15\bin\gswin64c.exe"
          PdftkPath = @"C:\programs\PDFtk Server\bin\pdftk.exe"
          PandocPath = @"pandoc" } 

    runFullBuild env appConfig <| buildScript ()


