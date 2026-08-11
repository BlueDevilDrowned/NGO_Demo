public sealed class UpperBodyEmptyState : UpperBodyState
{
    public UpperBodyEmptyState(Actor actor) : base(actor)
    {
    }

    public override void Enter()
    {
        animation.StopLayer(Layer);
        animation.SetLayerWeight(Layer,0f,0.1f);
    }
}
