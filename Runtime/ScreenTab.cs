#nullable enable
using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;

namespace ScreenFlow
{
    public sealed class ScreenTab<TKey> : IScreenTab<TKey> where TKey : notnull
    {
        private readonly ScreenFactoryRegistry _registry = new();
        private readonly Dictionary<TKey, TabEntry> _tabs = new();
        private UniTask _transitionQueue = UniTask.CompletedTask;
        private TKey? _currentKey;

        public IScreen? Current => _currentKey != null && _tabs.TryGetValue(_currentKey, out var entry)
            ? entry.Screen
            : null;

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

            if (_currentKey != null && _tabs.TryGetValue(_currentKey, out var current))
                await StopRun(current);

            foreach (var entry in _tabs.Values)
                await entry.Screen.Cleanup(cancellationToken);

            _tabs.Clear();
            _currentKey = default;
        }

        public UniTask Show<TTransition>(TKey key, TTransition transition,
            CancellationToken cancellationToken)
            where TTransition : IScreenTransition
        {
            _transitionQueue = EnqueueShow(_transitionQueue, key, transition, cancellationToken).Preserve();
            return _transitionQueue;
        }

        public UniTask Remove(TKey key, CancellationToken cancellationToken)
        {
            _transitionQueue = EnqueueRemove(_transitionQueue, key, cancellationToken).Preserve();
            return _transitionQueue;
        }

        private async UniTask EnqueueShow<TTransition>(UniTask previous, TKey key,
            TTransition transition, CancellationToken cancellationToken)
            where TTransition : IScreenTransition
        {
            try { await previous; } catch (Exception) { }

            if (_currentKey != null && _currentKey.Equals(key))
                return;

            if (_currentKey != null && _tabs.TryGetValue(_currentKey, out var current))
            {
                await StopRun(current);
                await current.Screen.Exit(cancellationToken);
            }

            if (!_tabs.TryGetValue(key, out var target))
            {
                var screen = await _registry.Create(transition, cancellationToken);
                target = new TabEntry(screen, null, UniTask.CompletedTask);
            }
            else
            {
                if (target.Screen is ITransitionReceiver<TTransition> receiver)
                    receiver.ApplyTransition(transition);
            }

            await target.Screen.Enter(cancellationToken);
            _currentKey = key;

            var cts = new CancellationTokenSource();
            var runTask = target.Screen.Run(cts.Token);
            _tabs[key] = new TabEntry(target.Screen, cts, runTask);
        }

        private async UniTask EnqueueRemove(UniTask previous, TKey key,
            CancellationToken cancellationToken)
        {
            try { await previous; } catch (Exception) { }

            if (!_tabs.TryGetValue(key, out var entry))
                throw new InvalidOperationException($"Tab '{key}' is not loaded.");

            if (_currentKey != null && _currentKey.Equals(key))
                throw new InvalidOperationException($"Cannot remove the current tab '{key}'.");

            await entry.Screen.Cleanup(cancellationToken);
            _tabs.Remove(key);
        }

        private static async UniTask StopRun(TabEntry entry)
        {
            if (entry.Cts == null) return;
            entry.Cts.Cancel();
            try { await entry.RunTask; } catch (OperationCanceledException) { }
            entry.Cts.Dispose();
        }

        private sealed class TabEntry
        {
            public readonly IScreen Screen;
            public readonly CancellationTokenSource? Cts;
            public readonly UniTask RunTask;

            public TabEntry(IScreen screen, CancellationTokenSource? cts, UniTask runTask)
            {
                Screen = screen;
                Cts = cts;
                RunTask = runTask;
            }
        }
    }
}