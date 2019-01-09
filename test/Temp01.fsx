﻿// Copyright (c) Stephen Tetley 2018
// License: BSD 3 Clause

#r "netstandard"

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
#I @"C:\Users\stephen\.nuget\packages\Magick.NET-Q8-AnyCPU\7.9.2\lib\netstandard20"
#r @"Magick.NET-Q8-AnyCPU.dll"


#I @"C:\Users\stephen\.nuget\packages\markdowndoc\1.0.0\lib\netstandard2.0"
#r @"MarkdownDoc.dll"

open System.Text.RegularExpressions
open System.IO

let temp01 () = 
    let fileName = "MyFile.Z001.jpg"
    printfn "%s" fileName

    let justFile = Path.GetFileNameWithoutExtension fileName
    printfn "%s" justFile
    
    let patt = @"Z(\d+)$"
    let result = Regex.Match(justFile, patt)
    if result.Success then 
        int <| result.Groups.Item(1).Value
    else
        0

/// The temp indicator is a suffix "Z0.." before the file extension
let getNextTempName (filePath:string) : string =
    let root = System.IO.Path.GetDirectoryName filePath
    let justFile = Path.GetFileNameWithoutExtension filePath
    let extension  = System.IO.Path.GetExtension filePath

    let patt = @"Z(\d+)$"
    let result = Regex.Match(justFile, patt)
    let count = 
        if result.Success then 
            int <| result.Groups.Item(1).Value
        else 0
    let suffix = sprintf "Z%03d" (count+1)
    let newfile = sprintf "%s.%s%s" justFile suffix extension
    Path.Combine(root, newfile)