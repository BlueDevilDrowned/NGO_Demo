# NGO 手动组包与序列化笔记

> 适用项目：当前 NGO 项目  
> Unity：6000.4.0f1  
> Netcode for GameObjects：2.13.1  
> 对应实现：`Assets/Scripts/Core/RunTimeData/Snapshot`

这份笔记只讨论一个主题：**怎样把多个系统的数据手动组织成一个网络数据包，并在另一端安全地还原和分发。**

## 1. 先建立整体认识

网络不能直接理解 `ActorInputCommand`、状态机或 C# 对象。真正跨网络传输的是一串字节：

```text
ActorInputCommand
        |
        | 序列化
        v
FastBufferWriter 中的字节
        |
        | RPC / Custom Messaging
        v
另一台机器收到的字节
        |
        | 反序列化
        v
ActorInputCommand
```

这里包含三个不同层次：

| 层次 | 解决的问题 | 当前项目中的工具 |
| --- | --- | --- |
| 游戏数据 | 要同步什么 | `ActorInputCommand`、`ActorStateSnapshot` |
| 序列化与组包 | 数据如何变成有结构的字节 | `INetworkSerializable`、`FastBufferWriter/Reader` |
| 网络传输 | 字节怎样到达另一台机器 | NGO RPC，未来也可用 Custom Messaging |

`FastBufferWriter` 不是计算机网络的通用标准名称，而是 NGO 的具体 API。它背后的“序列化、数据包、包头、Payload、长度校验”属于通用网络开发知识。

## 2. 什么情况下需要手动组包

普通 RPC 已经自动完成序列化：

```csharp
[Rpc(SendTo.Server)]
private void SubmitInputRpc(ActorInputCommand command)
{
}
```

调用它时，NGO 生成的代码会在底层自动执行：

```text
command -> Writer -> 网络 -> Reader -> command
```

下面这些情况才值得手动组包：

- 多个系统按注册方式集中同步。
- 希望一个 Tick 只提交一个统一数据包。
- 需要自定义 `ChannelId`、Payload 长度或协议版本。
- 需要接收端按数据类别动态分发。
- 以后准备使用 Custom Messaging、输入历史、快照或回滚。

如果只有一个简单数据结构，直接使用强类型 RPC 通常更清楚，不必为了“更底层”而手动组包。

## 3. 必须认识的术语

### Serialization：序列化

把结构化数据按照固定规则写成字节。

### Deserialization：反序列化

按照相同规则把字节还原成结构化数据。

### Header：包头

描述数据包结构的元数据，例如 Channel 数量、ChannelId 和 Payload 长度。

### Payload：载荷

真正的业务数据，例如一个 `ActorInputCommand`。

### Framing：分帧或定界

接收端需要知道一段数据从哪里开始、到哪里结束。当前协议使用 Payload 长度定界。

### Protocol：协议

双方共同遵守的字节排列规则。字段类型、顺序、ChannelId 和长度都属于协议。

## 4. FastBufferWriter 是什么

`FastBufferWriter` 是 NGO 提供的高性能二进制写入器：

```csharp
using FastBufferWriter writer=new(
    256,
    Allocator.Temp,
    4096);
```

三个构造参数分别表示：

| 参数 | 含义 |
| --- | --- |
| `256` | 初始容量，单位是字节 |
| `Allocator.Temp` | 使用 Unity 临时非托管内存 |
| `4096` | 允许增长到的最大容量 |

关键认识：

- Writer 只写字节，不发送网络消息。
- Writer 使用非托管内存，用完必须 `Dispose()`。
- `using` 能保证正常结束、提前 `return` 或异常时都释放内存。
- `WriteValueSafe` 会检查剩余空间，必要时在最大容量内扩容。
- `Position` 是当前写入位置，`Length` 是已写入数据长度。
- `Seek` 移动位置，`Truncate` 截断已经写入的数据。
- `ToArray()` 会复制出一个新的托管 `byte[]`，因此会产生 GC 分配。

当前代码使用 `using`：

```csharp
using FastBufferWriter writer=new(
    InitialReplicationBufferSize,
    Allocator.Temp,
    MaxReplicationBufferSize);

snapshotReplicator.WriteAll(
    ActorReplicationDirection.OwnerToServer,
    in context,
    writer);

SubmitReplicationRpc(writer.ToArray());
```

Writer 在这里按值传递仍然有效，是因为 NGO 的这个结构体内部保存了指向同一缓冲区 Handle 的指针。复制的是轻量包装，不是复制整份 Payload。**这是 `FastBufferWriter` 的具体实现特性，不要推断所有 struct 按值传递都会共享内部数据。**

## 5. FastBufferReader 是什么

`FastBufferReader` 执行相反操作：

```csharp
using FastBufferReader reader=new(packet,Allocator.Temp);

reader.ReadValueSafe(out ushort channelCount);
reader.ReadNetworkSerializable(out ActorInputCommand command);
```

需要注意：

- Reader 不接收网络消息，只读取已经收到的字节。
- 从托管 `byte[]` 创建 Reader 时会将数据复制到指定分配器管理的内存。
- 读取顺序和类型必须与写入顺序完全一致。
- `TryBeginRead(size)` 可以在读取前检查剩余字节。
- `ReadValueSafe` 自带边界检查。
- `Position` 不应超过 `Length`。
- Reader 同样需要及时 `Dispose()`。

## 6. INetworkSerializable 如何连接 Writer 和数据

`ActorInputCommand` 实现了 `INetworkSerializable`。下面为了突出序列化过程，省略了 `InputLook`、按键 Flags 等字段：

```csharp
public struct ActorInputCommand : INetworkSerializable
{
    public uint Tick;
    public Vector2 InputMove;
    public float ViewYaw;

    public void NetworkSerialize<T>(BufferSerializer<T> serializer)
        where T : IReaderWriter
    {
        serializer.SerializeValue(ref Tick);
        serializer.SerializeValue(ref InputMove);
        serializer.SerializeValue(ref ViewYaw);
    }
}
```

同一个函数同时用于写和读：

```text
T 是 Writer -> 从字段读取值，写入字节
T 是 Reader -> 从字节读取值，写回字段
```

这里使用 `ref`，是因为反序列化时 Serializer 必须能修改字段。

字段顺序属于网络协议。下面两端不匹配就是错误：

```text
发送：Tick -> InputMove -> ViewYaw
接收：Tick -> ViewYaw -> InputMove   // 错误
```

调用：

```csharp
writer.WriteNetworkSerializable(in command);
```

最终会进入 `command.NetworkSerialize(...)`。读取端的：

```csharp
reader.ReadNetworkSerializable(out ActorInputCommand command);
```

也会进入同一个 `NetworkSerialize(...)`。

## 7. 当前项目的数据包格式

当前统一包格式定义在 [ActorSnapshotReplicator.cs](../Assets/Scripts/Core/RunTimeData/Snapshot/ActorSnapshotReplicator.cs)：

```text
Packet
├── ChannelCount : ushort
├── Record 0
│   ├── ChannelId    : ushort
│   ├── PayloadLength: int
│   └── Payload      : byte[PayloadLength]
├── Record 1
│   ├── ChannelId
│   ├── PayloadLength
│   └── Payload
└── ...
```

例如一个只包含输入的包：

```text
[1]
[ActorInput ChannelId = 1]
[PayloadLength = N]
[ActorInputCommand 的 N 字节]
```

选择这些字段的原因：

- `ChannelCount`：接收端知道需要循环几次。
- `ChannelId`：知道交给哪个 Channel。
- `PayloadLength`：知道这段 Payload 的边界，也能跳过未知 Channel。

## 8. 写包为什么需要占位和回填

写 Payload 之前通常不知道最终长度，因此使用下面的过程：

```text
1. 记录 Length 字段的位置
2. 先写入 0 占位
3. 写入 Payload
4. 用结束位置 - 开始位置算出 PayloadLength
5. Seek 回 Length 字段并写入真实长度
6. Seek 回包尾，继续写下一个 Channel
```

对应伪代码：

```csharp
int lengthPosition=writer.Position;
writer.WriteValueSafe(0);

int payloadPosition=writer.Position;
WritePayload(writer);

int endPosition=writer.Position;
int payloadLength=endPosition-payloadPosition;

writer.Seek(lengthPosition);
writer.WriteValueSafe(payloadLength);
writer.Seek(endPosition);
```

如果 Channel 判断本 Tick 不需要提交，Replicator 会：

```csharp
writer.Truncate(recordPosition);
```

这样之前写入的 ChannelId 和长度占位也会被撤销。

## 9. 读包的安全顺序

不要收到 `byte[]` 后直接反序列化。推荐顺序是：

```text
1. 检查整个 packet 是否为空、是否超过最大长度
2. 读取 ChannelCount
3. 检查是否还有完整的 ChannelId + PayloadLength
4. 验证 PayloadLength >= 0
5. 验证 PayloadLength <= 剩余字节
6. 根据 ChannelId 找到注册的 Channel
7. 验证 Channel 方向
8. 反序列化 Payload
9. 验证 Reader 最终位置刚好等于 Payload 结束位置
10. 完成所有 Record 后验证没有多余尾部字节
```

为什么要检查“刚好读完”：

- 少读说明两端字段协议不一致，或者包里夹带了未知数据。
- 多读说明 Payload 长度错误，读取跨进了下一个 Record。
- 先验证再 Apply，避免非法数据先污染 `RunTimeData`。

`PayloadLength` 允许跳过未知 Channel，但不能让同一个 Channel 随意改变字段。修改已有 Payload 时，应同步升级两端，或者引入协议版本/新的 ChannelId。

## 10. 泛型 Channel 怎样实现统一处理

每种数据保留自己的强类型：

```csharp
ActorReplicationChannel<ActorInputCommand>
ActorReplicationChannel<ActorStateSnapshot>
```

但 Replicator 的集合保存非泛型基类：

```csharp
List<ActorReplicationChannel> channels;
```

因此：

```text
具体 Channel<TData> 知道怎样处理自己的 TData
                 |
                 v
非泛型 ActorReplicationChannel 统一暴露 Write / ReadAndApply
                 |
                 v
Replicator 可以用一个循环处理所有 Channel
```

这是一种常见的“类型擦除到统一基类 + 内部保留强类型”的设计。

## 11. 关键 C# 关键字的设计目的

### readonly struct

```csharp
public readonly struct ActorReplicationContext
```

表示整个上下文创建后不可修改。Channel 只能读取本次同步的网络身份和 Tick，不能意外改写。

### in

```csharp
TryWrite(in ActorReplicationContext context,...)
Apply(in ActorReplicationContext context,in TData payload)
```

`in` 表示只读引用：

- 被调用方法不能给参数重新赋值。
- 对较大的 struct 可以避免值复制。
- 即使性能收益很小，也明确表达“这里只读”的接口意图。

### out

```csharp
bool TryWrite(...,out TData payload)
```

这里需要同时返回两件事：

- `bool`：本 Tick 是否提交。
- `payload`：要提交的具体数据。

`out` 要求方法在所有返回路径上给参数赋值，所以失败路径通常先写：

```csharp
payload=default;
```

### ref

```csharp
serializer.SerializeValue(ref Tick);
```

Reader 必须把网络读取结果写回字段，因此这里不能使用只读的 `in`。

### where TData : struct, INetworkSerializable

限制 `TData` 必须是值类型，并且明确提供 NGO 序列化方法。错误类型会在编译期被拒绝。

### sealed override

基类固定“捕获 -> 序列化”和“反序列化 -> 校验 -> Apply”的流程。具体 Channel 只能实现 `TryWrite` 和 `Apply`，不能绕过公共校验步骤。

### readonly 字段

```csharp
private readonly List<ActorReplicationChannel> channels=new();
```

这里只表示字段不能改指向另一个 List，不表示 List 内容不可增删。因此仍然可以调用 `Add`、`Remove` 和 `Clear`。

### using 声明

```csharp
using FastBufferWriter writer=new(...);
```

离开当前作用域时自动调用 `Dispose()`。它和文件顶部的 `using Unity.Netcode;` 不是同一种用途。

## 12. Writer 和 RPC 分别负责什么

当前项目的完整上行链路：

```text
Owner CaptureLocalInput
        |
        v
ActorInputReplicationChannel.TryWrite
        |
        v
ActorSnapshotReplicator.WriteAll
        |
        v
FastBufferWriter 生成统一字节包
        |
        v
writer.ToArray()
        |
        v
SubmitReplicationRpc(byte[])
        |
        v
Server 创建 FastBufferReader
        |
        v
ActorInputReplicationChannel.Apply
```

职责边界：

```text
Writer/Reader：字节格式
RPC：发送方、接收方、权限、可靠性
Channel：某种数据能否写、怎样捕获、怎样验证和应用
Replicator：注册、统一组包、查找和分发
Actor：生命周期、Tick、创建关联和真正调用 RPC
```

## 13. 两个 RPC 方向

### Owner -> Server

```csharp
[Rpc(
    SendTo.Server,
    InvokePermission=RpcInvokePermission.Owner,
    Delivery=RpcDelivery.Unreliable)]
private void SubmitReplicationRpc(byte[] packet)
```

- `SendTo.Server`：目标只有服务器。
- `Owner` 权限：只有这个 NetworkObject 的 Owner 能调用。
- `Unreliable`：旧输入丢失时不重传，避免高频可靠消息排队。
- 服务器仍要验证 Tick、输入范围、Flags 和数值合法性。

### Server -> Clients

```csharp
[Rpc(
    SendTo.NotServer,
    InvokePermission=RpcInvokePermission.Server,
    Delivery=RpcDelivery.Unreliable)]
private void ApplyReplicationRpc(byte[] packet)
```

- `NotServer`：发送给非服务器客户端。
- Server 权限：客户端不能伪造权威下行状态。
- Host 已经拥有服务器权威状态，不需要再 Apply 一次。

## 14. 为什么使用 Unreliable

输入和状态快照每 Tick 都会产生较新版本。网络丢失 Tick 100 后，如果 Tick 101 已经到达，通常没有必要等待 Tick 100 重传。

```text
Reliable：旧包丢失 -> 重传旧包 -> 后续包可能等待
Unreliable：旧包丢失 -> 接受更新的包
```

但 `Unreliable` 不适合直接承载“只出现一次且不能丢”的事件，例如一次购买确认或只发送一次的攻击边沿。常见解决方式：

- 最近多个 Tick 的输入冗余。
- 事件序号和去重。
- 在确认前重复携带关键事件。
- 真正必须恰好执行一次的操作使用独立 Reliable 消息，并由服务器幂等处理。

## 15. Host 为什么需要特殊处理

Host 同时满足：

```csharp
IsServer == true;
IsClient == true;
IsOwner == true; // Host 自己的 PlayerObject
```

当前输入提交入口使用：

```csharp
if(!IsOwner||IsServer)return;
```

含义是：

- 远程 Owner 客户端需要 RPC 到服务器。
- Host Owner 的输入已经直接写在同一进程的权威 `RunTimeData`，不用 RPC 给自己。

下行使用 `SendTo.NotServer`，也避免 Host 把服务器状态再次 Apply 到自己。

## 16. 网络数据为什么不能直接信任

即使 RPC 限制为 Owner，Owner 客户端仍然可能发送非法业务数据。服务器至少检查：

- 包长度是否合理。
- Channel 方向是否正确。
- Tick 是否重复、过旧或异常超前。
- `float` 是否为 NaN/Infinity。
- 移动输入是否超过允许长度。
- Flags 是否包含未定义位。
- 当前状态、冷却和资源是否允许该操作。

`InvokePermission.Owner` 只能证明“是谁发的”，不能证明“内容正确”。

## 17. 当前实现的性能成本

当前实现优先保证结构清晰，存在这些分配和复制：

```text
FastBufferWriter 非托管缓冲区
        |
        | ToArray：复制 + 新建 byte[]
        v
RPC 参数序列化：NGO 再写入自己的发送缓冲区
        |
        v
接收 byte[]
        |
        | FastBufferReader(byte[])：复制到 Reader 内存
        v
反序列化
```

高 Tick、多 Actor 时需要用 Profiler 测量 GC 和带宽。后续优化方向包括：

- 只在数据变化或规定频率时写某些 Channel。
- 合并/量化字段，减小 Payload。
- 避免每 Tick `ToArray()`。
- 使用 NGO Custom Messaging 直接发送 Writer。
- 使用可复用缓冲区或 Native 容器。

不要在功能链路尚未验证时立刻做所有优化。先确认发送者、接收者、权限、Tick 和 Apply 行为正确，再用数据决定优化点。

## 18. 怎样新增一个 Channel

以未来的 Movement 同步为例，需要完成：

```text
1. 定义 MovementSnapshot : INetworkSerializable
2. 分配唯一且稳定的 ChannelId
3. 继承 ActorReplicationChannel<MovementSnapshot>
4. 在 TryWrite 中检查写权限并捕获快照
5. 在 Apply 中检查接收端身份并应用数据
6. Actor 创建 Movement 后创建 Channel
7. 向 ActorSnapshotReplicator 注册
8. 分别测试 Host、远程 Owner、观察客户端
```

练习骨架：

```csharp
public sealed class MovementReplicationChannel
    : ActorReplicationChannel<MovementSnapshot>
{
    public override ushort ChannelId=>/* TODO: 唯一 Id */;
    public override ActorReplicationDirection Direction=>
        /* TODO: 谁发给谁 */;

    protected override bool TryWrite(
        in ActorReplicationContext context,
        out MovementSnapshot payload)
    {
        payload=default;

        // TODO 1：检查谁有写权限
        // TODO 2：从 Movement 运行时数据捕获快照
        return false;
    }

    protected override void Apply(
        in ActorReplicationContext context,
        in MovementSnapshot payload)
    {
        // TODO 3：检查当前端是否允许应用
        // TODO 4：写入对应运行时对象
    }
}
```

完成标准：不修改 Replicator 的循环，只注册新 Channel，就能让它进入正确方向的统一数据包。

## 19. 常见错误速查

### Writer 写了数据，但另一端没有收到

Writer 不负责发送。检查是否调用了 RPC 或 Custom Messaging。

### Reader 读出错误值

检查字段类型和顺序是否与写入端完全一致。

### OverflowException

通常表示数据不足、长度字段错误或两端协议不一致。

### 某个 Channel 把后面的 Channel 也读坏了

检查 PayloadLength，以及反序列化后是否验证 `reader.Position == payloadEndPosition`。

### Host 正常，远程客户端失败

Host 可能绕过了真实网络路径。分别打印 `IsServer`、`IsClient`、`IsOwner`、方向和 Tick。

### 新 Channel 没被执行

检查是否创建并注册、ChannelId 是否重复、Direction 是否匹配、`TryWrite` 是否返回 `true`。

### 客户端可以提交服务器状态 Channel

除了 RPC 权限，还必须检查 Channel Direction，并在具体 Channel 的 `TryWrite/Apply` 再检查身份。

## 20. 当前项目仍需记住的限制

- `ToArray()` 每 Tick 产生托管数组分配。
- 当前没有显式协议版本号。
- 修改已有 Payload 字段需要两端同步更新。
- `StateEnterTick` 已发送，但客户端尚未用于恢复动画状态时间。
- 不可靠输入尚未携带历史冗余，关键 `Pressed` 事件可能因丢包遗漏。
- Input Tick 使用普通 `<=` 去重，暂未处理 `uint` 回绕。
- Input Channel 尚未拒绝未定义的按键 Flags，也尚未限制异常超前的未来 Tick。
- 手动快照不像 `NetworkVariable` 那样天然保存晚加入者状态；当前依靠服务器持续发送最新状态。

这些限制不影响理解第一版数据流，但进入实际网络质量测试前必须逐项评估。

## 21. 一句话复述

尝试不看代码复述下面这条链路：

```text
具体系统生成 TData
-> Channel 判断权限并捕获
-> Replicator 写入 ChannelId、长度和 Payload
-> RPC 发送 byte[]
-> 接收端 Reader 校验边界
-> Replicator 按 ChannelId 分发
-> Channel 验证并 Apply 到运行时数据
```

如果能明确说出每一步的发送者、接收者、数据所有者和失败条件，就已经掌握了当前手动组包系统的核心。

## 22. 对应代码索引

- [ActorReplicationContext.cs](../Assets/Scripts/Core/RunTimeData/Snapshot/ActorReplicationContext.cs)：不可变网络上下文和方向。
- [ActorReplicationChannel.cs](../Assets/Scripts/Core/RunTimeData/Snapshot/ActorReplicationChannel.cs)：统一基类、泛型序列化流程。
- [ActorSnapshotReplicator.cs](../Assets/Scripts/Core/RunTimeData/Snapshot/ActorSnapshotReplicator.cs)：注册、组包、读取和分发。
- [ActorInputReplicationChannel.cs](../Assets/Scripts/Core/RunTimeData/Snapshot/ActorInputReplicationChannel.cs)：Owner 到 Server 的实例。
- [ActorStateReplicationChannel.cs](../Assets/Scripts/Core/RunTimeData/Snapshot/ActorStateReplicationChannel.cs)：Server 到 Clients 的实例。
- [Actor.NetWorkState.cs](../Assets/Scripts/Core/Actor/Actor.NetWorkState.cs)：Writer/Reader 生命周期和 RPC 传输。
- [Actor.NetWorkTick.cs](../Assets/Scripts/Core/Actor/Actor.NetWorkTick.cs)：采集、上行、模拟和下行顺序。
