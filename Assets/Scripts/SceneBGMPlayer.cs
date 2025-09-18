using UnityEngine;

public class SceneBGMPlayer : MonoBehaviour
{
    public AudioClip sceneBGM;

    void Start()
    {
        Debug.Log("test");
        if (AudioManager.Instance)
            AudioManager.Instance.PlayBGM(sceneBGM);
    }
}
