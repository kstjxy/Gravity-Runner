using System;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

public class BGMSceneRouter : MonoBehaviour
{
    [Serializable]
    public struct Entry { public string sceneName; public AudioClip clip; }

    [Header("Scene → BGM map")]
    public Entry[] map;

    void OnEnable() { SceneManager.sceneLoaded += OnSceneLoaded; }
    void OnDisable() { SceneManager.sceneLoaded -= OnSceneLoaded; }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (!AudioManager.Instance) return;
        var e = map.FirstOrDefault(x => x.sceneName == scene.name);
        if (e.clip != null) AudioManager.Instance.PlayBGM(e.clip);
    }
}
