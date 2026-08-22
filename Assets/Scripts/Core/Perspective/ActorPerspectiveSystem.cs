using System;

/// <summary>
/// ActorPerspectiveSystem 类实现了 IActorOwnershipSystem 接口，用于管理角色的视角系统。
/// 该系统负责处理第一人称和第三人称视角的切换、预测和同步。
/// </summary>
public sealed class ActorPerspectiveSystem : IActorOwnershipSystem
{
    // 私有字段，存储角色引用、状态切换器和复制器
    private readonly Actor actor;
    private readonly ActorPerspectiveStateSwitcher stateSwitcher;
    private readonly ActorPerspectiveReplication replication;

    // 标志位，表示系统是否已释放，是否有待处理的预测，以及待处理的输入刻度
    private bool isDisposed;
    private bool hasPendingPrediction;
    private uint pendingInputTick;

    // 属性，获取和设置权威模式和呈现模式，以及是否有待处理的预测
    public CameraPerspectiveMode AuthoritativeMode{get;private set;}
    public CameraPerspectiveMode PresentationMode{get;private set;}
    public bool HasPendingPrediction=>hasPendingPrediction;

    // 事件，当呈现模式改变时触发
    public event Action<CameraPerspectiveMode>PresentationModeChanged;

    /// <summary>
    /// 构造函数，初始化 ActorPerspectiveSystem 实例
    /// </summary>
    /// <param name="actor">要关联的 Actor 实例</param>
    public ActorPerspectiveSystem(Actor actor)
    {
        // 验证 actor 参数不为空
        this.actor=actor??throw new ArgumentNullException(nameof(actor));
        // 获取角色的脑部配置
        ActorBrainSo brain=actor.actorSO.actorBrainSO;

        // 确定初始视角模式
        CameraPerspectiveMode initialMode=brain!=null
            ?brain.InitialPerspectiveMode
            :CameraPerspectiveMode.ThirdPerson;
        // 验证初始模式有效性，无效则使用第三人称
        if(!ActorPerspectiveSnapshotUtility.IsValid(initialMode))
            initialMode=CameraPerspectiveMode.ThirdPerson;

        // 初始化状态切换器
        stateSwitcher=new ActorPerspectiveStateSwitcher(
            brain,
            actor.actorStateSystem);
        // 设置权威模式和呈现模式
        AuthoritativeMode=initialMode;
        PresentationMode=initialMode;
        // 应用到模拟系统
        actor.simulation.perspectiveMode=initialMode;

        // 初始化复制器
        replication=new ActorPerspectiveReplication(actor,initialMode);
        // 注册系统到 actor
        actor.RegisterSystem(this);
    }

    /// <summary>
    /// 所有者刻度处理方法，处理视角切换的输入
    /// </summary>
    /// <param name="inputTick">输入刻度</param>
    public void OwnerTick(uint inputTick)
    {
        // 如果系统已释放、不是所有者或未按下切换输入，则直接返回
        if(isDisposed||!actor.IsOwner||
           !actor.inputSystem.playerController.WasPressed(
               InputButtons.InputChange))
            return;

        // 获取下一个视角模式（第一人称和第三人称切换）
        CameraPerspectiveMode nextMode=GetOpposite(PresentationMode);
        // 提交切换意图
        replication.SubmitIntent(nextMode);

        // 检查是否可以切换到新模式并应用
        if(stateSwitcher.CanSwitchTo(nextMode)&&
           ApplyPresentationMode(nextMode,false))
        {
            hasPendingPrediction=true;
            pendingInputTick=inputTick;
        }
    }

    /// <summary>
    /// 服务器刻度处理方法，处理服务器端的视角状态同步
    /// </summary>
    public void ServerTick()
    {
        // 如果系统已释放或不是服务器，则直接返回
        if(isDisposed||!actor.IsServer)return;

        // 尝试获取待处理的意图
        if(!replication.TryConsumeIntent(out ActorPerspectiveRequest request))
            return;

        // 尝试应用权威模式
        TryApplyAuthoritativeMode(request.Mode);
        // 标记权威状态
        replication.MarkAuthoritativeState(
            AuthoritativeMode,
            request.InputTick);

        // 如果不是所有者，直接返回
        if(!actor.IsOwner)return;
        // 如果有待处理的预测且输入刻度小于待处理的输入刻度，则返回
        if(hasPendingPrediction&&request.InputTick<pendingInputTick)return;

        hasPendingPrediction=false;
        ApplyPresentationMode(AuthoritativeMode,false);
    }

    /// <summary>
    /// 呈现更新方法，处理客户端的视角状态更新
    /// </summary>
    public void PresentationUpdate()
    {
        // 如果系统已释放、是服务器或无法获取状态快照，则直接返回
        if(isDisposed||actor.IsServer||
           !replication.TryConsumeState(
               out ActorPerspectiveStateSnapshot snapshot))
            return;

        // 更新权威模式
        AuthoritativeMode=snapshot.Mode;

        // 如果是所有者且有待处理的预测
        if(actor.IsOwner&&hasPendingPrediction)
        {
            // 如果已处理的输入刻度小于待处理的输入刻度，则返回
            if(snapshot.ProcessedInputTick<pendingInputTick)return;
            hasPendingPrediction=false;
        }

        // 应用呈现模式
        ApplyPresentationMode(snapshot.Mode,false);
    }

    /// <summary>
    /// 获得所有权时的处理方法
    /// </summary>
    public void OnGainedOwnership()
    {
        // 如果系统已释放或不是所有者，则直接返回
        if(isDisposed||!actor.IsOwner)return;
        // 应用呈现模式
        ApplyPresentationMode(PresentationMode,true);
    }

    /// <summary>
    /// 失去所有权时的处理方法
    /// </summary>
    public void OnLostOwnership()
    {
        hasPendingPrediction=false;
        actor.viewVisibilityController?.SetFirstPersonHidden(false);
    }

    /// <summary>
    /// 释放资源方法
    /// </summary>
    public void Dispose()
    {
        // 如果系统已释放，则直接返回
        if(isDisposed)return;

        isDisposed=true;
        hasPendingPrediction=false;
        replication.Dispose();
        actor.viewVisibilityController?.SetFirstPersonHidden(false);
        PresentationModeChanged=null;
    }

    /// <summary>
    /// 尝试应用权威模式
    /// </summary>
    /// <param name="nextMode">下一个视角模式</param>
    /// <returns>是否成功应用</returns>
    private bool TryApplyAuthoritativeMode(CameraPerspectiveMode nextMode)
    {
        // 如果模式无效、与当前模式相同或无法切换，则返回 false
        if(!ActorPerspectiveSnapshotUtility.IsValid(nextMode)||
           nextMode==AuthoritativeMode||
           !stateSwitcher.TrySwitchTo(nextMode))
            return false;

        // 设置权威模式
        SetAuthoritativeMode(nextMode);
        return true;
    }

    /// <summary>
    /// 设置权威模式
    /// </summary>
    /// <param name="mode">视角模式</param>
    private void SetAuthoritativeMode(CameraPerspectiveMode mode)
    {
        AuthoritativeMode=mode;
    }

    private bool ApplyPresentationMode(
        CameraPerspectiveMode nextMode,
        bool force)
    {
        if(!ActorPerspectiveSnapshotUtility.IsValid(nextMode))return false;
        if(!force&&nextMode==PresentationMode)return true;

        if(actor.IsOwner)
        {
            //设置相机模式
            CameraPerspectiveMode previousMode=
                actor.cameraSystem.PerspectiveMode;
            if(!actor.cameraSystem.ApplyPerspectiveMode(nextMode))return false;


            //设置显示层级
            ActorViewVisibilityController visibility=
                actor.viewVisibilityController;
            bool hidden=nextMode==CameraPerspectiveMode.FirstPerson;
            if((hidden&&visibility==null)||
               (visibility!=null&&!visibility.SetFirstPersonHidden(hidden)))
            {
                actor.cameraSystem.ApplyPerspectiveMode(previousMode);
                return false;
            }
        }

        bool changed=nextMode!=PresentationMode;
        PresentationMode=nextMode;
        if(changed||force)
            PresentationModeChanged?.Invoke(nextMode);
        return true;
    }

    private static CameraPerspectiveMode GetOpposite(
        CameraPerspectiveMode mode)
    {
        return mode==CameraPerspectiveMode.FirstPerson
            ?CameraPerspectiveMode.ThirdPerson
            :CameraPerspectiveMode.FirstPerson;
    }
}
