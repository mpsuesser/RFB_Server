using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Stats : MonoBehaviour
{
    public static Stats instance;
    void Awake() {
        if (instance != null) {
            Debug.Log("More than one Stats instance in scene!");
            return;
        }

        instance = this;
    }

    public enum Stat {
        MVP_POINTS = 0,
        CATCHES = 1,
        YARDS_GAINED,
        TOUCHDOWNS,
        INTERCEPTIONS,
        TACKLES,
        THROWS,
        COMPLETIONS,
        COMPLETION_PERCENTAGE,
        THROWING_YARDS,
        RUNNING_YARDS,
        TOUCHDOWNS_THROWN,
        INTERCEPTIONS_THROWN
    }

    private Dictionary<int, int[]> record = new Dictionary<int, int[]>();

    private void InitClient(int _clientId) {
        if (record.ContainsKey(_clientId)) {
            Debug.Log($"Removing stats record for client {_clientId} because another client with that ID has joined.");
            record.Remove(_clientId);
        }

        int[] initialValues = new int[] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };
        record.Add(_clientId, initialValues);
    }

    public void InitPlayers() {
        List<PlayerInfo> plist = Server.lobby.GetPlayerInfoList();
        foreach (PlayerInfo player in plist) {
            InitClient(player.id);
        }
    }

    public void AddToStat(int _clientId, Stat _stat, int _value) {
        if (!record.ContainsKey(_clientId)) {
            Debug.Log($"Could not find client {_clientId} in stats record.");
            return;
        }

        record[_clientId][(int)_stat] += _value;
    }

    public void DeductFromStat(int _clientId, Stat _stat, int _value) {
        if (!record.ContainsKey(_clientId)) {
            Debug.Log($"Could not find client {_clientId} in stats record.");
            return;
        }

        record[_clientId][(int)_stat] -= _value;
    }

    public void Clear() {
        Debug_DumpStats();

        record.Clear();
    }

    private void Debug_DumpStats() {
        Debug.Log("Dumping all stats recorded before clearing...");
        
        foreach (int client in record.Keys) {
            Debug.Log($"{client}: [ {string.Join(" ", record[client])} ]");
        }
    }
}
