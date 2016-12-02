#load "fcklib/FckLib.fsx"
open System.IO
open Printf
open FckLib.CommandLine
let root = __SOURCE_DIRECTORY__
let cmd =
    match getCommandLine() with
    | cmd :: _ -> cmd
    | _ -> "help"

let file cmd = sprintf "%s/fck-%s.txt" root cmd
 
let filename = file cmd

if File.Exists filename then
    File.ReadAllText filename
    |> printfn "%s"
else
    printfn "The command %s doesn't exist" cmd 
    printfn ""
    file "help"
    |> File.ReadAllText
    |> printfn "%s" 