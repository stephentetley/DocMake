﻿// Copyright (c) Stephen Tetley 2019
// License: BSD 3 Clause


#r "netstandard"
open System

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
#I @"C:\Users\stephen\.nuget\packages\magick.net-q8-anycpu\7.11.1\lib\netstandard20"
#r @"Magick.NET-Q8-AnyCPU.dll"
#I @"C:\Users\stephen\.nuget\packages\magick.net-q8-anycpu\7.11.1\runtimes\win-x64\native"

// ExcelProvider
#I @"C:\Users\stephen\.nuget\packages\ExcelProvider\1.0.1\lib\netstandard2.0"
#r "ExcelProvider.Runtime.dll"

#I @"C:\Users\stephen\.nuget\packages\ExcelProvider\1.0.1\typeproviders\fsharp41\netstandard2.0"
#r "ExcelDataReader.DataSet.dll"
#r "ExcelDataReader.dll"
#r "ExcelProvider.DesignTime.dll"
open FSharp.Interop.Excel


// SLFormat & MarkdownDoc (not on nuget.org)
#I @"C:\Users\stephen\.nuget\packages\slformat\1.0.2-alpha-20190721\lib\netstandard2.0"
#r @"SLFormat.dll"
#I @"C:\Users\stephen\.nuget\packages\markdowndoc\1.0.1-alpha-20191014\lib\netstandard2.0"
#r @"MarkdownDoc.dll"


#load "..\src\DocBuild\Base\Internal\FilePaths.fs"
#load "..\src\DocBuild\Base\Internal\GhostscriptPrim.fs"
#load "..\src\DocBuild\Base\Internal\PandocPrim.fs"
#load "..\src\DocBuild\Base\Internal\PdftkPrim.fs"
#load "..\src\DocBuild\Base\Internal\ImageMagickPrim.fs"
#load "..\src\DocBuild\Base\Common.fs"
#load "..\src\DocBuild\Base\DocMonad.fs"
#load "..\src\DocBuild\Base\Document.fs"
#load "..\src\DocBuild\Base\Collection.fs"
#load "..\src\DocBuild\Base\FindFiles.fs"
#load "..\src\DocBuild\Base\FileOperations.fs"
#load "..\src\DocBuild\Document\Pdf.fs"
#load "..\src\DocBuild\Document\Jpeg.fs"
#load "..\src\DocBuild\Document\Markdown.fs"
#load "..\src\DocBuild\Extra\PhotoBook.fs"
#load "..\src\DocBuild\Extra\TitlePage.fs"

#load "..\src-msoffice\DocBuild\Office\Internal\Utils.fs"
#load "..\src-msoffice\DocBuild\Office\Internal\WordPrim.fs"
#load "..\src-msoffice\DocBuild\Office\Internal\ExcelPrim.fs"
#load "..\src-msoffice\DocBuild\Office\Internal\PowerPointPrim.fs"
#load "..\src-msoffice\DocBuild\Office\WordDocument.fs"
#load "..\src-msoffice\DocBuild\Office\ExcelDocument.fs"
#load "..\src-msoffice\DocBuild\Office\PowerPointDocument.fs"

open DocBuild.Base
open DocBuild.Document
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



let WindowsEnv : DocBuildEnv = 
    { WorkingDirectory  = @"G:\work\Projects\rtu\year5\output"
      SourceDirectory   = @"G:\work\Projects\rtu\year5"
      IncludeDirectories = [ @"G:\work\Projects\rtu\year5\include" ]
      PrintOrScreen = PrintQuality.Screen
      PandocOpts = 
        { CustomStylesDocx = None
          PdfEngine = Some "pdflatex"
        }
    }

let WindowsWordResources () : AppResources<WordDocument.WordHandle> = 
    let userRes = new WordDocument.WordHandle()
    { GhostscriptExe = @"C:\programs\gs\gs9.15\bin\gswin64c.exe"
      PdftkExe = @"pdftk"
      PandocExe = @"pandoc"
      UserResources = userRes
    }

type DocMonadWord<'a> = DocMonad<'a, WordDocument.WordHandle>

type SurveyTable = 
    ExcelFile< 
        FileName = @"G:\work\Projects\rtu\year5\RTU Asset Replacement Y5 Surveys.xlsx",
        ForceString = true >

type SurveyRow = SurveyTable.Row

let readSurveySpeadsheet () : SurveyRow list = 
    let helper = 
        { new IExcelProviderHelper<SurveyTable,SurveyRow>
          with member this.ReadTableRows table = table.Data 
               member this.IsBlankRow row = match row.GetValue(0) with null -> true | _ -> false }
         
    excelReadRowsAsList helper (new SurveyTable())

let survey (siteName:string) (saiNumber:string) : DocMonadWord<WordDoc> = 
    let outputName = sprintf "%s survey.docx" (safeName siteName)
    let searches : SearchList = [ ("#SAINUMBER", saiNumber); ("#SITENAME", siteName) ]
    docMonad { 
        let! (template:WordDoc) = getIncludeWordDoc "TEMPLATE Survey.docx"
        return! WordDocument.findReplaceAs searches outputName template
    }

let hazards (siteName:string) (saiNumber:string) : DocMonadWord<WordDoc> = 
    let outputName = sprintf "%s Hazard Identification Check List.docx" (safeName siteName)
    let searches : SearchList = [ ("#SAINUMBER", saiNumber); ("#SITENAME", siteName) ]
    docMonad { 
        let! (template:WordDoc) = getIncludeWordDoc "TEMPLATE Hazard Identification Check List.docx"
        return! WordDocument.findReplaceAs searches outputName template
    }

let ntrim (source:string) : string = 
    match source with
    | null -> ""
    | _ -> source.Trim()

let genSiteSheets (row:SurveyRow) : DocMonadWord<unit> = 
    printfn "%s" row.``SAI Site Name``
    let saiNumber = ntrim row.``SAI Number``
    let siteName = ntrim row.``SAI Site Name``
    localWorkingSubdirectory (safeName siteName) 
        <| docMonad { 
                do! survey siteName saiNumber |>> ignore
                do! hazards siteName saiNumber |>> ignore
                return ()
            }


let demo01 () = 
    let resources = WindowsWordResources ()
    runDocMonad resources WindowsEnv 
        <| survey "HORSEFIELD TERRACE/WPS" "ADB00023042"


let main () = 
    let sites = readSurveySpeadsheet () |> List.filter (fun row -> (String.IsNullOrEmpty row.``Surveyed Assigned ``))
    printfn "%i Sites" (List.length sites)
    let resources = WindowsWordResources ()
    runDocMonad resources WindowsEnv 
        <| forMz sites genSiteSheets
