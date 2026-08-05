# NGO RPC 笔记

> 适用项目：当前 NGO 项目  
> Unity：6000.4.0f1  
> Netcode for GameObjects：2.13.1  
> 推荐 API：通用 `[Rpc]`，不再以旧版 `[ServerRpc]` / `[ClientRpc]` 为主

这份笔记只讨论一个主题：**一台机器怎样请求另一台机器执行一个带参数的方法，以及 NGO 如何决定谁能调用、谁会收到、何时执行和是否重传。**

相关专题：[NGO 手动组包与序列化笔记](NGO手动组包与序列化笔记.md)

## 1. RPC 的正确心智模型

RPC 是 Remote Procedure Call，中文通常叫“远程过程调用”。代码看起来像普通方法调用：

```csharp
SubmitInputRpc(command);
```

但真实过程是：

```text
调用端执行 SubmitInputRpc(command)
        |
        | NGO 生成的调用代码拦截
        v
序列化方法参数
        |
        v
发送 RPC 消息
        |
        v
目标端接收并反序列化参数
        |
        v
目标端执行 SubmitInputRpc 的方法体
```

最重要的区别：

```text
普通方法：调用代码和方法体通常在同一进程立即执行
RPC：调用代码在发送端，方法体在目标端执行
```

如果目标包含本机，RPC 也可能在本地立即执行。Host 上尤其要注意这一点。

## 2. NGO 为什么要求 Rpc 后缀

声明通用 RPC：

```csharp
[Rpc(SendTo.Server)]
private void SubmitInputRpc(ActorInputCommand command)
{
    // 方法体在服务器执行
}
```

NGO 要求：

- 方法位于 `NetworkBehaviour` 中。
- 方法带 `[Rpc(...)]`。
- 方法名以 `Rpc` 结尾。
- 标准 RPC 不直接返回远端结果。
- 参数类型必须能被 NGO 序列化。

`Rpc` 后缀既帮助人一眼看出这是远程调用，也会被 NGO 的 ILPostProcessor 用来识别和生成代码。

Unity 编译后，NGO 的 IL 后处理器会改写 RPC 调用点。你写的：

```csharp
SubmitInputRpc(command);
```

不再只是普通的本地函数调用，而会生成组包和发送逻辑。因此不要通过反射、方法委托等方式假设它仍是普通方法入口。

## 3. 什么时候可以调用 RPC

RPC 依赖以下对象：

```text
NetworkManager 已启动并建立连接
        |
        v
NetworkObject 已 Spawn
        |
        v
NetworkBehaviour 已进入网络生命周期
        |
        v
可以调用 RPC
```

通常应在 `OnNetworkSpawn` 之后调用，并在调用前确认：

```csharp
IsSpawned == true
```

官方行为要点：

- 没有活动连接时调用的 RPC 不会自动排队等待以后连接，而会被忽略或丢弃。
- 发送方在可靠 RPC 真正发出前断开，这条 RPC 仍可能被丢弃。
- RPC 依赖 NetworkObject 身份，不能从任意普通 C# 类直接声明。

普通系统应把数据交给 `Actor : NetworkBehaviour`，由 Actor 在正确生命周期调用 RPC。

## 4. 先分清三个问题

阅读一个 RPC 时依次问：

1. **谁正在调用它？** 当前代码运行在哪个进程。
2. **谁有权限调用它？** 由 `InvokePermission` 控制。
3. **方法体在哪些目标执行？** 由 `SendTo` 控制。

目标和权限不是同一个概念：

```csharp
[Rpc(
    SendTo.Server,
    InvokePermission=RpcInvokePermission.Owner)]
private void SubmitInputRpc(...) { }
```

含义是：

```text
接收目标：Server
允许调用者：这个 NetworkObject 的 Owner
```

它不是“Server 拥有这个对象”，也不是“Owner 上执行方法体”。

## 5. SendTo 目标表

NGO 2.13.1 的常用编译期目标：

| 目标 | 方法体在哪里执行 | Host 注意点 |
| --- | --- | --- |
| `Server` | 服务器 | Server 本地调用时会本地执行 |
| `NotServer` | 除服务器外的观察客户端 | Host 被当作 Server，不执行 |
| `Owner` | NetworkObject 当前 Owner | 本机是 Owner 时本地执行 |
| `NotOwner` | 除 Owner 外的观察者 | Host 特殊情况下可能以 Server/Client 身份各收到一次 |
| `Me` | 只在本机执行 | 可配合延迟到下一帧 |
| `NotMe` | 除本机外的观察者 | 不包含调用机器 |
| `Everyone` | 所有观察者以及本机 | 本地也会执行 |
| `ClientsAndHost` | 所有客户端表现端，包含 Host Client | Dedicated Server 不执行 |
| `SpecifiedInParams` | 调用时必须提供目标 | 适合单个或一组客户端 |

大多数广播目标会按照当前 NetworkObject 的 Observer 列表过滤。不是这个对象观察者的客户端通常不会收到相应 RPC。

客户端之间没有直接连接。所谓 Client A 发给 Client B，消息仍会经过 Server 代理和转发。

## 6. NotServer 与 ClientsAndHost 的区别

两者在 Dedicated Server 下很像，但 Host 下不同：

```text
SendTo.NotServer
Dedicated Server：发给 Clients
Host：发给远程 Clients，不在 Host 本地执行

SendTo.ClientsAndHost
Dedicated Server：发给 Clients
Host：发给远程 Clients，也在 Host Client 执行
```

当前项目下行状态使用 `NotServer`，因为 Host 已经直接拥有服务器权威状态，不应再 Apply 一次。

如果是 UI、音效或只存在于客户端表现层的通知，可能更适合 `ClientsAndHost`。

## 7. InvokePermission 调用权限

```csharp
RpcInvokePermission.Everyone
RpcInvokePermission.Server
RpcInvokePermission.Owner
```

| 权限 | 谁可以调用 | 典型用途 |
| --- | --- | --- |
| `Everyone` | 任意已连接端，默认值 | 明确允许多方调用的消息 |
| `Server` | 仅 Server | 权威状态、服务器通知 |
| `Owner` | 仅 NetworkObject Owner | 玩家输入、Owner 请求 |

错误端调用 `Server` 或 `Owner` 权限 RPC 时，NGO 会拒绝并可能抛出异常。业务代码仍然要在调用前判断身份：

```csharp
if(!IsOwner)return;
SubmitInputRpc(command);
```

权限只验证调用身份，不验证参数内容。例如 Owner 仍可能提交：

- NaN 或 Infinity。
- 超过允许范围的输入。
- 伪造未来 Tick。
- 不符合冷却或角色状态的操作。

因此服务器必须继续做业务验证。

## 8. RpcParams 是什么

`RpcParams` 是可选的最后一个特殊参数：

```csharp
[Rpc(SendTo.Server)]
private void PingRpc(
    uint sequence,
    RpcParams rpcParams=default)
{
    ulong senderId=rpcParams.Receive.SenderClientId;
}
```

它有两种不同用途：

```text
发送端：读取 rpcParams.Send，决定运行时目标或本地延迟方式
接收端：NGO 填充 rpcParams.Receive，提供 SenderClientId
```

`RpcParams` 必须放在最后，并且它本身不会作为普通 Payload 序列化到网络中。

不要让客户端把自己的 ID 作为普通参数传上来：

```csharp
SubmitRpc(myClaimedClientId); // 不可信
```

应在接收端读取：

```csharp
rpcParams.Receive.SenderClientId
```

不过当前输入 RPC 已使用 `InvokePermission.Owner`，因此发送者身份首先由 NGO 限制；参数内容仍由 Input Channel 校验。

## 9. 运行时指定单个客户端

如果编译时不知道目标，可以声明：

```csharp
[Rpc(SendTo.SpecifiedInParams)]
private void ReplyRpc(
    uint sequence,
    RpcParams rpcParams=default)
{
}
```

调用时指定目标：

```csharp
ReplyRpc(
    sequence,
    RpcTarget.Single(clientId,RpcTargetUse.Temp));
```

也可以使用：

```csharp
RpcTarget.Group(clientIds,RpcTargetUse.Temp)
RpcTarget.Not(clientId,RpcTargetUse.Temp)
```

`RpcTargetUse` 的区别：

| 类型 | 用途 |
| --- | --- |
| `Temp` | 只用于当前调用，复用内部对象，减少 GC；之后可能被覆盖 |
| `Persistent` | 目标需要长期保存时创建独立对象 |

不要保存 `Temp` 返回的 Target 供以后使用。

如果 RPC 已声明固定目标，例如 `SendTo.Server`，运行时不能随意覆盖，除非：

```csharp
AllowTargetOverride=true
```

或者一开始就使用 `SendTo.SpecifiedInParams`。

## 10. 请求者是谁，怎样只回复请求者

完整 Ping/Pong 数据流：

```text
Client A 调用 PingRpc
        |
        v
Server 的 PingRpc 方法体
        |
        | rpcParams.Receive.SenderClientId == Client A
        v
Server 调用 ReplyRpc，运行时目标指定 Client A
        |
        v
只有 Client A 执行 ReplyRpc 方法体
```

示例：

```csharp
[Rpc(SendTo.Server)]
private void PingRpc(uint sequence,RpcParams rpcParams=default)
{
    ulong senderId=rpcParams.Receive.SenderClientId;
    PongRpc(
        sequence,
        RpcTarget.Single(senderId,RpcTargetUse.Temp));
}

[Rpc(SendTo.SpecifiedInParams)]
private void PongRpc(uint sequence,RpcParams rpcParams=default)
{
    Debug.Log($"Pong {sequence}");
}
```

## 11. RPC 的本地立即执行

如果目标包含本机，RPC 默认可能在当前调用栈立即执行，而不是等待真实网络：

```text
Host Server 调用目标包含 Host 的 RPC
        |
        v
Host 本地方法体可能立刻执行
```

这会导致两个风险：

- Host 的执行顺序与远程客户端不同。
- RPC A 的方法体立即调用 RPC B，互相递归时可能造成栈溢出。

可以声明：

```csharp
[Rpc(SendTo.Everyone,DeferLocal=true)]
private void ExampleRpc(RpcParams rpcParams=default) { }
```

或者单次调用时使用：

```csharp
ExampleRpc(LocalDeferMode.Defer);
```

这会把本地执行推迟到下一帧开头，使它更接近远程消息流程。它不会真的把本地消息绕网络发送一圈。

## 12. Reliable 与 Unreliable

RPC 默认是 Reliable：

```csharp
[Rpc(SendTo.Server)]
private void ReliableRpc() { }
```

显式使用 Unreliable：

```csharp
[Rpc(
    SendTo.Server,
    Delivery=RpcDelivery.Unreliable)]
private void UnreliableRpc() { }
```

### Reliable

- 连接存在时会尝试保证远端收到和执行。
- 丢包后需要重传，会增加带宽和等待。
- 同一个 NetworkObject 上的 Reliable RPC 保证调用顺序。
- 不同 NetworkObject 之间不保证全局顺序。

### Unreliable

- 允许丢失。
- 不保证调用顺序。
- 新状态不会等待已经过时的旧状态重传。
- 适合高频、下一包能够覆盖上一包的数据。

选择表：

| 数据 | 建议 | 原因 |
| --- | --- | --- |
| 每 Tick 输入/瞄准方向 | Unreliable + Tick/冗余 | 新数据很快覆盖旧数据 |
| 高频状态快照 | Unreliable | 避免旧状态排队 |
| 购买请求 | Reliable + 幂等序号 | 不能静默丢失，也不能重复扣款 |
| 聊天消息 | Reliable | 用户期望完整收到 |
| 非关键粒子或声音 | Unreliable | 丢一次通常可以接受 |
| 只发送一次的攻击边沿 | 不能只靠单个 Unreliable 包 | 需要冗余、确认或独立可靠事件 |

可靠不等于业务上“恰好执行一次”。断线、重试、应用层重复请求仍要求关键操作具有序号和幂等处理。

## 13. 调用顺序保证到什么范围

下面的顺序只对同一个 NetworkObject 上的 Reliable RPC 有保证：

```text
Actor A: Rpc1 -> Rpc2 -> Rpc3
远端 Actor A: Rpc1 -> Rpc2 -> Rpc3
```

下面没有跨对象全局保证：

```text
Actor A: Rpc1
Actor B: Rpc2
```

Unreliable RPC 不保证顺序。嵌套 RPC 又包含本地立即执行目标时，也可能出现与直觉不同的顺序，应使用明确状态机、序号或 `DeferLocal`，不要依赖复杂调用栈的偶然顺序。

## 14. RPC 参数如何传输

RPC 参数会被 NGO 自动序列化。常见支持类型包括：

- C# 基础值类型。
- Unity 常用值类型，例如 `Vector2`、`Vector3`、`Quaternion`。
- Enum。
- NGO 支持的数组和容器。
- 实现 `INetworkSerializable` 的类型。
- `NetworkObjectReference` 等网络引用类型。

不能把普通对象引用理解为跨机器共享引用：

```csharp
GameObject
MonoBehaviour
StateMachine
RunTimeData
```

不同进程有各自的对象实例。需要传 NetworkObject 时使用网络引用或稳定 ID，需要传业务状态时定义 Snapshot。

大 Payload 会增加序列化、复制、带宽和分片风险。RPC 参数应尽量表达一次消息真正需要的最小数据。

## 15. RPC 与 INetworkSerializable 的关系

`INetworkSerializable` 只定义“如何读写”，RPC 才定义“发给谁”：

```text
INetworkSerializable：字段 -> 字节的规则
RPC：发送目标、调用权限、可靠性和执行入口
```

例如：

```csharp
[Rpc(SendTo.Server)]
private void SubmitInputRpc(ActorInputCommand command)
{
}
```

NGO 会自动调用 `ActorInputCommand.NetworkSerialize`。

当前项目改为发送统一 `byte[]` 后，内部 Channel Payload 由我们手动序列化，但外层 `byte[]` 仍然作为 RPC 参数由 NGO 再次发送。

## 16. 当前项目的两个 RPC

实现位置：[Actor.NetWorkState.cs](../Assets/Scripts/Core/Actor/Actor.NetWorkState.cs)

### 输入上行

```csharp
[Rpc(
    SendTo.Server,
    InvokePermission=RpcInvokePermission.Owner,
    Delivery=RpcDelivery.Unreliable)]
private void SubmitReplicationRpc(byte[] packet)
```

```text
调用者：远程 Owner Client
接收者：Server
内容：OwnerToServer 方向的统一 Channel 包
权限：只有当前 Actor Owner 可调用
持久性：瞬时输入消息，不为晚加入者保存
```

Host Owner 不调用它，因为 Host 的输入已经直接进入同一进程的权威运行时数据。

### 状态下行

```csharp
[Rpc(
    SendTo.NotServer,
    InvokePermission=RpcInvokePermission.Server,
    Delivery=RpcDelivery.Unreliable)]
private void ApplyReplicationRpc(byte[] packet)
```

```text
调用者：Server
接收者：观察该 Actor 的非服务器 Clients
内容：ServerToClients 方向的统一 Channel 包
权限：只有 Server 可调用
持久性：瞬时快照；服务器靠后续 Tick 持续发送最新状态
```

选择 `NotServer` 是为了避免 Host 重复应用自己的权威状态。

## 17. RPC、NetworkVariable、Custom Messaging 怎么选

| 机制 | 适合 | 不自动提供 |
| --- | --- | --- |
| RPC | 一次请求、事件、命令、快照消息 | 晚加入者历史状态 |
| NetworkVariable | 持续存在的最新状态 | 每一次中间变化 |
| Custom Messaging | 自定义消息名、手动缓冲区和更底层协议 | NetworkBehaviour RPC 的便捷目标和代码生成 |

判断持续状态最简单的问题：

```text
晚加入的玩家是否必须立刻知道当前值？
```

如果答案是“是”，只依靠已经发生过的一次 RPC 不够。需要 NetworkVariable、Spawn 数据、显式完整快照或持续重发最新状态。

## 18. RPC 没有普通远端返回值

不能把远端 RPC 当成同步函数：

```csharp
int result=CalculateOnServerRpc(); // 不使用这种思路
```

网络有延迟，服务器结果只能稍后回来。使用请求/响应：

```text
Client RequestRpc(requestId, data)
        |
        v
Server 处理
        |
        v
Server ResponseRpc(requestId, result)
        |
        v
Client 根据 requestId 匹配等待中的请求
```

`requestId` 很重要，因为响应可能延迟、乱序，也可能在请求方已经取消操作后才到达。

## 19. RPC 与服务器权威

“代码在服务器执行”不等于“客户端请求必定成功”。推荐流程：

```text
Client RequestRpc
        |
        v
Server 检查发送者、状态、距离、冷却、资源、Tick
        |
        +---- 不合法：拒绝或返回失败
        |
        v
修改服务器权威状态
        |
        v
通过快照 / NetworkVariable / ResponseRpc 通知客户端
```

RPC 名称也应表达它是请求还是结果，例如：

```text
RequestInteractRpc
SubmitInputRpc
ApplyAuthoritativeStateRpc
NotifyHitRpc
```

避免客户端调用一个名为 `SetHealthRpc(9999)` 的入口，让意图和服务器结果混在一起。

## 20. Observer 与 Ownership

Ownership 决定“对象归哪个 Client”，Observer 决定“哪些 Client 当前能看到这个 NetworkObject”。

很多广播目标会经过 Observer 列表过滤：

```text
Server 调用 Actor 的 NotServer RPC
        |
        v
只发给当前观察这个 Actor 的 Clients
```

所以某客户端收不到 RPC 时，除了检查 Target，还要检查：

- NetworkObject 是否 Spawn。
- 客户端是否在 Observer 列表。
- 对象是否因为可见性/兴趣管理被隐藏。
- RPC 是否在对象 Despawn 前发出。

## 21. RPC 的版本兼容性

NGO 会根据 RPC 方法签名生成 32 位 Hash。Hash 受到这些内容影响：

- 所在程序集。
- 所在类型。
- 方法名。
- 参数类型。
- 返回类型属于方法签名的一部分。

只修改参数变量名通常不会改变签名 Hash，但下面这些会改变：

```text
SubmitRpc(int value)
SubmitRpc(float value)       // 参数类型变化
SendRpc(int value)          // 方法名变化
OtherActor.SubmitRpc(...)   // 所在类型变化
```

不同构建仍在线互通期间，不应随意修改已有 RPC 签名。需要滚动升级或跨版本兼容时，应保留旧入口一段时间，或设计显式协议版本。

方法 Hash 只是定位 RPC 的非加密 Hash，不是权限或安全校验。

## 22. 常见性能问题

### 每 Tick 大量独立 RPC

每条 RPC 都有消息头、参数序列化和调度成本。大量小 RPC 可以考虑按 Tick 集中组包。

### 每 Tick Reliable RPC

差网络下旧消息重传和排队会放大延迟。高频可覆盖数据通常考虑 Unreliable。

### 运行时目标分配

频繁创建托管目标数组或 Persistent Target 会增加 GC。一次性目标优先考虑 `RpcTargetUse.Temp`，但不能长期保存。

### byte[] 和 ToArray

当前统一包通过 `writer.ToArray()` 生成新数组，再作为 RPC 参数发送，会产生复制和 GC。功能稳定后再用 Profiler 判断是否改用 Custom Messaging。

### 广播本可单播的消息

请求响应应只回复请求客户端，不要把私人结果广播给所有人。

## 23. 常见错误速查

### 调用 RPC 后方法体在本机也执行了

检查目标是否包含本机。Host 和 `Everyone`、`Owner`、`ClientsAndHost` 都可能本地执行。

### RPC 完全没执行

检查连接、`IsSpawned`、NetworkObject Observer、Attribute、`Rpc` 后缀、调用权限和调用端身份。

### 非 Owner 调用时报错

RPC 使用了 `InvokePermission.Owner`。调用前用 `IsOwner` 保护，或者重新判断这个入口是否真的应该允许 Everyone。

### 只想回复请求者，却广播给所有人

在服务器读取 `rpcParams.Receive.SenderClientId`，使用 `RpcTarget.Single(...,RpcTargetUse.Temp)`。

### Host 正常，远程 Client 不正常

Host 目标可能本地立即执行，没有真实延迟和丢包。必须用至少一个远程客户端测试。

### Reliable 仍然感觉越来越慢

可靠只保证重传和顺序，不保证低延迟。检查是否高频发送、丢包后发生排队。

### 晚加入者不知道门已经打开

过去的一次 RPC 不会自动重放。门的当前状态应使用 NetworkVariable 或完整状态同步。

### 两个 Actor 的 Reliable RPC 顺序不一致

顺序保证只在同一个 NetworkObject 内成立，不跨 NetworkObject。

## 24. 调试日志应该记录什么

```csharp
Debug.Log(
    $"[RPC] Object={NetworkObjectId} " +
    $"Owner={OwnerClientId} " +
    $"Local={NetworkManager.LocalClientId} " +
    $"Server={IsServer} Client={IsClient} IsOwner={IsOwner} " +
    $"Sender={rpcParams.Receive.SenderClientId} " +
    $"Tick={tick}");
```

调试时分别记录：

- 调用 RPC 前的发送日志。
- RPC 方法体入口的接收日志。
- NetworkObjectId 和 OwnerClientId。
- SenderClientId。
- Tick 或请求序号。
- 业务验证拒绝原因。

不要只打印“RPC called”，否则无法判断是哪台机器、哪个 Actor、哪个 Tick。

## 25. 小练习：只回复请求客户端

### 目标

Owner Client 向 Server 发送一个 `sequence`，Server 只把相同 `sequence` 回复给请求者。

### 你的任务

1. 声明一个 `SendTo.Server` 的 `PingRpc`。
2. 在服务器从 `RpcParams.Receive` 获取发送者 ID。
3. 声明一个 `SendTo.SpecifiedInParams` 的 `PongRpc`。
4. 用 `RpcTarget.Single(...,RpcTargetUse.Temp)` 只回复请求者。

骨架：

```csharp
[Rpc(/* TODO: 目标 */)]
private void PingRpc(uint sequence,RpcParams rpcParams=default)
{
    ulong senderId=/* TODO */;
    PongRpc(
        sequence,
        /* TODO: 单个目标 */);
}

[Rpc(/* TODO: 运行时指定目标 */)]
private void PongRpc(uint sequence,RpcParams rpcParams=default)
{
    Debug.Log($"Pong {sequence}");
}
```

### 完成标准

- 两个客户端同时连接。
- Client A 发 Ping 时，只有 Client A 打印对应 Pong。
- Client B 不打印 Client A 的 Pong。
- Server 日志中的 SenderClientId 与 Client A 一致。

### 本练习暂不处理

- 超时。
- RTT 统计。
- 丢包重试。
- 请求取消。

## 26. RPC 设计检查表

声明 RPC 前回答：

1. 谁调用？
2. 谁接收？
3. `SendTo` 是否精确，是否广播过度？
4. `InvokePermission` 是 Everyone、Server 还是 Owner？
5. 方法体是否可能在 Host 本地立即执行？
6. 数据是瞬时事件还是持续状态？
7. 丢失能否由下一包覆盖？
8. Reliable 排队是否会让旧数据失去意义？
9. 参数是否是最小必要数据？
10. 服务器怎样验证参数？
11. 晚加入者是否需要当前结果？
12. 是否需要 requestId、Tick 或事件序号去重？
13. 是否只应回复请求者？
14. 修改签名是否会破坏在线版本兼容？

## 27. 当前版本官方资料

本项目安装版本的本地文档：

```text
Library/PackageCache/com.unity.netcode.gameobjects@d43d28498f17/Documentation~/advanced-topics/message-system/rpc.md
Library/PackageCache/com.unity.netcode.gameobjects@d43d28498f17/Documentation~/advanced-topics/message-system/rpc-params.md
Library/PackageCache/com.unity.netcode.gameobjects@d43d28498f17/Documentation~/advanced-topics/message-system/reliability.md
Library/PackageCache/com.unity.netcode.gameobjects@d43d28498f17/Documentation~/advanced-topics/message-system/rpc-compatibility.md
Library/PackageCache/com.unity.netcode.gameobjects@d43d28498f17/Documentation~/learn/rpcvnetvar.md
```

网上很多教程仍使用旧版 `[ServerRpc]` 和 `[ClientRpc]`。NGO 2.13.1 当前推荐通用 `[Rpc(SendTo...)]`，遇到写法冲突时优先参考项目实际安装版本的文档。
