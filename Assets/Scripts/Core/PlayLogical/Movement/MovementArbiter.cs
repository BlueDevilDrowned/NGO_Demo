using System;
using System.Collections.Generic;

/// <summary>
/// MovementArbiter 类是一个密封类，负责管理Actor的移动控制逻辑。
/// 它协调各种移动请求和控制请求，决定Actor的最终移动行为。
/// </summary>
/// 
/// //仲裁的是执行，所以请求还是会被传入，但是在处理请求这一步还是被仲裁器拦截了
/// 
public sealed class MovementArbiter
{
    // 持有对Actor的引用，表示这个移动仲裁器所控制的演员
    private readonly Actor actor;
    // 负责处理Actor的具体移动逻辑
    private readonly ActorMovement movement;
    // 存储来自不同请求者的移动控制请求
    private readonly Dictionary<object,MovementControlRequest> controlRequests=new();
    // 标记是否禁用了移动功能
    private bool movementDisabled;
    // 标记是否禁用了角色控制器
    private bool characterControllerDisabled;

    /// <summary>
    /// MovementArbiter类的构造函数，初始化移动系统
    /// </summary>
    /// <param name="actor">要控制的Actor对象</param>
    public MovementArbiter(Actor actor)
    {
        this.actor=actor!=null
            ?actor
            :throw new ArgumentNullException(nameof(actor)); // 确保actor不为null
        movement=new ActorMovement(actor);
        ResolveControlRequests(); // 初始化时解析控制请求
    }

    /// <summary>
    /// 获取重力模块
    /// </summary>
    public GraviteModule gravite=>movement.gravite;
    /// <summary>
    /// 获取移动是否启用的状态
    /// </summary>
    public bool IsMovementEnabled=>!movementDisabled;
    /// <summary>
    /// 获取角色控制器是否启用的状态
    /// </summary>
    public bool IsCharacterControllerEnabled=>!characterControllerDisabled;

    /// <summary>
    /// 提交移动请求
    /// </summary>
    /// <param name="request">移动请求对象</param>
    public void Submit(in MovementRequest request)
    {
        movement.Submit(in request);
    }

    /// <summary>
    /// 提交移动控制请求
    /// </summary>
    /// <param name="requester">请求者对象</param>
    /// <param name="request">控制请求对象</param>
    public void SubmitControlRequest(
        object requester,
        in MovementControlRequest request)
    {
        if(requester==null)
            throw new ArgumentNullException(nameof(requester)); // 确保请求者不为null

        controlRequests[requester]=request;
        ResolveControlRequests(); // 提交新请求后重新解析控制请求
    }

    /// <summary>
    /// 移除控制请求
    /// </summary>
    /// <param name="requester">请求者对象</param>
    /// <returns>是否成功移除请求</returns>
    public bool RemoveControlRequest(object requester)
    {
        if(requester==null)return false;

        bool removed=controlRequests.Remove(requester);
        if(removed)
            ResolveControlRequests(); // 移除请求后重新解析控制请求
        return removed;
    }

    /// <summary>
    /// 每帧调用的更新方法
    /// </summary>
    public void Tick()
    {
        ResolveControlRequests();
    }

    /// <summary>
    /// 开始帧处理，在移动启用时开始移动处理
    /// </summary>
    public void BeginTick()
    {
        Tick();
        if(IsMovementEnabled&&IsCharacterControllerEnabled)
            movement.BeginTick();
    }

    /// <summary>
    /// 执行移动逻辑，在移动和角色控制器都启用时执行移动
    /// </summary>
    public void Execute()
    {
        if(IsMovementEnabled&&IsCharacterControllerEnabled)
        {
            movement.Execute();
            return;
        }

        movement.ClearRequests(); // 如果移动被禁用，清除所有移动请求
    }

    /// <summary>
    /// 解析所有控制请求，更新移动和角色控制器的状态
    /// </summary>
    private void ResolveControlRequests()
    {
        bool disableMovement=false;
        bool disableCharacterController=false;

        // 遍历所有控制请求，确定是否需要禁用移动或角色控制器
        foreach(MovementControlRequest request in controlRequests.Values)
        {
            disableMovement|=request.DisableMovement;
            disableCharacterController|=request.DisableCharacterController;
        }

        // 如果之前移动未禁用但现在需要禁用，则停止移动
        if(!movementDisabled&&disableMovement)
            movement.Stop();

        // 更新移动和角色控制器的禁用状态
        movementDisabled=disableMovement;
        characterControllerDisabled=disableCharacterController;

        // 确保角色控制器的启用状态与禁用标记相反
        if(actor.characterController!=null&&
           actor.characterController.enabled==characterControllerDisabled)
            actor.characterController.enabled=!characterControllerDisabled;
    }
}
