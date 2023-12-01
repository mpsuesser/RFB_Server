using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Lobby
{
    private static int[] slots; // the value of each element will be a client ID, or 0
    public int[] Slots {
        get {
            return slots;
        }
    }

    public int Count {
        get {
            int count = 0;
            for (int i = 0; i < slots.Length; i++) {
                if (slots[i] != 0) {
                    count++;
                }
            }

            return count;
        }
    }

    public Lobby() {
        slots = new int[12];
    }

    public int AddPlayerToLobby(int _clientId) {
        for (int i = 0; i < slots.Length; i++) {
            if (slots[i] == 0) {
                slots[i] = _clientId;

                return i + 1;
            }
        }

        return -1; // lobby full
    }

    private bool LobbySlotOccupied(int _slot) {
        return slots[_slot - 1] != 0;
    }

    public void MoveClientToSlot(int _clientId, int _slot) {
        if (_slot < 1) {
            Debug.Log("Slot provided to MoveClientToSlot must be greater than 0.");
            return;
        }

        if (LobbySlotOccupied(_slot)) {
            return;
        }

        for (int i = 0; i < slots.Length; i++) {
            if (slots[i] == _clientId) {
                slots[i] = 0;
                slots[_slot - 1] = _clientId;
                Server.clients[_clientId].player.slotNumber = _slot;

                ServerSend.UpdateLobbyToAll();
                return;
            }
        }

        Debug.Log("That client wasn't in the lobby to begin with! This shouldn't happen.");
        return;
    }

    public void RemoveClientFromLobby(int _clientId) {
        for (int i = 0; i < slots.Length; i++) {
            if (slots[i] == _clientId) {
                slots[i] = 0;
            }
        }

        if (Count > 0) {
            ServerSend.UpdateLobbyToAll();
        } else {
            if (GameState.instance.gameStarted) {
                GameMaster.instance.SignalKillGame();
            }
        }
    }


    #region Helpers
    public int GetSlotByClientId(int _clientId) {
        for (int i = 0; i < slots.Length; i++) {
            if (slots[i] == _clientId) {
                return i + 1;
            }
        }

        return -1;
    }

    public PlayerInfo GetPlayerInfoByClientId(int _clientId) {
        for (int i = 0; i < slots.Length; i++) {
            if (slots[i] == _clientId) {
                return Server.clients[_clientId].Player;
            }
        }

        return null;
    }

    public int GetClientIdForSlot(int _slotNumber) {
        if (slots[_slotNumber-1] != 0) {
            return slots[_slotNumber - 1];
        }

        return -1;
    }

    public List<PlayerInfo> GetPlayerInfoList() {
        List<PlayerInfo> plist = new List<PlayerInfo>();
        for (int i = 0; i < slots.Length; i++) {
            if (slots[i] != 0) {
                plist.Add(Server.clients[slots[i]].Player);
            }
        }

        return plist;
    }
    #endregion
}
