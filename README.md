# 2020s-Final-Project

In this project, you will write a health checking service (a server) that
provides a [RESTful
API](https://www.restapitutorial.com/lessons/whatisrest.html).

At a high level, the server takes an HTTP request as input and returns a JSON as
output. The server can register a client, and a registered client will send a
heart-beat message to the server to prove that it is alive. If the client is not
sending a heart-beat message for long, then the server should know that the
client is not alive.

### How to Get Started

You first need to visit Suave's [homepage](https://suave.io/), and read the
documentation such as [this](https://suave.io/index.html),
[this](https://suave.io/routing.html), and [this](https://suave.io/async.html).
You then read the boilerplate code and comments well to get started.

For your reference, the Suave package is used by the
[FsClassroom](https://github.com/KAIST-CS220/FsClassRoom) project. So the
project should be a good reference for you too.

### Expected Package to Use

You are highly reocmmended to use [Suave](https://suave.io/), which is already
included in the project template. Suave helps you to create a web server with
monadic combinators. To output Json, you do not really need to rely on a
package: you can simply build a JSON string, or you can use
[DataContractJsonSerializer](https://docs.microsoft.com/en-us/dotnet/api/system.runtime.serialization.json.datacontractjsonserializer?view=netcore-3.1).
Of course, you can use an additional package such as
[Chiron](https://github.com/xyncro/chiron) if you prefer to use it. Although,
the HealthChecker.Tests project uses Chrion, but you really don't need to use it
for this project; it is totally up to your decision.

### Execution of the Server

The server should run by typing `dotnet run` at the `HealthChecker` directory.
You should pass in three command line arguments by `dotnet run -- <ip> <port>
<freq>`, where `<ip>` and `<port>` are an IP address and a port number to bind,
and `<freq>` is to specify how many seconds the server will wait for the next
heartbeat message.

For example, `dotnet run -- 192.168.0.1 8888 10` means that the server will run
on `http://192.168.0.1:8888`, and it expects heartbeat signals to arrive every
10 seconds. If heartbeat messages come later than 10 seconds, it considers that
the client is *unavailable* for the time window.

### Tests

We provide 10 test cases at the `HealthChecker.Tests` project. You can test your
implementation by running `dotnet test`. We also ask you to write one or more
additional test case(s) in your deliverable (see the Deliverables section).

If your implementation is not complete, you will see the following error:
```
활성 테스트 실행이 중단되었습니다. 이유: 테스트 호스트 프로세스 작동이 중단됨 : Unhandled exception. System.Net.Sockets.SocketException (10048): 각 소켓 주소(프로토콜/네트워크 주소/포트)는 하나만 사용할 수 있습니다.
   at System.Net.Sockets.Socket.UpdateStatusAfterSocketErrorAndThrowException(SocketError error, String callerName)
```

This is normal: If your server is completely well functioning, then those errors
would disappear.

### Computing Availability

The server computes the availability of each client by considering heart-beat
messages for every cycle. Each cycle has `<freq>` seconds, and the server checks
heart-beat messages at the end of each cycle.

Specifically, it will measure every `<freq>` second if any heart-beat message
has been recieved within `<freq> * n - 1` second and `<freq> * n + 1` second,
where `n` means the n-th cycle. For example, let us suppose the frequency
parameter `<freq>` is given as 10 seconds. Then, every 10 second, the server
will check if a client has sent a heart-beat message from 9 to 11 second, from
19 to 21 second, and so on.

If the server does not see any message within a window, then the server will
decide that the client was inactive for the n-th cycle. For example, if a server
recieved a heartbeat message exactly after 8.5 second of registration, then the
server will consider that the very first cycle was inactive even though the
registration step was successful.

For simplicity, any messages outside of windows will be ignored. For example,
even though a client sends two heartbeat messages at 4 sec. and 5 sec., both
messages will be ignored, and the server will consider the first cycle was
inactive.

Also, we can only check the availability of the n-th cycle only after `<freq> *
n + 1` second. For example, we cannot decide if the first cycle was unavailable
until 10.999 seconds. We can only decide the failure of the first cycle after 11
seconds.

### Warning

Do not remove or rename the `runService` function in `Program.fs`. This function
is the main entry point for `HealthChecker.Tests`.

### Deliverables

Your deliverable is a GitHub repository that includes two directories:
`HealthChecker` and `HealthChecker.Tests`. You are free to modify the project
files, add/remove .fs files in the projects, but make sure the followings:

- The HealthChecker should run by typing `dotnet run` at the HealthChecker
  directory.
- The HealthChecker.Tests should run by typing `dotnet test` at the root
  directory.

We request you to create test cases for your health checker program. You should
add at least one new test case. Your test case should not be specific to a
certain implementation. This means, it should only use the REST API, but should
not directly call an F# function.

### Available URIs

We define a list of available URIs as below. For simplicity, you make all the
requests to be a `GET` method. Any error should return [400 Bad
Request](https://docs.microsoft.com/en-us/dotnet/api/system.net.httpstatuscode?view=netcore-3.1).

- `/register`
    - This request will return a random token (a unique ID) of 10 alphanumeric
      characters. The token will be used to register a client.
    - Example output:
        ```json
        { "token": "abcde12345" }
        ```

- `/register/<token>`
    - This request will register a client with the token `<token>`. The
      `<token>` should be the one that is published by the server, and should
      have never been used by any other clients.
    - This request returns either success or failure. If success is returned,
      the server should start tracking the health of the client.
    - Example output:
        ```json
        { "status": "success" }
        ```

- `/deregister/<token>`
    - This request will deregister the client, which has the `<token>`.
    - This request returns either success or failure. The failure can happen
      only in three different cases: (1) the given token does not exist; and (2)
      the given token has been already de-registered from the server.
    - Example output:
        ```json
        { "status": "success" }
        ```

- `/heartbeat/<token>`
    - This request updates the availability of the client of `<token>`. This
      heartbeat message should be delivered to the server from each client for
      every `<freq>` seconds, where `<freq>` is a user-configurable parameter
      from the command line of the server.
    - Example output:
        ```json
        { "status": "success" }
        ```

- `/status`
    - This request shows the *current* status of the registered clients.
    - The output is an array of client status, where each status entry contains
      information about (1) the client token, and (2) the number of failed
      cycles.
    - Example output (the first client was unavailable for two cycles so far):
        ```json
        [
          { "token": "abcde12345",
            "numfailures": 2 },
          { "token": "xxxxx11111",
            "numfailures": 0 }
        ]
        ```

### Debugging

You can run your server on the localhost by `dotnet run -- 127.0.0.1 9999 10`.
Once the command runs successful, you can then use `curl` or similar tools to
test your APIs. For example, `curl http://127.0.0.1:9999/` will send a GET
request to the root of the server. You can also use your browser to do debugging
although it can be cumbersome.

Another straightforward way to debug is to add more tests to the
HealthChecker.Tests project. You should carefully read and understand the
existing tests. We provide 10 test cases to help you debug your program.
