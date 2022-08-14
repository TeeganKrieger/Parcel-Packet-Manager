<br />
<div align="center">
  <h1>Parcel Packet Manager</h1>
  <h3 align="center">Simple Networking, Serialization, and Synchronization.</h3>
</div>

<a href="https://teegankrieger.github.io/Parcel-Packet-Manager/index.html">Documentation</a>

<!-- ABOUT THE PROJECT -->
## About The Project

Parcel Packet Manager is a lightweight library that simplifies networking. Parcel was designed primarily with video games in mind but can be applicable in non-video game contexts.

## Libraries and Tools used

* [Harmony](https://github.com/pardeike/Harmony)
* [FastDelegate](https://github.com/coder0xff/FastDelegate.Net)
* [DocFX](https://github.com/dotnet/docfx)


## Installation

Coming Soon...

Once version 0.4 has been reached in the Roadmap, Installation instructions and a build will be released. Until then, this project is considered too incomplete for proper use.

## Contributing

If you discover a bug, please create an issue and detail the steps to recreate the bug. You can also make suggestions for Parcel or the documentation using issues as well. Please be sure to be as thorough in your issues as you can be.

### Pull Requests

Before beginning a contribution, ensure what you are fixing or adding has been raised as an issue. You are more than welcome to begin working right after raising an issue, however, it is recommended that you await approval, especially for suggestions, as your work could go to waste if the idea is outright rejected. For bug fixes, be sure nobody else has raised the issue before or is working on resolving the issue, as your work could go to waste again.

Once you feel your contribution is production ready and you've ensured your code follows our style guide, feel free to submit a pull request. It will be reviewed and hopefully approved! 

## Roadmap

<!-- v0.1 - Initial Commit - Packet Transmission and Synced Objects work. -->
<!-- v0.2 - Disconnection - Manual and Automatic disconnection, kicking, blacklist/reject before join. -->
<!-- v0.3 - Improved Serializers - Find a way to support SyncedObject serializers. -->
<!-- v0.4 - More Serializers - Add most missing serializers such as object[] -->
<!-- v0.5 - Remote Procedure Calls - Introduce RPCs -->
<!-- v0.6 - Remote Events - Introduce Remote Events -->
<!-- v0.7 - v0.9 - Left open for bug fixes and additional features>

<!-- v1.0 - Full Release - Minimal Bugs, Synced Objects, RPCs, Remote Events,  -->

### v0.1
Client and Server, Packet Serialization, and Synced Objects.

| Status | Milestone | 
| :---: | :--- |
| ✔ | UDP Network Adapter |
| ✔ | Basic Serialization Framework |
| ✔ | Client and Server classes |
| ✔ | Packet Serialization |
| ✔ | Synced Object implementation |

### v0.2
Manual and Automatic Disconnection, Kicking, Blacklist, and Connection Rejection.

| Status | Milestone | 
| :---: | :--- |
| ✔ | Proper Manual Disconnection |
| ✔ | Automatic Disconnection |
| ✔ | Easy Ping System |
| ✔ | Server Kick Option | 
| ✔ | Server Connection Rejection Option |

### v0.3
Support for Custom Synced Object Serializers.

| Status | Milestone |
| :---: | :--- |
| ❌ | Make Synced Object Property Trackers Avaliable |
| ❌ | Add Support for Synced Object Serializers. |

### v0.4
Add Missing Serializers.

| Status | Milestone |
| :---: | :--- |
| ❌ | Object Array Serializer |
| ❌ | String Array Serializer |
| ❌ | Enum Array Serializer |


### v0.5
Synchronous and Asynchronous Remote Procedure Calls.

| Status | Milestone |
| :---: | :--- |
| ❌ | Client to Server RPC |
| ❌ | Server to Client RPC |

### v0.6
Remote Events.

| Status | Milestone |
| :---: | :--- |
| ❌ | Synced Object Remote Events |

### v1.0

| Status | Milestone |
| :---: | :--- |
| ❌ | Bug Fixes |