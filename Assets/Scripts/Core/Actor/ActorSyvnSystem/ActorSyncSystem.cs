///设计思路
///数据的同步我设计了两块板子：权威数据板，只能由服务器修改；意图输入板，客户端写入意图用的
/// 游戏核心逻辑都是由服务器计算，所以服务器读写权威数据版
/// 而客户端只负责表现，对于权威板只读。客户端只能把数据写入意图板，再传给服务器，服务器同步数据，写入权威板，再同步给各个服务器
/// 
/// 模块细化思考：
/// 数据怎么流通？
///
public class ActorSyncSystem
{
    private Actor actor;
    public ActorSyncSystem(Actor actor)
    {
        this.actor=actor;
    }

    
}