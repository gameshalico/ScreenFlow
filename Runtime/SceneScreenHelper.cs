using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine.SceneManagement;

namespace ScreenFlow
{
    public static class SceneScreenHelper
    {
        public static async UniTask<Scene> Load(string sceneName, CancellationToken cancellationToken)
        {
            await SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Additive).ToUniTask(cancellationToken: cancellationToken);
            return SceneManager.GetSceneByName(sceneName);
        }

        public static UniTask Unload(Scene scene, CancellationToken cancellationToken)
        {
            return SceneManager.UnloadSceneAsync(scene).ToUniTask(cancellationToken: cancellationToken);
        }
    }
}
