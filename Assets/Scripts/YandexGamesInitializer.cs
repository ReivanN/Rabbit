using UnityEngine;
using YG;

public class YandexGamesInitializer : MonoBehaviour
{
    void Awake()
    {
        DontDestroyOnLoad(this);
        YG2.StickyAdActivity(true);
        YG2.InterstitialAdvShow();
    }
}
