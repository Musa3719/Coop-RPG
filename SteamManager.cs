using UnityEngine;
using Steamworks;

public class SteamManager : MonoBehaviour
{
    private void Awake()
    {
        if (GameObject.FindGameObjectsWithTag("SteamManager").Length > 1)
            Destroy(gameObject);
        else
            DontDestroyOnLoad(gameObject);
    }
    private void Start()
    {
        //SteamClient.Init(480, true);
    }
}
