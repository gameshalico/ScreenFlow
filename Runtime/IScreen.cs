using System.Threading;
using Cysharp.Threading.Tasks;

namespace ScreenFlow
{
    public interface IScreen
    {
        UniTask Initialize(CancellationToken cancellationToken);
        UniTask Enter(CancellationToken cancellationToken);
        UniTask Run(CancellationToken cancellationToken);
        UniTask Exit(CancellationToken cancellationToken);
        UniTask Cleanup(CancellationToken cancellationToken);
    }
}
