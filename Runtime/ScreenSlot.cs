#nullable enable
using System;
using System.Threading;
using Cysharp.Threading.Tasks;

namespace ScreenFlow
{
    public sealed class ScreenSlot : IScreenSlot
    {
        private readonly ScreenFactoryRegistry _registry = new();
        private UniTask _transitionQueue = UniTask.CompletedTask;
        private CancellationTokenSource? _runCts;
        private UniTask _runTask;

        public IScreen? Current { get; private set; }

        public void Register<TTransition>(Func<TTransition, IScreen> factory)
            where TTransition : IScreenTransition
        {
            _registry.Register(factory);
        }

        public UniTask Initialize(CancellationToken cancellationToken) => UniTask.CompletedTask;
        public UniTask Enter(CancellationToken cancellationToken) => UniTask.CompletedTask;
        public UniTask Run(CancellationToken cancellationToken) => UniTask.CompletedTask;
        public UniTask Exit(CancellationToken cancellationToken) => UniTask.CompletedTask;

        public async UniTask Cleanup(CancellationToken cancellationToken)
        {
            try { await _transitionQueue; } catch (Exception) { }

            if (Current != null)
            {
                await StopRun();
                await Current.Cleanup(cancellationToken);
                Current = null;
            }
        }

        public UniTask Show<TTransition>(TTransition transition, CancellationToken cancellationToken)
            where TTransition : IScreenTransition
        {
            _transitionQueue = EnqueueShow(_transitionQueue, transition, cancellationToken).Preserve();
            return _transitionQueue;
        }

        private async UniTask EnqueueShow<TTransition>(UniTask previous, TTransition transition, CancellationToken cancellationToken)
            where TTransition : IScreenTransition
        {
            try { await previous; } catch (Exception) { }

            if (Current != null)
            {
                await StopRun();
                await Current.Exit(cancellationToken);
                await Current.Cleanup(cancellationToken);
                Current = null;
            }

            var next = await _registry.Create(transition, cancellationToken);
            await next.Enter(cancellationToken);
            Current = next;

            _runCts = new CancellationTokenSource();
            _runTask = next.Run(_runCts.Token);
        }

        private async UniTask StopRun()
        {
            if (_runCts == null) return;
            _runCts.Cancel();
            try { await _runTask; } catch (OperationCanceledException) { }
            _runCts.Dispose();
            _runCts = null;
        }
    }
}