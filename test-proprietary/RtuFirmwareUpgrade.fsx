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

// ExcelProvider
#I @"C:\Users\stephen\.nuget\packages\ExcelProvider\1.0.1\lib\netstandard2.0"
#r "ExcelProvider.Runtime.dll"

#I @"C:\Users\stephen\.nuget\packages\ExcelProvider\1.0.1\typeproviders\fsharp41\netstandard2.0"
#r "ExcelDataReader.DataSet.dll"
#r "ExcelDataReader.dll"
#r "ExcelProvider.DesignTime.dll"


// ImageMagick
#I @"C:\Users\stephen\.nuget\packages\magick.net-q8-anycpu\7.11.1\lib\netstandard20"
#r @"Magick.NET-Q8-AnyCPU.dll"
#I @"C:\Users\stephen\.nuget\packages\magick.net-q8-anycpu\7.11.1\runtimes\win-x64\native"


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
#load "..\src\DocBuild\Base\Skeletons.fs"
#load "..\src\DocBuild\Document\Pdf.fs"
#load "..\src\DocBuild\Document\Jpeg.fs"
#load "..\src\DocBuild\Document\Markdown.fs"
#load "..\src\DocBuild\Extra\Contents.fs"
#load "..\src\DocBuild\Extra\PhotoBook.fs"
#load "..\src\DocBuild\Extra\TitlePage.fs"

#load "..\src-msoffice\DocBuild\Office\Internal\Utils.fs"
#load "..\src-msoffice\DocBuild\Office\Internal\WordPrim.fs"
#load "..\src-msoffice\DocBuild\Office\Internal\ExcelPrim.fs"
#load "..\src-msoffice\DocBuild\Office\Internal\PowerPointPrim.fs"
#load "..\src-msoffice\DocBuild\Office\WordDocument.fs"
#load "..\src-msoffice\DocBuild\Office\ExcelDocument.fs"
#load "..\src-msoffice\DocBuild\Office\PowerPointDocument.fs"
#load "..\src-msoffice\DocBuild\Office\PandocWordShim.fs"

open DocBuild.Base
open DocBuild.Base.DocMonad
open DocBuild.Document
open DocBuild.Office

#load "ExcelProviderHelper.fs"
#load "Proprietary.fs"
open Proprietary


// ImageMagick Dll loader.
// A hack to get over Dll loading error due to the 
// native dll `Magick.NET-Q8-x64.Native.dll`
[<Literal>] 
let NativeMagick = @"C:\Users\stephen\.nuget\packages\magick.net-q8-anycpu\7.9.2\runtimes\win-x64\native"
Environment.SetEnvironmentVariable("PATH", 
    Environment.GetEnvironmentVariable("PATH") + ";" + NativeMagick
    )



let WindowsEnv : DocBuildEnv = 
    { SourceDirectory   = @"G:\work\Projects\rtu\final-docs\input\firmware_upgrades1"
      WorkingDirectory  = @"G:\work\Projects\rtu\final-docs\output\firmware_upgrades1"
      IncludeDirectories = [ @"G:\work\Projects\rtu\final-docs\include"]
      PrintOrScreen = PrintQuality.Screen
      PandocOpts = 
        {  
          CustomStylesDocx = None
          PdfEngine = None
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

let sourceToSiteName (sourceName:string) : string = 
    sourceName.Replace("_", "/")


let sourceWordDocToPdf (fileGlob:string) : DocMonadWord<PdfDoc option> = 
    docMonad { 
        match! tryExactlyOne <<| findSourceFilesMatching fileGlob false with
        | None -> return None
        | Some infile ->
            let! doc = getWordDoc infile
            return! (WordDocument.exportPdf doc |>> Some)
    }

let genCoversheet (siteName:string) (saiNumber:string) : DocMonadWord<PdfDoc> = 
    let outputName = "coversheet.docx"
    let searches : SearchList = [ ("#SAINUMBER", saiNumber); ("#SITENAME", siteName) ]
    docMonad { 
        let! (template:WordDoc) = getIncludeWordDoc "FIRMWARE_UPGRADE RTU Cover Sheet.docx"
        let! docx = WordDocument.findReplaceAs searches outputName template
        return! WordDocument.exportPdf docx
    }

let genSiteWorks () : DocMonadWord<PdfDoc> = 
    optionToFailM "No Site Works document" 
                  (sourceWordDocToPdf "*Site Works*.doc*")
                


let build1 (saiMap : SaiMap) : DocMonadWord<PdfDoc> = 
    docMonad { 
        let! sourceName =  sourceDirectoryName ()
        let siteName = sourceName |> sourceToSiteName
        let! saiNumber = liftOption "No SAI Number" (getSaiNumber saiMap siteName)
        let! cover = genCoversheet siteName saiNumber
        let! workSheet = genSiteWorks ()
        let finalName = sprintf "%s S3953 Mk5 MMIM Firmware Upgrade.pdf" sourceName |> safeName
        let col1 = Collection.ofList [ cover; workSheet ]        
        return! Pdf.concatPdfs Pdf.GsDefault finalName col1 
    }



let main () = 
    let res = WindowsWordResources ()
    let saiMap = buildSaiMap ()
    runDocMonad res WindowsEnv 
        <| foreachSourceDirectory defaultSkeletonOptions (build1 saiMap)