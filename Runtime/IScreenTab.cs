#nullable enable
using System.Threading;
using Cysharp.Threading.Tasks;

namespace ScreenFlow
{
    public interface IScreenTab<TKey> : IScreenContainer where TKey : notnull
    {
        IScreen? Current { get; }

        UniTask Show<TTransition>(TKey key, TTransition transition, CancellationToken cancellationToken)
            where TTransition : IScreenTransition;

        UniTask Remove(TKey key, CancellationToken cancellationToken);
    }
}