open System
open System.IO
open System.Diagnostics

let buildCmd = """@echo off

if not exist .paket\paket.exe (
    .paket\paket.bootstrapper.exe
)

.paket\paket.exe restore

set encoding=utf-8
packages\build\FAKE\tools\FAKE.exe build.fsx %*
"""

let buildScript = """#I "packages/build/fake/tools/"
#r "FakeLib"

open Fake

let bin = "bin"

Target "Clean" <| fun _ ->
    DeleteDir bin

Target "Build" <| fun _ ->
    !! "**/*.?sproj"
    |> MSBuild bin "Rebuild" []
    |> Log "Build"

Target "All" DoNothing

"Clean" ==> "Build" ==> "All"

RunTargetOrDefault "All"
"""

let (</>) x y = System.IO.Path.Combine(x,y)

module Encoding =
    let utf8 = Text.Encoding.UTF8
    let noBom = new Text.UTF8Encoding(false)

module IO =
    let writeIfNeeded path filename encoding text =
        let filename = path </> filename
        if not <| File.Exists filename then
            File.WriteAllText(filename, text, encoding)
    

let writeBuildCmd path =
    IO.writeIfNeeded path "build.cmd" Encoding.noBom buildCmd

let writeBuildScript path =
    IO.writeIfNeeded path "build.fsx" Encoding.utf8 buildScript

let hasPacketDependency path =
    File.Exists(path </> "packet.dependencies")

let paket path cmdline=
    
    let proc = 
        Process.Start(
            ProcessStartInfo("paket",
                cmdline, 
                WorkingDirectory = path,
                WindowStyle = ProcessWindowStyle.Hidden ) )
    proc.WaitForExit()


let initialize() =
    let path = Environment.CurrentDirectory
    writeBuildCmd path
    writeBuildScript path

    if not (hasPacketDependency path) then
        paket path "init"
    
    paket path "add nuget Fake group build"


printfn "Initializing project"
initialize()
