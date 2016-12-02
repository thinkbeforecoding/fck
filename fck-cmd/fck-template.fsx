#I "packages/Fake/tools/"
#r "Fakelib.dll"
#load "fcklib/fcklib.fsx"

open System
open System.IO
open System.Xml.Linq
open Printf
open Fake
open FckLib
open Railway
open CommandLine
 
let findProjects path =
    !! (path </> "*/**.?sproj")
    |> Seq.map (fun f -> Path.GetDirectoryName(f))

type Binding = {
    Assembly: AssemblyIdentity
    Redirect: Redirect    
} with
    override this.ToString() =
        sprintf "%s %s" this.Assembly.Name this.Redirect.NewVersion        

and AssemblyIdentity = {
    Name: string
    PublicKeyToken: string 
    Culture: string }

and Redirect = {
    OldVersion: string
    NewVersion: string }

type BindingDiff = {
    Added: Binding list
    Changed: (Binding * Binding) list
    Removed: Binding list
} with 
    static member Empty = { Added = []; Changed = []; Removed = []}

    
module Bindings =
    open Xml
    let xn n = Xml.xns "urn:schemas-microsoft-com:asm.v1" + n
    let assemblyBinding = xn "assemblyBinding"
    let dependentAssembly = xn "dependentAssembly"
    let assemblyIdentity = xn "assemblyIdentity"
    let bindingRedirect = xn "bindingRedirect"
    let name = attribute "name"
    let publickKeyToken = attribute "publickKeyToken"
    let culture = attribute "culture"
    let oldVersion = attribute "oldVersion"
    let newVersion = attribute "newVersion"

module Runtime =
    open Xml
    let runtime = xn "runtime"
    let gcServer = xn "gcServer"
    let enabled e = attribute "enabled" e 

open Xml
open Bindings
let loadBindings filename =
    let document = XDocument.Load(filename: string)

    let assembly e = 
        { Name = name e
          PublicKeyToken = publickKeyToken e
          Culture = culture e}
    let redirect e =
        { OldVersion = oldVersion e
          NewVersion = newVersion e}

    document.Descendants assemblyBinding
    |> Seq.collect(elements dependentAssembly)
    |> Seq.map (fun d -> 
            { Assembly = d |> element assemblyIdentity |> assembly
              Redirect = d |> element bindingRedirect |> redirect })
    |> Set.ofSeq

let setBindings target source =
    let targetXml = XDocument.Load(target: string)
    let sourceXml = XDocument.Load(source: string)

    let sourceBindings = sourceXml.Descendants assemblyBinding |> Seq.tryHead
    let targetBindings = targetXml.Descendants assemblyBinding |> Seq.tryHead
    match sourceBindings, targetBindings with
    | Some s, Some t ->
        t.RemoveNodes() 
        s.Nodes() |> Seq.toArray |> t.Add
        
        targetXml.Save(target)
    | None, Some t ->
        t.Remove()
        targetXml.Save(target)
    | Some s, None ->
        let runtime =
            match targetXml.Descendants (Xml.xn "runtime") |> Seq.tryHead with
            | None ->
                let r = XElement(Xml.xn "runtime")
                targetXml.Element(Xml.xn "configuration").Add(r)
                r
            | Some r -> r
        let bindings = XElement(assemblyBinding)
        runtime.Add(bindings)
        s.Nodes() |> Seq.toArray |> bindings.Add
        targetXml.Save(target)
    | _ -> ()
    ()


let diffBindings config template =
    let isDefinedIn bindings binding =
        bindings
        |> Set.exists (fun b -> b.Assembly.Name = binding.Assembly.Name)
    let tryFindIn bindings binding =
        bindings
        |> Seq.tryFind (fun b -> b.Assembly.Name = binding.Assembly.Name)

    let configBindings = loadBindings config
    let templateBindings = loadBindings template

    {
        Added = 
            configBindings
            |> Set.filter (not << isDefinedIn templateBindings)
            |> Set.toList
        Changed =
            configBindings
            |> Set.toList
            |> List.choosePair (tryFindIn templateBindings)
            |> List.filter (uncurry2 (<>))

        Removed =
            templateBindings
            |> Set.filter (not << isDefinedIn configBindings)
            |> Set.toList
    }
let bindingDifferences path =
    let dirname = Path.GetFileName path
    let config = path </> "App.config"
    let template = path </> "App.template.config"

    
    match File.Exists config, File.Exists template with
    | true, true -> 
        logfn "%s" dirname
        let diff = diffBindings config template 
        if diff = BindingDiff.Empty then
            trace "    Template match"
            Success ()
        else        
            traceError "    Template and config are different"
            if not (List.isEmpty diff.Added) then
                traceImportant "Added"
                diff.Added
                |> List.iter (logfn "    %O")
            if not (List.isEmpty diff.Changed) then
                traceImportant "Changed"
                diff.Changed
                |> List.iter (uncurry2 (logfn "    %O\n    ==> %O")) 
            if not (List.isEmpty diff.Removed) then
                traceImportant "Removed"
                diff.Removed
                |> List.iter (logfn "    %O")
            Failure "Template and config are different"
    | true, false -> 
        logfn "%s" dirname
        logfn "    There is no template"
        Success ()
    | false, true -> 
        logfn "%s" dirname
        logfn "    There is no app.config"
        Success ()
    | false, false -> Success ()

let fixBindings path =
    let dirname = Path.GetFileName path
    let config = path </> "App.config"
    let template = path </> "App.template.config"

    
    match File.Exists config, File.Exists template with
    | true, true -> 
        let diff = diffBindings config template 
        if diff = BindingDiff.Empty then
            Success ()
        else        
            logfn "%s" dirname
            traceImportant "    Fix"
            setBindings template config
            

            Success ()
    | _ -> Success ()


match getCommandLine() with
| Cmd "check" -> 
    trace "Check templates" 
    
    findProjects Environment.CurrentDirectory
    |> Seq.map bindingDifferences
    |> Seq.toList
    |> allSucceeded
| Cmd "fix" ->
    trace "Fix binding redirects in template config"

    findProjects Environment.CurrentDirectory
    |> Seq.map fixBindings
    |> Seq.toList
    |> allSucceeded
| cmd :: _ ->
    traceError <| sprintf "Unknown command %s" cmd
    Help.show "template"
    Failure "Unknown command"
| [] ->
    traceError "No command"
    Help.show "template"
      
    Failure "Unknown command"   

|> exit
