module HealthChecker.Program

open System
open System.Threading
open Suave
open Suave.Operators
open Suave.Filters
open Suave.Successful
open Suave.RequestErrors

let createBinding ip port =
  HttpBinding.createSimple HTTP ip port

let root =
  OK "Hi, I am the HealthChecker. Use predefined POST messages to proceed."

(* FIXME: you want to fix this. You may want to make this as a function in the
   end (although this is not completely necessary). *)
let parts =
  [ path "/" >=> root
    (* FIXME: add your APIs by registering WebParts here. *)
    BAD_REQUEST "Invalid access!" ]

/// Do not rename this function as this is the main entry point for
/// HealthChecker.Tests.
let runService ip port freq (cts: CancellationTokenSource) =
  ignore freq // FIXME you should use the freq parameter in the end.
  let cfg =
    { defaultConfig with
        bindings = [ createBinding ip port ]
        cancellationToken = cts.Token }
  choose (parts) |> startWebServerAsync cfg

[<EntryPoint>]
let main argv =
  if Array.length argv = 3 then
    let ip = argv.[0]
    let port = int argv.[1]
    let freq = float argv.[2]
    let cts = new CancellationTokenSource ()
    let _, server = runService ip port freq cts
    Async.RunSynchronously (server, cancellationToken = cts.Token)
    0
  else
    eprintfn "Usage: dotnet run -- <ip> <port> <freq>"
    1
