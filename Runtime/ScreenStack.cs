#nullable enable
using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;

namespace ScreenFlow
{
    public sealed class ScreenStack : IScreenStack
    {
        private readonly List<ScreenEntry> _stack = new();
        private readonly ScreenFactoryRegistry _registry = new();
        private UniTask _transitionQueue = UniTask.CompletedTask;

        public IScreen? Current => _stack.Count > 0 ? _stack[^1].Screen : null;
        public int Count => _stack.Count;

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

            for (var i = _stack.Count - 1; i >= 0; i--)
            {
                var entry = _stack[i];
                await StopRun(entry);
                await entry.Screen.Cleanup(cancellationToken);
            }
            _stack.Clear();
        }

        public UniTask Push<TTransition>(TTransition transition, CancellationToken cancellationToken)
            where TTransition : IScreenTransition
        {
            _transitionQueue = EnqueuePush(_transitionQueue, transition, cancellationToken).Preserve();
            return _transitionQueue;
        }

        public UniTask Pop(CancellationToken cancellationToken)
        {
            _transitionQueue = EnqueuePop(_transitionQueue, cancellationToken).Preserve();
            return _transitionQueue;
        }

        private async UniTask EnqueuePush<TTransition>(UniTask previous, TTransition transition, CancellationToken cancellationToken)
            where TTransition : IScreenTransition
        {
            try { await previous; } catch (Exception) { }

            if (Current is ISuspendableScreen suspendable)
                await suspendable.Suspend(cancellationToken);

            var screen = await _registry.Create(transition, cancellationToken);
            await screen.Enter(cancellationToken);

            var cts = new CancellationTokenSource();
            var runTask = screen.Run(cts.Token);
            _stack.Add(new ScreenEntry(screen, cts, runTask));
        }

        private async UniTask EnqueuePop(UniTask previous, CancellationToken cancellationToken)
        {
            try { await previous; } catch (Exception) { }

            if (_stack.Count == 0)
                throw new InvalidOperationException("Screen stack is empty.");

            var entry = _stack[^1];
            _stack.RemoveAt(_stack.Count - 1);

            await StopRun(entry);
            await entry.Screen.Exit(cancellationToken);
            await entry.Screen.Cleanup(cancellationToken);

            if (Current is ISuspendableScreen suspendable)
                await suspendable.Resume(cancellationToken);
        }

        private static async UniTask StopRun(ScreenEntry entry)
        {
            entry.Cts.Cancel();
            try { await entry.RunTask; } catch (OperationCanceledException) { }
            entry.Cts.Dispose();
        }

        private sealed class ScreenEntry
        {
            public readonly IScreen Screen;
            public readonly CancellationTokenSource Cts;
            public readonly UniTask RunTask;

            public ScreenEntry(IScreen screen, CancellationTokenSource cts, UniTask runTask)
            {
                Screen = screen;
                Cts = cts;
                RunTask = runTask;
            }
        }
    }
}