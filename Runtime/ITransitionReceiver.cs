namespace ScreenFlow
{
    public interface ITransitionReceiver<in TTransition> where TTransition : IScreenTransition
    {
        void ApplyTransition(TTransition transition);
    }
}
