#nullable enable
using System.Threading;
using Cysharp.Threading.Tasks;

namespace ScreenFlow
{
    public interface IScreenSlot : IScreenContainer
    {
        IScreen? Current { get; }

        UniTask Show<TTransition>(TTransition transition, CancellationToken cancellationToken)
            where TTransition : IScreenTransition;
    }

    public static class ScreenSlotExtensions
    {
        public static async UniTask Show<TTransition>(this IScreenSlot slot, CancellationToken cancellationToken)
            where TTransition : IScreenTransition, new()
        {
            await slot.Show(new TTransition(), cancellationToken);
        }
    }
}