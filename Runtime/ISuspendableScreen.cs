using System.Threading;
using Cysharp.Threading.Tasks;

namespace ScreenFlow
{
    public interface ISuspendableScreen
    {
        UniTask Suspend(CancellationToken cancellationToken);
        UniTask Resume(CancellationToken cancellationToken);
    }
}
