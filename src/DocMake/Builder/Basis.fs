﻿module DocMake.Builder.Basis

open System.IO
open System.Threading


open DocMake.Base.Common
open DocMake.Builder.BuildMonad


/// Document has a Phantom Type so we can distinguish between different types 
/// (Word, Excel, Pdf, ...)
/// Maybe we ought to store whether a file has been derived in the build process
/// (and so deleteable)... 
type Document<'a> = { DocumentPath : string }

let makeDocument (filePath:string) : Document<'a> = 
    { DocumentPath = filePath }


let freshDocument () : BuildMonad<'res,Document<'a>> = 
    fmapM makeDocument <| freshFileName ()

let documentExtension (doc:Document<'a>) : string = 
    System.IO.FileInfo(doc.DocumentPath).Extension


let documentDirectory (doc:Document<'a>) : string = 
    System.IO.FileInfo(doc.DocumentPath).DirectoryName

let documentChangeExtension (extension: string) (doc:Document<'a>) :Document<'a> = 
    let d1 = System.IO.Path.ChangeExtension(doc.DocumentPath, extension)
    makeDocument d1

let documentName (doc:Document<'a>) : string = 
    System.IO.FileInfo(doc.DocumentPath).Name


let assertFile(fileName:string) : BuildMonad<'res,string> =  
    if File.Exists(fileName) then 
        breturn(fileName)
    else 
        throwError <| sprintf "assertFile failed: '%s'" fileName

// assertExtension ?

let getDocument (fileName:string) : BuildMonad<'res,Document<'a>> =   
    if File.Exists(fileName) then 
        breturn({DocumentPath=fileName})
    else 
        throwError <| sprintf "getDocument failed: '%s'" fileName


let copyToWorkingDirectory (fileName:string) : BuildMonad<'res,Document<'a>> = 
    if File.Exists(fileName) then 
        buildMonad { 
            let name1 = System.IO.FileInfo(fileName).Name
            let! cwd = asksEnv (fun e -> e.WorkingDirectory)
            let dest = System.IO.Path.Combine(cwd,name1)
            do System.IO.File.Copy(fileName,dest)
            return (makeDocument dest)
        }
    else 
        throwError <| sprintf "getDocument failed: '%s'" fileName


let renameDocument (src:Document<'a>) (dest:string) : BuildMonad<'res,Document<'a>> =  
    executeIO <| fun () -> 
        let srcPath = src.DocumentPath
        let pathTo = documentDirectory src
        let outPath = System.IO.Path.Combine(pathTo,dest)
        if System.IO.File.Exists(outPath) then System.IO.File.Delete(outPath)
        System.IO.File.Move(srcPath,outPath)
        {DocumentPath=outPath}

let renameTo (dest:string) (src:Document<'a>) : BuildMonad<'res,Document<'a>> = 
    renameDocument src dest


let askWorkingDirectory () : BuildMonad<'res,string> = 
    asksEnv (fun e -> e.WorkingDirectory)

let deleteWorkingDirectory () : BuildMonad<'res,unit> = 
    buildMonad { 
        let! cwd = askWorkingDirectory ()
        do printfn "Deleting: %s" cwd
        do! executeIO <| fun () ->
            if System.IO.Directory.Exists(cwd) then System.IO.Directory.Delete(path=cwd,recursive=true)
        do! executeIO <| fun () -> Thread.Sleep(360)
        }


let createWorkingDirectory () : BuildMonad<'res,unit> = 
    buildMonad { 
        let! cwd = asksEnv (fun e -> e.WorkingDirectory)
        do! executeIO (fun () -> maybeCreateDirectory cwd) 
    }

let localWorkingDirectory (wd:string) (ma:BuildMonad<'res,'a>) : BuildMonad<'res,'a> = 
    localEnv (fun (e:Env) -> { e with WorkingDirectory = wd }) ma

let localSubDirectory (subdir:string) (ma:BuildMonad<'res,'a>) : BuildMonad<'res,'a> = 
    localEnv (fun (e:Env) -> 
                let cwd = System.IO.Path.Combine(e.WorkingDirectory, subdir)
                { e with WorkingDirectory = cwd }) 
            (createWorkingDirectory () >>. ma)

