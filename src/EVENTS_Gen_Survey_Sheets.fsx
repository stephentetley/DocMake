﻿// Office deps
#I @"C:\WINDOWS\assembly\GAC_MSIL\Microsoft.Office.Interop.Word\15.0.0.0__71e9bce111e9429c"
#r "Microsoft.Office.Interop.Word"
#I @"C:\WINDOWS\assembly\GAC_MSIL\Microsoft.Office.Interop.Excel\15.0.0.0__71e9bce111e9429c"
#r "Microsoft.Office.Interop.Excel"
#I @"C:\WINDOWS\assembly\GAC_MSIL\Microsoft.Office.Interop.PowerPoint\15.0.0.0__71e9bce111e9429c"
#r "Microsoft.Office.Interop.PowerPoint"
#I @"C:\Windows\assembly\GAC_MSIL\office\15.0.0.0__71e9bce111e9429c"
#r "office"


#I @"..\packages\Newtonsoft.Json.10.0.3\lib\net45"
#r "Newtonsoft.Json"
open Newtonsoft.Json


#I @"..\packages\ExcelProvider.0.8.2\lib"
#r "ExcelProvider.dll"
open FSharp.ExcelProvider


#I @"..\packages\FAKE.5.0.0-beta005\tools"
#r @"..\packages\FAKE.5.0.0-beta005\tools\FakeLib.dll"
open Fake
open Fake.Core
open Fake.Core.Environment
open Fake.Core.Globbing.Operators
open Fake.Core.TargetOperators


#load @"DocMake\Base\Common.fs"
#load @"DocMake\Base\OfficeUtils.fs"
#load @"DocMake\Base\JsonUtils.fs"
#load @"DocMake\Base\GENHelper.fs"
#load @"DocMake\Tasks\DocFindReplace.fs"
open DocMake.Base.Common
open DocMake.Base.GENHelper
open DocMake.Tasks.DocFindReplace

/// This is a one-to-many build (one site list, many docs), so 
// we don't use FAKE directly, we just use it as a library.


let _templateRoot   = @"G:\work\Projects\events2\gen-surveys-risks\__Templates"
let _outputRoot     = @"G:\work\Projects\events2\gen-surveys-risks\output"


type SiteTable = 
    ExcelFile< @"G:\work\Projects\events2\EDM2 Site-List.xlsx",
               SheetName = "SITE_LIST",
               ForceString = false >

type SiteRow = SiteTable.Row

let siteTableDict : GetRowsDict<SiteTable, SiteRow> = 
    { GetRows     = fun imports -> imports.Data 
      NotNullProc = fun row -> match row.GetValue(0) with | null -> false | _ -> true }


let filterByBatch (batchName:string) (source:SiteRow list) : SiteRow list = 
    let matchBatch (row:SiteRow) : bool = 
        match row.``Survey Batch`` with
        | null -> false
        | ans -> ans = batchName
    List.filter  matchBatch source

let getSiteRows (batchName:string) : SiteRow list = 
    excelTableGetRows siteTableDict (new SiteTable()) |> filterByBatch batchName



let makeTopFolder (batchName:string) : unit = 
    maybeCreateDirectory <| _outputRoot @@ batchName

let makeSiteFolder (batchName:string) (siteName:string) : unit = 
    let cleanName = safeName siteName
    maybeCreateDirectory <| _outputRoot @@ batchName @@ cleanName


let makeSurveySearches (row:SiteRow) : SearchList = 
    [ "#SITENAME",          row.Name
    ; "#SAINUMBER" ,        row.``SAI Number``
    ; "#SITEADDRESS",       row.``Site Address``
    ; "#OPERSTATUS",        row.``Operational Status``
    ; "#SITEGRIDREF",       row.``Site Grid Ref``
    ; "#ASSETTYPE",         row.Type
    ; "#OPERNAME",          row.``Operational Responsibility``
    ; "#OUTFALLGRIDREF",    row.``Outfall Grid Ref (from IW sheet)``
    ; "#RECWATERWOURSE",    row.``Receiving Watercourse``
    ]




let genSurvey (batchName:string)  (row:SiteRow) : unit =
    let template = _templateRoot @@ "TEMPLATE EDM2 Survey.docx"
    let cleanName = safeName row.Name
    let path1 = _outputRoot @@ batchName @@ cleanName
    let outPath = path1 @@ (sprintf "%s EDM2 Survey.docx" cleanName)
    DocFindReplace (fun p -> 
        { p with 
            TemplateFile = template
            OutputFile = outPath
            Matches  = makeSurveySearches row
        }) 

let makeHazardsSearches (row:SiteRow) : SearchList = 
    [ "#SITENAME",          row.Name
    ; "#SAINUMBER" ,        row.``SAI Number``
    ]

let genHazardSheet (batchName:string)  (row:SiteRow) : unit =
    let template = _templateRoot @@ "TEMPLATE Hazard Identification Check List.docx"
    let cleanName = safeName row.Name
    let path1 = _outputRoot @@ batchName @@ cleanName
    let outPath = path1 @@  (sprintf "%s Hazard Identification Check List.docx" cleanName)    
    DocFindReplace (fun p -> 
        { p with 
            TemplateFile = template
            OutputFile = outPath
            Matches = makeHazardsSearches row 
        }) 

// Generating all takes too long just generate a batch.



let main (batchName:string) : unit = 
    let siteList = getSiteRows batchName
    let todoCount = List.length siteList
    let safeBatchName = safeName batchName

    let proc1 (ix:int) (row:SiteRow) = 
        printfn "Generating %i of %i: %s ..." (ix+1) todoCount row.Name
        makeSiteFolder safeBatchName row.Name
        genSurvey safeBatchName row
        genHazardSheet safeBatchName row
    
    // actions...
    makeTopFolder safeBatchName
    siteList |> List.iteri proc1
