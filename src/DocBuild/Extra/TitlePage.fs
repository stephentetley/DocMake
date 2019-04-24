﻿// Copyright (c) Stephen Tetley 2019
// License: BSD 3 Clause

namespace DocBuild.Extra

module TitlePage = 

    open MarkdownDoc
    
    open DocBuild.Base
    open DocBuild.Base.DocMonad

    open DocBuild.Document
    open DocBuild.Document.Pdf
    open DocBuild.Document.Markdown


    let private genMarkdown (title:string) 
                            (body:Markdown option) : Markdown = 
        let d1 = h1 (text title)
        match body with
        | None -> d1
        | Some d2 -> d1 ^@^ d2




    type TitlePageConfig = 
        { Title: string
          DocBody: Markdown option
          RelativeOutputName: string }


    let makeTitlePage (config:TitlePageConfig) : DocMonad<'userRes, MarkdownDoc> =
        docMonad {
            let mdDoc = genMarkdown config.Title config.DocBody
            return! Markdown.saveMarkdown config.RelativeOutputName mdDoc
        }

    let genPrefixWithTitlePage (render: MarkdownDoc -> DocMonad<'userRes,PdfDoc>)
                               (title:string) 
                               (body: Markdown option) 
                               (pdf:PdfDoc) : DocMonad<'userRes,PdfDoc> =
        docMonad {
            // TODO this is imperminent, need an easy genfile function
            let temp = "title.temp.md"    
            let! md = makeTitlePage { Title = title; DocBody = body; RelativeOutputName = temp }
            let! title = render md
            let outName = modifyFileName (fun s -> s + "+title") pdf.FileName 
            return! pdftkConcatPdfs outName (Collection.fromList [title; pdf]) |>> setTitle pdf.Title
        }


    let prefixWithTitlePageWithTeX (title:string) 
                                   (body: Markdown option) 
                                   (pdf:PdfDoc) : DocMonad<'userRes,PdfDoc> =
        genPrefixWithTitlePage markdownToTeXToPdf title body pdf
        
