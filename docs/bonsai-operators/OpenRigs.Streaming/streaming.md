# Overview

`OpenRigs.Streaming` contains Bonsai operators to stream live experiment telemetry over ZeroMQ, and receive it on a remote machine. A rig publishes values, images and events as they happen. A machine elsewhere on the same network subscribes to the named stream and returns them in their original types. This can be useful for remote monitoring and control of an ongoing experiment, allowing the delegation of sometimes heavy GUIs to a different machine than acquisition. For this reason, a publisher drops messages rather than blocking when a subscriber cannot keep up, and a slow remote viewer can never hold up acquisition. This makes it suitable for monitoring and remote displays, but unsuitable for collecting data to log. Logging should still be performed at the acquisition machine.

---

## Operators

| Operator | Role |
|----------|------|
| `StreamController` | Opens the publisher socket, declares the `OutgoingMessage` subject and defines and publishes rig/session identity. |
| `PackDataMessage` | Serializes a value and packs it into a message with Topic, JSON header and payload. |
| `PackVideoMessage` | Decimates, resizes, encodes and packs video frames into ZeroMQ messages. |
| `ReceiveStream` | Subscribes to a named stream and unpacks to a `StreamMessage`. |
| `UnpackDataMessage` | Deserializes the payload into `SessionKey`, `Index` and `Value`. |
| `UnpackVideoMessage` | Decodes a JPEG payload into an image with `SessionKey` and `Index`. |

Sending is `value` -> `PackDataMessage` -> `MulticastSubject`, with `StreamController` owning the socket. Receiving is `ReceiveStream` -> `UnpackDataMessage`, where the expected type is set on the second node. Video follows the same pair: `PackVideoMessage` on the rig, `ReceiveStream` -> `UnpackVideoMessage` on the viewer. Every stream, whatever its encoding, travels on the one socket and is told apart by its stream name.

---

## Quick start

Put a `StreamController` at the top of a workflow and set its `RigId`, then send any value into a `PackDataMessage` with a `StreamName`. The controller's publisher socket picks the message up from there. The two need no wire between them, which is why `StreamController` sits on its own below. Any number of streams share the one socket:

:::workflow
![StreamingSend](~/workflows/StreamingSend.bonsai)
:::

On the receiving machine, subscribe to the same rig and declare the type you expect. Subscribing to `data` picks up all streams under `data/`, and a `Condition` on `Topic` can split them again:

:::workflow
![StreamingReceive](~/workflows/StreamingReceive.bonsai)
:::

`UnpackDataMessage` parses each message to `SessionKey`, `Index` and `Value`, where `Value` has the type the publisher sent rather than a JSON string.

### Adding video

A camera goes on the same socket. Send its frames into a `PackVideoMessage` with a `StreamName` such as `video`. By default the packer takes one frame every 0.2 seconds, resizes it to 320 by 240 and encodes it as JPEG, which suits a live view. Like `PackDataMessage`, it needs no wire to the controller:

:::workflow
![StreamingSendVideo](~/workflows/StreamingSendVideo.bonsai)
:::

On the receiving machine, a second `ReceiveStream` on the same connection subscribes to `video`, and `UnpackVideoMessage` decodes each frame into an image as `Value`, with `SessionKey` and `Index` beside it:

:::workflow
![StreamingReceiveVideo](~/workflows/StreamingReceiveVideo.bonsai)
:::

A subscriber to `data` never receives a video frame, since ZeroMQ filters by topic at the publisher. One `PackVideoMessage` is needed per camera. Names such as `video/face` and `video/body` can both be picked up by a subscription to `video`.

---

## Wire contract

Three frames per message:

| Frame | Contents |
|-------|----------|
| 0 | Topic: `{rigId}/{stream}/`, trailing slash included |
| 1 | Header: JSON string |
| 2 | Payload: encoded as named by `enc` |

The header carries six fields:

| Field | Description |
|-------|-------------|
| `rigId` | Identifier of the publishing rig |
| `sessionKey` | Session the message belongs to, ideally matching the logging directory |
| `stream` | Stream name |
| `index` | Per-stream message counter |
| `enc` | Payload encoding: typically `json` or `jpeg` |
| `type` | The payload type, so a receiver knows what to deserialize into |

`sessionKey` should match the directory created by the logging package, which lets a streamed record be traced back to the data on disk. `index` increments per stream, allowing the receiver to detect dropped frames. `type` names the payload type.

Message format is ZeroMQ and JSON, and a consumer need not be Bonsai. Any language with a ZeroMQ binding, Python for example, can subscribe by topic prefix.

### Topic filtering notes

The trailing slash matters: without it a subscription to `MyRig/trial/` would also match `MyRig/trialSummary/`. It also means hierarchical names work. A camera published as `video/face` is picked up by a subscription to `video`, and a rig publishing `data/first` and `data/second` serves both to a subscriber asking for `data`.

Taking a group of streams and separating them again is done using a `Condition` on `Topic`. 

:::workflow
![StreamingReceive](~/workflows/StreamingReceive.bonsai)
:::

### Internal includes

Two workflows in the package are plumbing rather than operators that you would usually place by hand:

- **CreateEnvelope**: builds the topic string and the JSON header from the stream identity plus the `StreamName`, `Encoding` and `PayloadType` properties.
- **BuildMessage**: takes an encoded payload and the envelope and assembles the three-frame `NetMQMessage`.

`PackDataMessage` and `PackVideoMessage` both include `BuildMessage`, which in turn includes `CreateEnvelope`. Use them directly only when adding a new packer for an encoding the package does not cover.

---

## Cross-machine setup

Bind and connect are different: `@tcp://…` binds, `>tcp://…` connects. A publisher binds and a subscriber connects. A useful side effect of this behaviour is that it does not cost the network time or bandwidth to publish any stream if there is no subscriber. 

Set `0.0.0.0` on the publishing machine. This binds the stream to the local machine Network Interface Card, using the IP address assigned to that machine. The subscribing machine can then connect to this using the IP address of the publishing machine and port 5556, which carries every stream. You can find the IP address from the publishing machine in the command prompt by typing ipconfig /all. Look for the IPv4 address.

---
