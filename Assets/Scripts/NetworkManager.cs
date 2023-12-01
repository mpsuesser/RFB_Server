using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NetworkManager : MonoBehaviour
{
    private void Start()
    {
        QualitySettings.vSyncCount = 0;
        Application.targetFrameRate = 30;

        Server.Start(12, Constants.SERVER_LISTEN_PORT);
    }

    private void OnApplicationQuit() {
        Server.Stop();
    }
}
