using System;
using System.Collections.Generic;
using Emrgry.Core;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Emrgry.Network.Scene
{
    public sealed class NetworkSceneService : INetworkSceneService, IDisposable
    {
        private readonly NetworkManager _networkManager;
        private readonly IEventBus _eventBus;

        public event Action<string> SceneLoadStarted;
        public event Action<string> SceneLoadCompleted;

        public NetworkSceneService(NetworkManager networkManager, IEventBus eventBus)
        {
            _networkManager = networkManager;
            _eventBus = eventBus;

            if (_networkManager.SceneManager != null)
            {
                _networkManager.SceneManager.OnLoad += OnSceneLoad;
                _networkManager.SceneManager.OnLoadComplete += OnSceneLoadComplete;
                _networkManager.SceneManager.OnLoadEventCompleted += OnLoadEventCompleted;
            }
        }

        public void LoadScene(string sceneName, LoadSceneMode mode = LoadSceneMode.Single)
        {
            if (!_networkManager.IsServer)
            {
                Debug.LogWarning("[NetworkSceneService] Only the server can load scenes.");
                return;
            }

            var status = _networkManager.SceneManager.LoadScene(sceneName, mode);
            if (status != SceneEventProgressStatus.Started)
                Debug.LogError($"[NetworkSceneService] Failed to load scene {sceneName}. Status: {status}");
        }

        public void Dispose()
        {
            if (_networkManager?.SceneManager != null)
            {
                _networkManager.SceneManager.OnLoad -= OnSceneLoad;
                _networkManager.SceneManager.OnLoadComplete -= OnSceneLoadComplete;
                _networkManager.SceneManager.OnLoadEventCompleted -= OnLoadEventCompleted;
            }
        }

        private void OnSceneLoad(ulong clientId, string sceneName, LoadSceneMode mode, AsyncOperation operation)
        {
            SceneLoadStarted?.Invoke(sceneName);
            _eventBus.Publish(new SceneLoadProgressEvent(sceneName, 0f));
        }

        private void OnSceneLoadComplete(ulong clientId, string sceneName, LoadSceneMode mode)
        {
            SceneLoadCompleted?.Invoke(sceneName);
            _eventBus.Publish(new SceneLoadProgressEvent(sceneName, 1f));
        }

        private void OnLoadEventCompleted(string sceneName, LoadSceneMode loadSceneMode,
            List<ulong> clientsCompleted, List<ulong> clientsTimedOut)
        {
            _eventBus.Publish(new SceneLoadCompletedEvent(sceneName));
        }
    }
}
