#load "fcklib/FckLib.fsx"
#r "packages/FAKE/tools/FakeLib.dll"

open FckLib.CommandLine
open Fake.TraceHelper

let name =
    match getCommandLine() with
    | name :: _ -> name
    | _ -> "you"

tracefn "Hello %s" name
