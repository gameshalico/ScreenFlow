#nullable enable
using System.Threading;
using Cysharp.Threading.Tasks;

namespace ScreenFlow
{
    public interface IScreenStack : IScreenContainer
    {
        IScreen? Current { get; }
        int Count { get; }

        UniTask Push<TTransition>(TTransition transition, CancellationToken cancellationToken)
            where TTransition : IScreenTransition;

        UniTask Pop(CancellationToken cancellationToken);
    }

    public static class ScreenStackExtensions
    {
        public static async UniTask Push<TTransition>(this IScreenStack stack, CancellationToken cancellationToken)
            where TTransition : IScreenTransition, new()
        {
            await stack.Push(new TTransition(), cancellationToken);
        }
    }
}