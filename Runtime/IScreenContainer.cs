#nullable enable
using System;

namespace ScreenFlow
{
    public interface IScreenContainer : IScreen
    {
        void Register<TTransition>(Func<TTransition, IScreen> factory)
            where TTransition : IScreenTransition;
    }

    public static class ScreenContainerExtensions
    {
        public static void Register<TTransition>(this IScreenContainer container, Func<IScreen> factory)
            where TTransition : IScreenTransition
        {
            container.Register<TTransition>(_ => factory());
        }

        public static void Register<TTransition, TScreen>(this IScreenContainer container)
            where TTransition : IScreenTransition, new()
            where TScreen : IScreen, new()
        {
            container.Register<TTransition>(_ => new TScreen());
        }
    }
}