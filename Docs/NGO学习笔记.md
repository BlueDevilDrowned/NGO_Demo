# Unity NGO 学习笔记

> 适用项目：当前 NGO 项目  
> Unity：6000.4.0f1  
> Netcode for GameObjects：2.13.1  
> 网络拓扑：Client-Server  
> 当前 TickRate：30

这份笔记不是 NGO 全部 API 的目录，而是围绕本项目要实现的角色同步，解释最常用的概念、API、执行顺序和设计边界。

进阶专题：[NGO 手动组包与序列化笔记](NGO手动组包与序列化笔记.md)

## 1. 先建立整体认识

联网游戏不是让所有机器共享同一份对象，而是：

- 服务器有一份游戏世界。
- 每个客户端也有一份自己的游戏世界副本。
- NGO 负责把需要同步的信息从一端传到另一端。
- 必须明确每一类数据由谁做最终决定，也就是“权威”。

本项目当前采用：

```text
拥有者客户端采集输入
        |
        | ActorInputCommand（RPC）
        v
服务器验证输入 -> 按 Tick 模拟角色 -> 修改服务器 RunTimeData / Transform
        |                              |
        | ActorStateSnapshot           | NetworkTransform
        v                              v
客户端更新表现数据              客户端平滑显示位置和旋转
        |
        v
客户端用 Animancer 播放动画
```

一句话概括：**客户端提交意图，服务器决定结果，客户端显示结果。**

这属于服务器权威的状态同步。客户端不需要把计算结果再发给服务器验证，也不是“客户端输入发给服务器，服务器再把输入发回所有客户端计算”。正确流程只有一次上行和一次结果下行：

```text
Owner -> 输入 -> Server -> 权威结果 -> Clients
```

## 2. Host、Server、Client 和 Dedicated Server

### Server

服务器负责权威模拟、生成网络对象、验证客户端请求、同步最终状态。

### Client

客户端连接服务器，采集本地玩家输入，并显示服务器同步过来的世界。

### Host

Host 是“服务器 + 一个本地客户端”运行在同一个进程中。因此 Host 上：

```csharp
IsServer == true;
IsClient == true;
IsHost == true;
```

这会带来一个常见误区：Host 测试正常，不代表远程客户端正常。因为 Host 发给服务器的 RPC 可能在本机立即执行，没有真实网络延迟。

### Dedicated Server

Dedicated Server 是只有服务器、没有本地玩家画面的独立服务器：

```csharp
IsServer == true;
IsClient == false;
IsHost == false;
```

本项目的方案可以同时用于 Host 和 Dedicated Server。不要把服务器逻辑写成 `if (IsHost)`，应当写成 `if (IsServer)`。

## 3. 三个最核心组件

### NetworkManager

`NetworkManager` 管理整个网络会话，包括：

- 启动 Host、Server 或 Client。
- 管理连接和客户端 ID。
- 管理网络 Tick。
- 注册 Network Prefab。
- 自动创建 PlayerObject。
- 管理 NGO 场景同步。

当前 `City` 场景的主要配置：

| 配置 | 当前值 | 含义 |
| --- | --- | --- |
| Tick Rate | 30 | 每秒 30 个网络 Tick，每 Tick 约 33.3 ms |
| Player Prefab | `Player0.prefab` | 客户端接入后自动创建的角色 |
| Auto Spawn Player Prefab | 开启 | 自动生成每个客户端的 PlayerObject |
| Scene Management | 开启 | 使用 NGO 管理联网场景同步 |
| Network Topology | Client-Server | 服务器是主要权威 |

常见启动 API：

```csharp
NetworkManager.Singleton.StartHost();
NetworkManager.Singleton.StartServer();
NetworkManager.Singleton.StartClient();
NetworkManager.Singleton.Shutdown();
```

### NetworkObject

`NetworkObject` 是“这个 GameObject 在网络中有身份”的标记。它提供：

- `NetworkObjectId`：本次网络会话中的对象 ID。
- `OwnerClientId`：该对象属于哪个客户端。
- Spawn、Despawn 和 Ownership。
- 让各端知道它们本地的哪个对象对应同一个网络对象。

RPC、`NetworkVariable`、`NetworkTransform` 都依赖 `NetworkObject`。

重要规则：

- 网络 Prefab 根节点通常只放一个 `NetworkObject`。
- Prefab 必须注册到 `NetworkManager` 使用的 Network Prefabs 列表。
- 动态网络对象通常只能由服务器 Spawn/Despawn。
- 不能直接通过 RPC 发送普通 `GameObject` 引用；使用 `NetworkObjectReference`。

服务器生成对象：

```csharp
GameObject instance = Instantiate(prefab, position, rotation);
instance.GetComponent<NetworkObject>().Spawn();
```

带所有者生成：

```csharp
networkObject.SpawnWithOwnership(clientId);
```

### NetworkBehaviour

`NetworkBehaviour` 继承自 `MonoBehaviour`，额外提供 NGO 生命周期、身份判断、RPC 和 `NetworkVariable` 能力。

```csharp
public partial class Actor : NetworkBehaviour
{
}
```

不是每个角色子系统都必须继承 `NetworkBehaviour`。本项目只让 `Actor` 作为网络集成入口是合理的：

- `Actor`：继承 `NetworkBehaviour`，处理 RPC、网络生命周期和网络 Tick。
- `NetWorkPlayerController`：普通 C# 类，只采集输入。
- `StateMachine`、`RunTimeData`：普通 C# 数据和逻辑对象。
- Animancer Facade：只负责动画表现。

普通类不能自己声明 NGO RPC，但可以把数据交给 `Actor`，由 `Actor` 发送。

## 4. 身份判断 API

| 属性 | 表示什么 | 本项目典型用途 |
| --- | --- | --- |
| `IsOwner` | 本机客户端是否拥有该 NetworkObject | 是否采集这个角色的本地输入 |
| `IsLocalPlayer` | 该对象是否是本机的 PlayerObject | 玩家专属相机、HUD 等 |
| `IsServer` | 当前进程是否运行服务器部分 | 权威模拟、验证、Spawn |
| `IsClient` | 当前进程是否运行客户端部分 | 画面、音效、客户端表现 |
| `IsHost` | 当前进程是否同时是 Server 和 Client | 只处理 Host 特例，不能代替 `IsServer` |
| `IsSpawned` | 该 NetworkObject 当前是否已网络生成 | 调用网络 API 前的保护 |

`IsOwner` 和 `IsLocalPlayer` 不完全相同。客户端可能拥有自己的 PlayerObject、宠物和投射物，此时它们都 `IsOwner == true`，但只有 PlayerObject 是 `IsLocalPlayer == true`。

本项目输入启用条件应当是：

```csharp
if (IsOwner)
{
    netWorkPlayerController.EnableInput();
}
```

服务器模拟条件应当是：

```csharp
if (IsServer)
{
    SimulateAuthoritativeState();
}
```

## 5. Ownership 和 Authority 不是一回事

### Ownership：对象归谁

PlayerObject 通常归对应的客户端，因此每个客户端都拥有自己的角色，而不是只有房主拥有全部角色。

### Authority：某类状态由谁最终决定

同一个角色可以同时采用：

- 输入采集：Owner 负责。
- 移动和战斗结果：Server 负责。
- Transform：Server Authority。
- 相机：Owner 本地负责，不需要同步。
- 动画显示：每个客户端根据同步状态本地播放。

因此，“只有权威端能改”不等于“只有房主能控制角色”。客户端控制的是输入意图；服务器替它执行权威移动。

需要把数据的“写权限”按类别拆开考虑，而不是给整个 Actor 只指定一种权威。

## 6. Unity 生命周期和 NGO 生命周期

### Awake

`Awake` 适合创建与网络身份无关的本地对象：

- 创建 `NetWorkPlayerController`。
- 创建 `RunTimeData`、状态机、注册状态。
- 初始化动画门面。

在 `Awake` 中不要依赖 `IsOwner`、`IsServer`、`OwnerClientId` 等网络身份，因为对象可能还没有 Spawn。

### OnNetworkSpawn

对象完成网络 Spawn 后调用。此时可以安全使用网络身份：

- 根据 `IsOwner` 启用输入。
- 根据 `IsServer` 初始化服务器数据。
- 注册 Network Tick。
- 订阅 `NetworkVariable.OnValueChanged`。

### OnNetworkDespawn

对象退出网络会话时调用。必须与 `OnNetworkSpawn` 对称清理：

```csharp
public override void OnNetworkSpawn()
{
    NetworkManager.NetworkTickSystem.Tick += OnNetworkTick;

    if (IsOwner)
    {
        netWorkPlayerController.EnableInput();
    }
}

public override void OnNetworkDespawn()
{
    if (NetworkManager != null)
    {
        NetworkManager.NetworkTickSystem.Tick -= OnNetworkTick;
    }

    netWorkPlayerController.DisableInput();
    base.OnNetworkDespawn();
}
```

如果不取消 Tick 或事件订阅，对象重生或重连后可能重复执行，也可能在对象销毁后继续收到回调。

### OnDestroy

处理本地对象的最终释放，例如 `Dispose()`。覆盖 `NetworkBehaviour.OnDestroy` 时要调用 `base.OnDestroy()`。

### 多个 NetworkBehaviour 的顺序

同一个 NetworkObject 上多个 `NetworkBehaviour.OnNetworkSpawn` 的顺序受 Inspector 中组件顺序影响。NGO 2.13.1 还提供 `OnNetworkPostSpawn`，它会在该对象所有 `OnNetworkSpawn` 执行完后调用。

本项目使用一个 `Actor` 配合 partial 文件，可以更直接地控制顺序：partial 只是把同一个类拆到不同文件，不会生成多个组件，也不会改变运行顺序。入口仍由 `Actor` 显式调用。

## 7. Tick 到底是什么

Tick 是网络模拟的离散步编号。当前 TickRate 为 30，含义是服务器理想情况下每秒处理 30 次网络模拟：

```text
1 秒 / 30 = 每 Tick 约 0.0333 秒
```

Tick 的价值不只是“每隔一段时间调用一次”，而是给输入和状态一个共同编号：

```text
输入 Tick 100 -> 服务器处理 -> 生成状态 Tick 100
输入 Tick 101 -> 服务器处理 -> 生成状态 Tick 101
```

这样可以：

- 丢弃重复或过旧输入。
- 知道服务器处理到了哪条输入。
- 对快照排序。
- 以后实现客户端预测与纠正。
- 让逻辑速率不直接依赖某台机器的渲染帧率。

### Tick 与 Update、FixedUpdate 的区别

| 回调 | 频率来源 | 适合 |
| --- | --- | --- |
| `Update` | 本机渲染帧率 | 输入采集、相机、普通表现 |
| `FixedUpdate` | Unity 物理步长 | Rigidbody 物理模拟 |
| Network Tick | NGO TickRate | 输入提交、网络模拟编号、状态采样 |

本项目建议：

- Input System 回调持续更新 `LocalInputData`。
- 每个网络 Tick 由 Owner 调用 `BuildCommand(tick)`，生成输入快照。
- 服务器消费输入并推进一次权威模拟。
- `Update` 用于动画和画面表现，不作为多人游戏结果的最终裁判。

NGO 2.13.1 的 `NetworkTime.Tick` 类型是 `int`。当前项目的 `ActorInputCommand.Tick` 使用 `uint`，因此构建命令时需要显式转换：

```csharp
uint tick = unchecked(
    (uint)NetworkManager.NetworkTickSystem.LocalTime.Tick);
```

服务器生成权威快照时应使用服务器时间语义，例如：

```csharp
uint tick = unchecked(
    (uint)NetworkManager.NetworkTickSystem.ServerTime.Tick);
```

也可以把项目协议中的 Tick 字段统一改为 `int`，直接匹配 NGO。无论选哪种类型，都应全项目保持一致；如果以后需要处理 Tick 整数回绕，不能只用普通的 `<=` 比较。

注意：TickRate 不是“网络包一定每秒收到 30 个”。延迟、丢包、批处理和主线程卡顿都会影响实际到达时间。

## 8. RPC 是什么

RPC（Remote Procedure Call）看起来像调用本地方法，但 NGO 会把参数序列化，发送给目标端，然后在目标端执行方法体。

NGO 2.x 推荐统一写法：

```csharp
[Rpc(SendTo.Server)]
private void SubmitInputRpc(ActorInputCommand command)
{
    // 这个方法体在服务器执行
}
```

规则：

- 方法必须位于 `NetworkBehaviour` 中。
- 方法需要 `[Rpc(...)]`。
- 方法名必须以 `Rpc` 结尾。
- 调用 RPC 时，发送端并不会执行“权威结果”，只是在请求目标端运行方法。
- RPC 参数必须是 NGO 支持序列化的类型。

常见目标：

| 目标 | 用途 |
| --- | --- |
| `SendTo.Server` | 客户端向服务器提交请求或输入 |
| `SendTo.Owner` | 服务器只通知对象拥有者 |
| `SendTo.NotServer` | 发给客户端，不包括 Host 的服务器身份 |
| `SendTo.ClientsAndHost` | 发给所有客户端表现端，包括 Host 客户端 |
| `SendTo.Everyone` | 包含本机和所有观察者，需注意本地立即调用 |

### 获取发送者并校验 Owner

不要相信客户端在参数里自报的 clientId。使用 NGO 提供的接收上下文：

```csharp
[Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Owner)]
private void SubmitInputRpc(
    ActorInputCommand command,
    RpcParams rpcParams = default)
{
    ulong senderId = rpcParams.Receive.SenderClientId;

    if (senderId != OwnerClientId)
    {
        return;
    }

    AcceptInputOnServer(command);
}
```

`InvokePermission.Owner` 限制只有当前对象 Owner 可以调用，但服务器仍应校验数据内容。

### Reliable 和 Unreliable

RPC 默认是 `Reliable`：保证送达，并保持同一网络对象上可靠 RPC 的发送顺序。代价是丢包后要重传，后面的包可能等待旧包，产生队头阻塞。

```csharp
[Rpc(SendTo.Server, Delivery = RpcDelivery.Unreliable)]
private void SubmitInputRpc(ActorInputCommand command)
{
}
```

`Unreliable`：允许丢失和乱序，但新数据不用等待旧包重传。

选择原则：

- 必须发生一次的低频操作，如购买、确认选择：Reliable。
- 高频且旧数据很快失去价值，如位置快照、瞄准方向：通常 Unreliable。
- “刚按下攻击”既是瞬时事件又不能随便丢，不能只把一个 `Pressed` 位放在单个不可靠包里。

本项目建议分阶段：

1. 第一版可先用 Reliable 输入 RPC 验证完整流程。
2. 不要长期保持“每 Tick 一个 Reliable RPC”的最终设计，差网络时可能积压旧输入。
3. 优化版使用 Unreliable 输入包，并在包中携带最近数个 Tick 的命令，服务器按 Tick 去重。
4. 或为关键按键建立序号/确认机制，在收到服务器确认前重复携带。

## 9. NetworkVariable 是什么

`NetworkVariable<T>` 用来同步“持续存在的当前状态”。它保存最新值，并会把当前值同步给晚加入的客户端。

```csharp
private readonly NetworkVariable<int> health = new(
    100,
    NetworkVariableReadPermission.Everyone,
    NetworkVariableWritePermission.Server);
```

服务器修改：

```csharp
if (IsServer)
{
    health.Value -= damage;
}
```

客户端监听：

```csharp
public override void OnNetworkSpawn()
{
    health.OnValueChanged += OnHealthChanged;
    OnHealthChanged(health.Value, health.Value); // 主动应用初始值
}

public override void OnNetworkDespawn()
{
    health.OnValueChanged -= OnHealthChanged;
    base.OnNetworkDespawn();
}

private void OnHealthChanged(int previous, int current)
{
    // 更新血条表现
}
```

### RPC 和 NetworkVariable 怎么选

问自己一个问题：**中途加入的玩家是否需要知道它？**

| 数据 | 推荐 | 原因 |
| --- | --- | --- |
| 当前血量 | NetworkVariable | 晚加入者需要当前值 |
| 门是否打开 | NetworkVariable | 是持续状态 |
| 一次爆炸特效 | RPC | 已经发生的瞬时事件不必补播 |
| 一次攻击输入 | RPC | 是客户端提交的瞬时命令 |
| 当前角色状态 ID | NetworkVariable 或状态快照 | 是持续状态 |
| Transform | NetworkTransform | 已有专用同步组件 |

`NetworkVariable` 是最终一致，不保证把两次网络发送之间的每一次中间赋值都传给客户端。比如同一 Tick 内从 10 改成 11 再改成 12，客户端可能只收到 12。需要每次都发生的事件应使用 RPC。

## 10. INetworkSerializable 做了什么

`INetworkSerializable` 只告诉 NGO “如何把这个类型写入网络缓冲区，以及如何读回来”。它不会自动发送数据。

当前项目中的 `ActorInputCommand`：

```csharp
public struct ActorInputCommand : INetworkSerializable
{
    public uint Tick;
    public Vector2 InputMove;
    public Vector2 InputLook;
    public InputButtons Held;
    public InputButtons Pressed;

    public void NetworkSerialize<T>(BufferSerializer<T> serializer)
        where T : IReaderWriter
    {
        serializer.SerializeValue(ref Tick);
        serializer.SerializeValue(ref InputMove);
        serializer.SerializeValue(ref InputLook);
        serializer.SerializeValue(ref Held);
        serializer.SerializeValue(ref Pressed);
    }
}
```

`BufferSerializer<T>` 在发送时是 Writer，在接收时是 Reader，同一段代码负责两个方向。字段顺序必须固定一致。

只有把这个结构用在 RPC、`NetworkVariable` 或自定义消息中时，序列化函数才会真正参与传输：

```csharp
SubmitInputRpc(command); // 这里才触发网络发送
```

所以：

```text
实现 INetworkSerializable != 数据已经同步
Serializable 属性          != 数据已经同步
public 字段                 != 数据已经同步
```

## 11. 为什么不能直接同步整个 RunTimeData 或“整个文件”

C# 文件只是源代码组织单位，运行时网络传输的是具体的值和消息，不存在“同步整个 `.cs` 文件”。

即便把 `RunTimeData` 全部做成可序列化，也不适合原样同步，原因包括：

- 其中可能逐渐包含状态机对象、组件引用、Animancer 状态等不可直接序列化内容。
- 很多字段是服务端中间计算值，客户端并不需要。
- 高频发送整个对象浪费带宽。
- 不同字段需要不同权限、频率和可靠性。
- 发送整个黑板容易让客户端和服务器同时写同一状态，权威边界变模糊。

正确做法是保留两层数据：

### RunTimeData：本地运行黑板

仍然供状态机、移动、战斗等系统读写。服务器上的这一份是权威黑板。它不会因为 `[Serializable]` 自动联网。

### Snapshot：网络协议数据

只挑客户端表现或预测真正需要的字段，组成紧凑结构，例如：

```csharp
public struct ActorStateSnapshot : INetworkSerializable
{
    public uint ServerTick;
    public uint LastProcessedInputTick;
    public ushort FullBodyStateId;
    public Vector3 Velocity;
    public byte Flags;

    public void NetworkSerialize<T>(BufferSerializer<T> serializer)
        where T : IReaderWriter
    {
        serializer.SerializeValue(ref ServerTick);
        serializer.SerializeValue(ref LastProcessedInputTick);
        serializer.SerializeValue(ref FullBodyStateId);
        serializer.SerializeValue(ref Velocity);
        serializer.SerializeValue(ref Flags);
    }
}
```

如果 Transform 已由 `NetworkTransform` 同步，第一版快照通常不必再次携带位置和旋转，避免重复带宽。未来做客户端预测和纠正时，再把权威位置加入专用校正快照。

## 12. NetworkTransform 为什么另一端可能没反应

`NetworkTransform` 只同步**权威端对 Transform 的修改**。普通客户端在服务器权威模式下直接执行：

```csharp
transform.position += movement;
```

只会改它自己的本地副本，服务器不会接受，也不会转发给别人；下一次服务器状态还可能把它纠正回去。

在 Client-Server 模式中，`NetworkTransform` 默认按服务器权威工作。与本项目设计一致的流程是：

```text
Owner 发送输入 -> Server 修改 Transform -> NetworkTransform 同步到客户端
```

排查时检查：

- 对象是否有 `NetworkObject` 且已经 Spawn。
- `NetworkTransform` 是否位于该 NetworkObject 或其有效子层级。
- Position/Rotation 对应轴是否勾选同步。
- 真正修改 Transform 的代码是否在 `IsServer` 分支执行。
- Prefab 是否在 Network Prefabs 列表中。
- 是否只用 Host 测试，而没有观察远程 Client。
- 是否同时有其他脚本、Animator Root Motion 或 CharacterController 覆盖 Transform。

`Interpolate` 用于非权威端平滑显示收到的状态，会带来少量显示延迟，但能减少跳帧感。它不会赋予客户端写权限，也不是客户端预测。

## 13. 动画、Animancer 和 NetworkAnimator

### 使用 Animancer 时 Animator 是否需要 Controller

Animancer 仍然使用 Unity 的 `Animator` 组件作为底层动画输出，但通常不需要为 `Animator.runtimeAnimatorController` 指定 Animator Controller。动画由 Animancer Graph 驱动。

### NetworkAnimator 能否防止状态机计算不一致

`NetworkAnimator` 是为 Mecanim Animator 参数和状态同步设计的，不会同步你的自定义状态机和 `RunTimeData`，也不会让两台机器上的任意逻辑自动变得确定一致。

本项目使用 Animancer，因此不需要为了同步而再套一层 `NetworkAnimator`。更合适的是同步动画所依赖的游戏语义：

- 状态 ID，例如 Idle、WalkStart、WalkLoop、WalkStop、Attack。
- 状态开始的 Server Tick。
- 速度、朝向或移动强度。
- 必要的动画变体，例如起步脚、攻击段数。
- 瞬时表现事件，例如命中特效、受击闪光。

客户端收到状态后，让自己的 Animancer 播放并平滑过渡。不要逐帧同步 Animancer 当前时间，也不要同步整个 Animancer Graph。

差网络下不应每收到一个包就把动画时间硬设置一次，否则才会不停跳帧。常见策略是：

- 状态未改变时，让本地动画自然播放。
- 状态改变时才切换或 CrossFade。
- 用 Server Tick 推算该状态已经播放了多久，只在首次进入或误差明显时校正。
- 对移动速度等连续参数做插值。
- 对攻击、受击等关键事件使用序号去重。

游戏逻辑由服务器状态机决定，动画是这个结果的表现。不要让“某个客户端动画播到末尾”成为服务器状态切换的唯一依据。

## 14. 本项目的数据职责

### NetWorkPlayerController

职责仅限本地 Input System：

- 保存当前按住状态。
- 记录这一段时间内刚按下的边沿事件。
- 按 Tick 生成 `ActorInputCommand`。
- 不继承 `MonoBehaviour` 或 `NetworkBehaviour`。
- 不发送 RPC，不判断服务器权限。

### ActorInputCommand

Owner 发给 Server 的网络协议：

- `Tick`：命令序号。
- `InputMove`、`InputLook`：连续输入。
- `Held`：当前仍然按住。
- `Pressed`：从上一个 BuildCommand 之后出现过的按下事件。

### RunTimeData

状态机的运行黑板：

- 服务器版本由服务器逻辑写，是权威数据源。
- 客户端版本可以保存同步来的表现数据，但不是权威判断来源。
- 它自身不负责发送、接收或验证。

### Actor

网络和生命周期集成根：

- 创建并持有各子系统。
- 在正确生命周期启用/停用输入。
- 注册并统一分发网络 Tick。
- 声明 RPC 和 NetworkVariable。
- 明确执行顺序。

按职责拆 partial 文件不会失去顺序控制：

```text
Actor.cs                 生命周期与总入口
Actor.NetworkInput.cs    输入构建、发送、接收和校验
Actor.Simulation.cs      服务器 Tick 模拟顺序
Actor.NetworkState.cs    快照生成、发送和客户端应用
```

执行顺序仍写在一个入口中：

```csharp
private void OnNetworkTick()
{
    if (IsOwner)
    {
        CaptureAndSubmitInput();
    }

    if (IsServer)
    {
        ConsumeInput();
        UpdateStateMachine();
        SimulateMovement();
        SimulateCombat();
        PublishState();
    }
}
```

Host 同时满足 `IsOwner` 和 `IsServer` 时，两段都会运行，这是预期行为。实现时可以为 Host 输入走本地队列，但必须让它经过与远程客户端相同的验证和模拟入口，避免 Host 获得额外规则。

## 15. 服务器应该验证什么

服务器权威不只是把客户端代码搬到服务器，还意味着不能直接相信输入。至少验证：

- RPC 发送者是否等于这个 Actor 的 `OwnerClientId`。
- Tick 是否比 `lastAcceptedInputTick` 新。
- Tick 是否离服务器时间过远，防止伪造未来命令。
- `InputMove`、`InputLook` 是否在允许范围内，必要时 Clamp。
- 按键 Flags 是否只包含定义过的位。
- 攻击、跳跃、交互是否满足冷却、资源和当前状态条件。
- 单位时间收到的命令数量是否异常。

示意：

```csharp
private bool TryValidateInput(
    ActorInputCommand command,
    ulong senderClientId)
{
    if (senderClientId != OwnerClientId)
    {
        return false;
    }

    if (command.Tick <= lastAcceptedInputTick)
    {
        return false;
    }

    command.InputMove = Vector2.ClampMagnitude(command.InputMove, 1f);
    command.InputLook = Vector2.ClampMagnitude(command.InputLook, 1f);
    return true;
}
```

实际实现不要只返回 `bool` 后丢掉 Clamp 后的局部副本；应让方法通过 `ref` 修改命令，或返回清洗后的命令。

## 16. 延迟、丢包和动画卡顿分别怎么处理

这些问题需要不同机制：

| 问题 | 机制 |
| --- | --- |
| 远端角色位置一格一格 | NetworkTransform 插值 |
| 本地按键后要等服务器才移动 | 客户端预测（后续功能） |
| 预测位置与服务器不一致 | 服务器校正 + Reconciliation |
| 丢了一条非关键快照 | 后续较新快照覆盖 |
| 丢了攻击按下事件 | 输入冗余、序号和确认 |
| 动画频繁重播或跳时间 | 只在状态变化时切换，连续参数插值 |
| Reliable 高频消息越来越慢 | 避免可靠队列积压，改用可恢复的不可靠协议 |

第一版不必立刻做预测。先让下面的服务器权威链路正确，即使本地有可感知延迟：

```text
输入上行 -> 服务器模拟 -> NetworkTransform / 状态下行 -> 客户端表现
```

之后再加入预测。预测是体验优化，不是权威模型的替代品。

## 17. 推荐实施顺序

### 第一阶段：跑通输入上行

- Owner 每 Tick 构建 `ActorInputCommand`。
- `Actor` 用 RPC 发送给 Server。
- Server 校验 Owner、Tick 和输入范围。
- Server 保存最新有效输入。
- 添加日志显示发送 Tick、接收 Tick 和 SenderClientId。

完成标准：远程 Client 按键时，服务器能稳定收到属于正确 Actor 的命令。

### 第二阶段：服务器权威模拟

- 只有 `IsServer` 推进权威状态机。
- 只有 Server 执行移动、战斗和有效性判断。
- Server 将有效输入写入服务器 `RunTimeData.Input`。
- `NetworkTransform` 同步服务器 Transform。
- 客户端状态机不能再各自独立决定权威状态。

完成标准：Client 不直接移动自己的 Transform，也能看到服务器控制它移动；其他 Client 同样看到结果。

### 第三阶段：状态快照与 Animancer 表现

- 定义最小 `ActorStateSnapshot`。
- 同步状态 ID、开始 Tick、速度和少量动画参数。
- 客户端把快照应用到表现层。
- 状态未改变时不重播动画。
- 用插值处理连续值。

完成标准：所有端动画语义一致，网络抖动时不会每个包都重置动画时间。

### 第四阶段：处理差网络

- 用 Multiplayer Play Mode 或网络模拟测试延迟、抖动和丢包。
- 输入协议改为不可靠 + 最近若干 Tick 冗余，或添加确认机制。
- 丢弃旧输入和旧快照。
- 测量 RTT、输入积压和服务器处理 Tick 差值。
- 调整 NetworkTransform 插值与阈值。

### 第五阶段：客户端预测（确实需要时）

- Owner 收到输入后立即做本地预测。
- 保存每个 Tick 的输入与预测结果。
- 服务器快照携带 `LastProcessedInputTick`。
- Owner 收到权威结果后回到服务器状态，重放尚未确认的输入。
- 非 Owner 仍然只做插值显示。

## 18. 常见错误速查

### “加了 INetworkSerializable，为什么没同步？”

它只定义读写方式。还需要 RPC、`NetworkVariable` 或自定义消息真正传输。

### “NetworkTransform 在另一端不动”

大概率修改发生在非权威客户端。服务器权威配置下必须让服务器改 Transform。

### “每个人都运行状态机，结果不是应该一样吗？”

不一定。输入到达时间、帧率、浮点、物理碰撞、事件顺序和丢包都会造成分歧。权威逻辑只在服务器决定，客户端运行的是表现或预测逻辑。

### “Owner 权威是不是只有房主能改？”

不是。每个 PlayerObject 可以属于各自客户端。房主同时是 Server 和一个 Client，只是其中一个 Owner。

### “NetworkVariable 可以代替所有 RPC 吗？”

不可以。它适合最新持续状态，不保证保留每次瞬时变化。

### “RPC 可以代替所有 NetworkVariable 吗？”

也不合适。晚加入者收不到已经发生过的 RPC，持久状态需要另行补发。

### “客户端收到输入再算，然后发回服务器验证吗？”

本项目不是这个流程。Owner 输入直接到 Server；Server 算出结果后发状态给 Clients，Clients 不把同一结果再发回去。

### “分 partial 文件会不会影响先后顺序？”

不会。编译后仍是同一个 `Actor` 类。顺序由 `OnNetworkTick` 中的显式调用决定。

### “为什么 Host 正常，Client 不正常？”

Host 的 RPC 可能本地立即执行，而且 Host 同时满足多个身份。必须用一个 Host/Server 加至少一个远程 Client 测试，并分别打印 `IsServer`、`IsClient`、`IsOwner` 和 `OwnerClientId`。

## 19. 调试清单

每次遇到“没同步”，按顺序检查：

1. NetworkManager 是否已经启动，客户端是否连接成功。
2. 对象是否有 `NetworkObject`，并且 `IsSpawned == true`。
3. Prefab 是否注册，所有端 Prefab 是否一致。
4. RPC 是否位于 `NetworkBehaviour`，带 `[Rpc]`，且方法名以 `Rpc` 结尾。
5. RPC 的发送目标和调用权限是否正确。
6. 日志中的 `SenderClientId` 是否等于对象 `OwnerClientId`。
7. 写状态的一端是否真的是权威端。
8. `NetworkVariable` 写权限是否允许当前端写。
9. 序列化字段顺序是否一致，数据类型是否受支持。
10. `OnNetworkSpawn` 是否订阅，`OnNetworkDespawn` 是否取消订阅。
11. 是否有 Animator、Root Motion、CharacterController 或其他脚本覆盖 Transform。
12. 是否只在 Host 上测试，掩盖了真实网络路径问题。

推荐日志格式：

```csharp
Debug.Log(
    $"[Actor Net] Object={NetworkObjectId} " +
    $"Owner={OwnerClientId} Local={NetworkManager.LocalClientId} " +
    $"Server={IsServer} Client={IsClient} IsOwner={IsOwner} " +
    $"Tick={command.Tick}");
```

## 20. 当前阶段最重要的结论

1. `NetWorkPlayerController` 只采集输入，保持普通 C# 类即可。
2. `Actor` 是网络集成入口，负责生命周期、Tick、RPC 和明确的执行顺序。
3. Owner 每 Tick 生成 `ActorInputCommand`，Server 接收、校验并模拟。
4. `RunTimeData` 继续是系统黑板，但不会自动同步；Server 的版本是权威源。
5. 不同步整个黑板，只定义最小 `ActorStateSnapshot`。
6. Transform 由 Server 修改，交给服务器权威 `NetworkTransform` 同步和插值。
7. Animancer 只做表现，客户端根据同步的状态语义播放，不同步整个动画图。
8. 第一目标是正确跑通服务器权威链路；预测、回滚和高级抗丢包协议随后再做。

## 21. 本地官方资料位置

本项目已安装的 NGO 2.13.1 自带文档位于：

```text
Library/PackageCache/com.unity.netcode.gameobjects@d43d28498f17/Documentation~
```

建议优先阅读：

```text
networking-concepts.md
terms-concepts/ownership.md
terms-concepts/authority.md
terms-concepts/client-server.md
components/core/networkobject.md
components/core/networkbehaviour.md
components/core/playerobjects.md
basics/networkvariable.md
advanced-topics/message-system/rpc.md
advanced-topics/message-system/reliability.md
advanced-topics/serialization/inetworkserializable.md
components/helper/networktransform.md
learn/ticks-and-update-rates.md
learn/rpcvnetvar.md
```

这些是当前项目实际安装版本的文档，API 写法比网上旧教程中的 `[ServerRpc]`、`[ClientRpc]` 更值得优先参考。
