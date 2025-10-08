using UnityEngine;
using YG;

public class YandexGamesInitializer : MonoBehaviour
{
    void Awake()
    {
        YG2.StickyAdActivity(true);
    }
}
