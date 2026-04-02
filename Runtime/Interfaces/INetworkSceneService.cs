using System;
using UnityEngine.SceneManagement;

namespace Emrgry.Network
{
    public interface INetworkSceneService
    {
        void LoadScene(string sceneName, LoadSceneMode mode = LoadSceneMode.Single);

        event Action<string> SceneLoadStarted;
        event Action<string> SceneLoadCompleted;
    }
}
