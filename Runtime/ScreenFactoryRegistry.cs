using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;

namespace ScreenFlow
{
    public sealed class ScreenFactoryRegistry
    {
        private readonly Dictionary<Type, object> _factories = new();

        public void Register<TTransition>(Func<TTransition, IScreen> factory)
            where TTransition : IScreenTransition
        {
            _factories[typeof(TTransition)] = factory;
        }

        public void Register<TTransition>(Func<IScreen> factory)
            where TTransition : IScreenTransition
        {
            _factories[typeof(TTransition)] = new Func<TTransition, IScreen>(_ => factory());
        }

        public async UniTask<IScreen> Create<TTransition>(TTransition transition, CancellationToken cancellationToken)
            where TTransition : IScreenTransition
        {
            if (!_factories.TryGetValue(typeof(TTransition), out var factory))
                throw new InvalidOperationException(
                    $"No screen factory registered for transition type: {typeof(TTransition).Name}");

            var screen = ((Func<TTransition, IScreen>)factory)(transition);
            await screen.Initialize(cancellationToken);
            if (screen is ITransitionReceiver<TTransition> receiver)
                receiver.ApplyTransition(transition);
            return screen;
        }
    }
}
