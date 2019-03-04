﻿// Copyright (c) Stephen Tetley 2019
// License: BSD 3 Clause


#r "netstandard"
open System
open System.Text.RegularExpressions

// Office deps
#I @"C:\WINDOWS\assembly\GAC_MSIL\Microsoft.Office.Interop.Word\15.0.0.0__71e9bce111e9429c"
#r "Microsoft.Office.Interop.Word"
#I @"C:\WINDOWS\assembly\GAC_MSIL\Microsoft.Office.Interop.Excel\15.0.0.0__71e9bce111e9429c"
#r "Microsoft.Office.Interop.Excel"
#I @"C:\WINDOWS\assembly\GAC_MSIL\Microsoft.Office.Interop.PowerPoint\15.0.0.0__71e9bce111e9429c"
#r "Microsoft.Office.Interop.PowerPoint"
#I @"C:\Windows\assembly\GAC_MSIL\office\15.0.0.0__71e9bce111e9429c"
#r "office"

// ImageMagick
#I @"C:\Users\stephen\.nuget\packages\magick.net-q8-anycpu\7.9.2\lib\netstandard20"
#r @"Magick.NET-Q8-AnyCPU.dll"
#I @"C:\Users\stephen\.nuget\packages\magick.net-q8-anycpu\7.9.2\runtimes\win-x64\native"

// ExcelProvider
#I @"C:\Users\stephen\.nuget\packages\ExcelProvider\1.0.1\lib\netstandard2.0"
#r "ExcelProvider.Runtime.dll"

#I @"C:\Users\stephen\.nuget\packages\ExcelProvider\1.0.1\typeproviders\fsharp41\netstandard2.0"
#r "ExcelDataReader.DataSet.dll"
#r "ExcelDataReader.dll"
#r "ExcelProvider.DesignTime.dll"
open FSharp.Interop.Excel


// SLFormat & MarkdownDoc (not on nuget.org)
#I @"C:\Users\stephen\.nuget\packages\slformat\1.0.2-alpha-20190227\lib\netstandard2.0"
#r @"SLFormat.dll"
#I @"C:\Users\stephen\.nuget\packages\markdowndoc\1.0.1-alpha-20190228\lib\netstandard2.0"
#r @"MarkdownDoc.dll"



#load "..\src\DocBuild\Base\FakeLikePrim.fs"
#load "..\src\DocBuild\Base\FilePaths.fs"
#load "..\src\DocBuild\Base\Common.fs"
#load "..\src\DocBuild\Base\Shell.fs"
#load "..\src\DocBuild\Base\DocMonad.fs"
#load "..\src\DocBuild\Base\DocMonadOperators.fs"
#load "..\src\DocBuild\Base\Document.fs"
#load "..\src\DocBuild\Base\Collection.fs"
#load "..\src\DocBuild\Base\FileOperations.fs"
#load "..\src\DocBuild\Raw\GhostscriptPrim.fs"
#load "..\src\DocBuild\Raw\PandocPrim.fs"
#load "..\src\DocBuild\Raw\PdftkPrim.fs"
#load "..\src\DocBuild\Raw\ImageMagickPrim.fs"
#load "..\src\DocBuild\Document\Pdf.fs"
#load "..\src\DocBuild\Document\Jpeg.fs"
#load "..\src\DocBuild\Document\Markdown.fs"
#load "..\src\DocBuild\Extra\PhotoBook.fs"

#load "..\src-msoffice\DocBuild\Office\Internal\Utils.fs"
#load "..\src-msoffice\DocBuild\Office\Internal\WordPrim.fs"
#load "..\src-msoffice\DocBuild\Office\Internal\ExcelPrim.fs"
#load "..\src-msoffice\DocBuild\Office\Internal\PowerPointPrim.fs"
#load "..\src-msoffice\DocBuild\Office\WordDocument.fs"
#load "..\src-msoffice\DocBuild\Office\ExcelDocument.fs"
#load "..\src-msoffice\DocBuild\Office\PowerPointDocument.fs"

open DocBuild.Base
open DocBuild.Base.DocMonad
open DocBuild.Base.DocMonadOperators
open DocBuild.Document
open DocBuild.Extra.PhotoBook
open DocBuild.Office


#load "ExcelProviderHelper.fs"
open ExcelProviderHelper

// ImageMagick Dll loader.
// A hack to get over Dll loading error due to the 
// native dll `Magick.NET-Q8-x64.Native.dll`
[<Literal>] 
let NativeMagick = @"C:\Users\stephen\.nuget\packages\magick.net-q8-anycpu\7.9.2\runtimes\win-x64\native"
Environment.SetEnvironmentVariable("PATH", 
    Environment.GetEnvironmentVariable("PATH") + ";" + NativeMagick
    )

//open MarkdownDoc
//let tempTC () = 
//    inlineImage "" (unixLikePath "G:\work\photo1.jpg") None


let WindowsEnv : BuilderEnv = 
    { WorkingDirectory = DirectoryPath @"G:\work\Projects\rtu\final-docs\output\year4-batch2"
      SourceDirectory =  DirectoryPath @"G:\work\Projects\rtu\final-docs\input\year4-batch2"
      IncludeDirectory = DirectoryPath @"G:\work\Projects\rtu\final-docs\include"
      GhostscriptExe = @"C:\programs\gs\gs9.15\bin\gswin64c.exe"
      PdftkExe = @"pdftk"
      PandocExe = @"pandoc" }

type DocMonadWord<'a> = DocMonad<WordDocument.WordHandle,'a>


type WorkTable = 
    ExcelFile< 
        FileName = @"G:\work\Projects\rtu\final-docs\input\year4-batch2\year4-docs.xlsx",
        ForceString = true >

type WorkRow = WorkTable.Row


let readWorkSpeadsheet () : WorkRow list = 
    let helper = 
        { new IExcelProviderHelper<WorkTable,WorkRow>
          with member this.ReadTableRows table = table.Data 
               member this.IsBlankRow row = match row.GetValue(0) with null -> true | _ -> false }
         
    excelReadRowsAsList helper (new WorkTable())

let renderMarkdownDoc (stylesheetName:string option)
                       (docTitle:string)
                       (markdown:MarkdownDoc) : DocMonadWord<PdfDoc> =
    docMonad {
        let! (stylesheet:WordDoc option) = 
            match stylesheetName with
            | None -> mreturn None
            | Some name -> includeWordDoc name |>> Some
 
        let! docx = Markdown.markdownToWord stylesheet markdown
        let! pdf = WordDocument.exportPdf PqScreen docx |>> setTitle docTitle
        return pdf
    }


let coverSeaches (row:WorkRow) : SearchList = 
    [ ("#SAINUM", row.``Sai Number``)
    ; ("#SITENAME", row.``Site Name``) 
    ; ("#YEAR", row.``Year ``) ]


let genCover (workRow:WorkRow) : DocMonadWord<PdfDoc> = 
    let outputName = sprintf "%s cover.docx" (workRow.``Site Name`` |> safeName )
    let searches : SearchList = coverSeaches workRow
    docMonad { 
        let! (template:WordDoc) = includeWordDoc "TEMPLATE MM3x-to-MMIM Cover Sheet.docx"
        let! outpath = getOutputPath outputName
        let! wordFile = WordDocument.findReplaceAs searches outpath template
        return! WordDocument.exportPdf PrintQuality.PqScreen wordFile
    }

let sourceWordDocToPdf (folder1:string) (fileGlob:string) (row:WorkRow) :DocMonadWord<PdfDoc option> = 
    let subdirectory = folder1 </> (row.``Site Name`` |> safeName ) 
    localSourceSubdirectory (subdirectory) 
        <| docMonad { 
            printfn "<<<<<"
            let! input = tryFindExactlyOneSourceFileMatching fileGlob false
            printfn ">>>>>"
            match input with
            | None -> return None
            | Some infile ->
                let! doc = getWordDoc infile
                return! (WordDocument.exportPdf PqScreen doc |>> Some)
        }


let genSurvey (row:WorkRow) :DocMonadWord<PdfDoc option> = 
    sourceWordDocToPdf "1.Surveys" "*urvey*.doc*" row
    

let genSiteWorks (row:WorkRow) :DocMonadWord<PdfDoc> = 
    optionFailM (sourceWordDocToPdf "2.Installs" "*Works*.doc*" row)
                "No Site Works document"

type PhotosProps = 
    { Title: string
      SourceSubpath: string 
      WorkingSubpath: string 
      OutputFileRelPath: string
    }

let (docxCustomReference:string) = @"custom-reference1.docx"

let photosDoc (props:PhotosProps) : DocMonadWord<PdfDoc> = 
    docMonad { 
        let! md = 
            makePhotoBook props.Title 
                          props.SourceSubpath  
                          props.WorkingSubpath props.OutputFileRelPath
        let! pdf = renderMarkdownDoc (Some docxCustomReference) props.Title md
        return pdf
    }

let genSurveyPhotos (row:WorkRow) : DocMonadWord<PdfDoc option> = 
    let name1 = safeName row.``Site Name``
    let props : PhotosProps = 
        { Title = "Survey Photos"
        ; SourceSubpath = "1.Surveys" </> name1 </> "photos"
        ; WorkingSubpath = "survey_photos"
        ; OutputFileRelPath= sprintf "%s survey photos.md" name1 }
    optionalM (photosDoc props)


let genWorkPhotos (row:WorkRow) : DocMonadWord<PdfDoc option> = 
    let name1 = safeName row.``Site Name``
    let props : PhotosProps = 
        { Title = "Install Photos"
        ; SourceSubpath = "2.Install" </> name1 </> "photos"
        ; WorkingSubpath = "install_photos"
        ; OutputFileRelPath= sprintf "%s install photos.md" name1 }
    optionalM (photosDoc props)
    

let genFinal (row:WorkRow) :DocMonadWord<PdfDoc> = 
    let safeSiteName = row.``Site Name`` |> safeName
    localWorkingSubdirectory (safeSiteName) 
        <| docMonad { 
                let! cover = genCover row 
                let! oSurvey = genSurvey row
                let! works = genSiteWorks row
                let! oSurveyPhotos = genSurveyPhotos row
                let! oWorksPhotos = genWorkPhotos row

                let (col:PdfCollection) = 
                    Collection.singleton cover 
                        &>> oSurvey     &>> oSurveyPhotos
                        &>> works       &>> oWorksPhotos

                let! outputAbsPath = extendWorkingPath (sprintf "%s Final.pdf" safeSiteName)
                return! Pdf.concatPdfs Pdf.GsScreen outputAbsPath col
            }


let isLike (pattern:string) (source:string) = 
    Regex.IsMatch(input=source, pattern=pattern)

let main () = 
    let sites = readWorkSpeadsheet () 
                    |> List.filter (fun row -> isLike "OWTHORNE" row.``Site Name``)
    printfn "%i Sites" (List.length sites)
    let userRes = new WordDocument.WordHandle()
    runDocMonad userRes WindowsEnv 
        <| forMz sites genFinal