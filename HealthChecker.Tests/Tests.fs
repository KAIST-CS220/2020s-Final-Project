namespace HealthChecker.Tests

open Chiron
open System.Threading
open System.Net.Http
open Microsoft.VisualStudio.TestTools.UnitTesting

type SuccessCode = {
  Status: string
}
with
  static member ToJson (s: SuccessCode) = json {
    do! Json.write "status" s.Status
  }
  static member FromJson (_: SuccessCode) = json {
    let! status = Json.read "status"
    return { Status = status }
  }

type ClientStatus = {
  Token: string
  NumFailures: int
}
with
  static member ToJson (s: ClientStatus) = json {
    do! Json.write "token" s.Token
    do! Json.write "numfailures" s.Token
  }
  static member FromJson (_: ClientStatus) = json {
    let! token = Json.read "token"
    let! numFailures = Json.read "numfailures"
    return { Token = token; NumFailures = numFailures }
  }

[<TestClass>]
type TestClass () =

  let ip = "127.0.0.1"
  let port = 12345
  let freq = 3.0

  let send req =
    let url = "http://" + ip + ":" + port.ToString () + "/" + req
    async {
      use client = new HttpClient ()
      let! msg = client.GetAsync (url) |> Async.AwaitTask
      let! str = msg.Content.ReadAsStringAsync () |> Async.AwaitTask
      return msg.StatusCode, str
    }

  let establish ip port freq =
    let cts = new CancellationTokenSource ()
    let _, server = HealthChecker.Program.runService ip port freq cts
    Async.Start (server, cancellationToken = cts.Token)
    Async.Sleep 500 |> Async.RunSynchronously
    cts

  let destroy (cts: CancellationTokenSource) =
    cts.Cancel ()

  [<TestMethod>]
  member __.``Basic Connection Test (register)``() =
    let cts = establish ip port freq
    let _, ret = send "register" |> Async.RunSynchronously
    Assert.AreEqual (10, ret.Length) // Register should return 10 characters.
    destroy cts

  [<TestMethod>]
  member __.``Registration should always return different random strings``() =
    let cts = establish ip port freq
    let _, ret1 = send "register" |> Async.RunSynchronously
    let _, ret2 = send "register" |> Async.RunSynchronously
    Assert.AreEqual (10, ret1.Length)
    Assert.AreEqual (10, ret2.Length)
    Assert.AreNotEqual (ret1, ret2)
    destroy cts

  [<TestMethod>]
  member __.``Registration should be done with know token``() =
    let cts = establish ip port freq
    let status, _ = send "register/abc" |> Async.RunSynchronously
    Assert.AreEqual (System.Net.HttpStatusCode.BadRequest, status)
    destroy cts

  [<TestMethod>]
  member __.``Regular Registration Test``() =
    let cts = establish ip port freq
    let _, r = send "register" |> Async.RunSynchronously
    let registerMsg = sprintf "register/%s" r
    let status, r = send registerMsg |> Async.RunSynchronously
    let expected = { Status = "success" }
    let actual: SuccessCode = (Json.parse >> Json.deserialize) r
    Assert.AreEqual (System.Net.HttpStatusCode.OK, status)
    Assert.AreEqual (expected, actual)
    destroy cts

  [<TestMethod>]
  member __.``No Duplicate Registration Requests``() =
    let cts = establish ip port freq
    let _, r = send "register" |> Async.RunSynchronously
    let registerMsg = sprintf "register/%s" r
    let status, r = send registerMsg |> Async.RunSynchronously
    Assert.AreEqual (System.Net.HttpStatusCode.OK, status)
    let expected = { Status = "success" }
    let actual: SuccessCode = (Json.parse >> Json.deserialize) r
    Assert.AreEqual (expected, actual)
    let status, _ = send registerMsg |> Async.RunSynchronously
    Assert.AreEqual (System.Net.HttpStatusCode.BadRequest, status)
    destroy cts

  [<TestMethod>]
  member __.``Deregistration should be done with know token``() =
    let cts = establish ip port freq
    let status, _ = send "deregister/abc" |> Async.RunSynchronously
    Assert.AreEqual (System.Net.HttpStatusCode.BadRequest, status)
    destroy cts

  [<TestMethod>]
  member __.``Empty Status``() =
    let cts = establish ip port freq
    let status, r = send "status" |> Async.RunSynchronously
    Assert.AreEqual (System.Net.HttpStatusCode.OK, status)
    Assert.AreEqual ("[]", r)
    destroy cts

  [<TestMethod>]
  member __.``Heartbeat message should have a know token``() =
    let cts = establish ip port freq
    let status, _ = send "heartbeat/abc" |> Async.RunSynchronously
    Assert.AreEqual (System.Net.HttpStatusCode.BadRequest, status)
    destroy cts

  [<TestMethod>]
  member __.``Regular Heartbeat Test``() =
    let cts = establish ip port freq
    let _, token = send "register" |> Async.RunSynchronously
    let registerMsg = sprintf "register/%s" token
    let status, _ = send registerMsg |> Async.RunSynchronously
    Assert.AreEqual (System.Net.HttpStatusCode.OK, status)
    let heartbeatMsg = sprintf "heartbeat/%s" token
    let status, _ = send heartbeatMsg |> Async.RunSynchronously
    Assert.AreEqual (System.Net.HttpStatusCode.OK, status)
    let status, r = send "status" |> Async.RunSynchronously
    Assert.AreEqual (System.Net.HttpStatusCode.OK, status)
    let expected = [| { Token = token; NumFailures = 0 } |]
    let actual: ClientStatus [] = (Json.parse >> Json.deserialize) r
    CollectionAssert.AreEqual(expected, actual)
    destroy cts

  [<TestMethod>]
  member __.``Heartbeat Failure Test``() =
    let cts = establish ip port freq
    let _, token = send "register" |> Async.RunSynchronously
    let registerMsg = sprintf "register/%s" token
    let status, _ = send registerMsg |> Async.RunSynchronously
    Assert.AreEqual (System.Net.HttpStatusCode.OK, status)
    Async.Sleep (int (freq * 1000.0)) |> Async.RunSynchronously
    let heartbeatMsg = sprintf "heartbeat/%s" token
    let _ = send heartbeatMsg |> Async.RunSynchronously
    Async.Sleep (int (freq * 500.0)) |> Async.RunSynchronously
    let heartbeatMsg = sprintf "heartbeat/%s" token
    let _ = send heartbeatMsg |> Async.RunSynchronously
    Async.Sleep (int (freq * 1000.0)) |> Async.RunSynchronously
    let status, r = send "status" |> Async.RunSynchronously
    Assert.AreEqual (System.Net.HttpStatusCode.OK, status)
    let expected = [| { Token = token; NumFailures = 1 } |]
    let actual: ClientStatus [] = (Json.parse >> Json.deserialize) r
    CollectionAssert.AreEqual(expected, actual)
    destroy cts

  (* ADD YOUR OWN TEST CASE HERE *)
