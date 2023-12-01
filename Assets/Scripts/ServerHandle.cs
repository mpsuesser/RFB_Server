using System.Collections;
using System.Collections.Generic;
using Riptide;
using UnityEngine;

public class ServerHandle {
    [MessageHandler((ushort)ClientPackets.udpHandshakeRequest)]
    public static void UDPHandshakeRequest(int _fromClient, Packet _packet) {
        int _clientId = _packet.ReadInt();
        string _username = _packet.ReadString();
        string _version;

        try {
            _version = _packet.ReadString();
        } catch {
            return;
        }

        Debug.Log($"UDP handshake requested by {Server.clients[_fromClient].tcp.socket.Client.RemoteEndPoint}");

        if (_version != Constants.VERSION) {
            Debug.Log($"{_username} tried connecting with out-of-date version {_version}.");
            ServerSend.VersionOutOfDate(_fromClient);
            return;
        }

        if (!ConfirmClient(_fromClient, _clientId)) {
            Debug.Log($"{_username} failed the client confirmation step.");
            return;
        }

        if (Server.clients[_fromClient].udpHandshakeRequestReceived) {
            Debug.Log("Scrapping handshake request because already received one from this client.");
        }
        Server.clients[_fromClient].udpHandshakeRequestReceived = true;

        ServerSend.UDPHandshakeReceived(_fromClient, "Here is a UDP message to let you know I've received your UDP message.");
    }

    [MessageHandler((ushort)ClientPackets.requestToJoinLobby)]
    public static void RequestToJoinLobby(int _fromClient, Packet _packet) {
        int _clientId = _packet.ReadInt();
        string _username = _packet.ReadString();

        Debug.Log($"{Server.clients[_fromClient].tcp.socket.Client.RemoteEndPoint} is requesting to join lobby as player {_fromClient} with username {_username}.");

        // only one game going at a time, no lobbies while game is going on
        if (GameState.instance.gameStarted) {
            ServerSend.GameInProgress(_fromClient);
            return;
        }

        // register this new lobby entrant in our server's lobby registry
        int slot = Server.lobby.AddPlayerToLobby(_fromClient);
        if (slot == -1) {
            Debug.Log("Slot was == -1, this should not happen unless lobby is full. Returning.");
            ServerSend.GameInProgress(_fromClient);
            return;
        }

        // initialize Client's PlayerInfo with the slot and username
        Server.clients[_fromClient].InitializePlayerInfo(_username, slot);

        // Send WelcomeToLobby message to this client
        ServerSend.WelcomeToLobby(_fromClient);

        // Send UpdateLobby message to all other clients
        foreach (Client _client in Server.clients.Values) {
            if (_client.player != null) {
                if (_client.id != _fromClient) {
                    ServerSend.UpdateLobby(_client.id);
                }
            }
        }
    }

    [MessageHandler((ushort)ClientPackets.updateLobbySlot)]
    public static void UpdateLobbySlot(int _fromClient, Packet _packet) {
        int clientId = _packet.ReadInt();
        int slotRequested = _packet.ReadInt();

        if (!ConfirmClient(_fromClient, clientId)) {
            return;
        }

        if (GameState.instance.gameStarted) {
            Debug.Log("Got a request to update lobby slot when game was started. This should not happen.");

            return;
        }

        Server.lobby.MoveClientToSlot(clientId, slotRequested);
    }

    [MessageHandler((ushort)ClientPackets.beginGame)]
    public static void BeginGame(int _fromClient, Packet _packet) {
        int clientId = _packet.ReadInt();
        
        if (!ConfirmClient(_fromClient, clientId)) {
            return;
        }

        if (Server.lobby.GetPlayerInfoByClientId(clientId).username != "Arold") {
            Debug.Log("Someone other than Arold tried starting the game.");
            return;
        }

        if (GameState.instance.gameStarted) {
            Debug.Log("Got a request to start the game when the game was already started. This should not happen.");

            return;
        }

        GameMaster.instance.SignalGameStart();
    }

    [MessageHandler((ushort)ClientPackets.moveToIssued)]
    public static void MoveToIssued(int _fromClient, Packet _packet) {
        int clientId = _packet.ReadInt();
        int unitId = _packet.ReadInt();
        Vector3 dest = _packet.ReadVector3();
        bool shiftHeld = _packet.ReadBool();

        if (!ConfirmClient(_fromClient, clientId)) {
            return;
        }

        GameMaster.instance.MoveToIssued(clientId, unitId, dest, shiftHeld);
    }

    [MessageHandler((ushort)ClientPackets.unitRightClicked)]
    public static void UnitRightClicked(int _fromClient, Packet _packet) {
        int clientId = _packet.ReadInt();
        int clickerUnitId = _packet.ReadInt();
        int clickedUnitId = _packet.ReadInt();
        bool shiftHeld = _packet.ReadBool();

        if (!ConfirmClient(_fromClient, clientId)) {
            return;
        }

        GameMaster.instance.UnitRightClicked(clientId, clickerUnitId, clickedUnitId, shiftHeld);
    }

    [MessageHandler((ushort)ClientPackets.broadcastMessageToTeam)]
    public static void BroadcastMessageToTeam(int _fromClient, Packet _packet) {
        int clientId = _packet.ReadInt();
        string message = _packet.ReadString();

        if (!ConfirmClient(_fromClient, clientId)) {
            return;
        }

        int messagerSlotNumber = Server.lobby.GetSlotByClientId(clientId);
        if (messagerSlotNumber == -1) { // not found in player list
            return;
        }

        int team = messagerSlotNumber < 7 ? 1 : 2;
        ServerSend.BroadcastMessageToTeam(messagerSlotNumber, message, team);
    }

    
    [MessageHandler((ushort)ClientPackets.broadcastMessageToAll)]
    public static void BroadcastMessageToAll(int _fromClient, Packet _packet) {
        int clientId = _packet.ReadInt();
        string message = _packet.ReadString();

        if (!ConfirmClient(_fromClient, clientId)) {
            return;
        }

        int messagerSlotNumber = Server.lobby.GetSlotByClientId(clientId);
        if (messagerSlotNumber == -1) { // not found in player list
            return;
        }

        ServerSend.BroadcastMessageToAll(messagerSlotNumber, message);
    }

    
    [MessageHandler((ushort)ClientPackets.unitCharge)]
    public static void UnitCharge(int _fromClient, Packet _packet) {
        int clientId = _packet.ReadInt();
        int unitId = _packet.ReadInt();

        if (!ConfirmClient(_fromClient, clientId)) {
            return;
        }

        GameMaster.instance.UnitCharge(clientId, unitId);
    }

    [MessageHandler((ushort)ClientPackets.unitJuke)]
    public static void UnitJuke(int _fromClient, Packet _packet) {
        int clientId = _packet.ReadInt();
        int unitId = _packet.ReadInt();

        if (!ConfirmClient(_fromClient, clientId)) {
            return;
        }

        GameMaster.instance.UnitJuke(clientId, unitId);
    }

    
    [MessageHandler((ushort)ClientPackets.unitTackle)]
    public static void UnitTackle(int _fromClient, Packet _packet) {
        int clientId = _packet.ReadInt();
        int unitTackling = _packet.ReadInt();
        int unitBeingTackled = _packet.ReadInt();
        bool shiftHeld = _packet.ReadBool();

        if (!ConfirmClient(_fromClient, clientId)) {
            return;
        }

        GameMaster.instance.UnitTackle(clientId, unitTackling, unitBeingTackled, shiftHeld);
    }

    
    [MessageHandler((ushort)ClientPackets.unitStiff)]
    public static void UnitStiff(int _fromClient, Packet _packet) {
        int clientId = _packet.ReadInt();
        int unitStiffing = _packet.ReadInt();
        int unitBeingStiffed = _packet.ReadInt();
        bool shiftHeld = _packet.ReadBool();

        if (!ConfirmClient(_fromClient, clientId)) {
            return;
        }

        GameMaster.instance.UnitStiff(clientId, unitStiffing, unitBeingStiffed, shiftHeld);
    }

    
    [MessageHandler((ushort)ClientPackets.unitThrow)]
    public static void UnitThrow(int _fromClient, Packet _packet) {
        int clientId = _packet.ReadInt();
        int unitId = _packet.ReadInt();
        Vector3 destination = _packet.ReadVector3();

        if (!ConfirmClient(_fromClient, clientId)) {
            return;
        }

        GameMaster.instance.UnitThrow(clientId, unitId, destination);
    }

    
    [MessageHandler((ushort)ClientPackets.unitHike)]
    public static void UnitHike(int _fromClient, Packet _packet) {
        int clientId = _packet.ReadInt();
        int unitId = _packet.ReadInt();

        if (!ConfirmClient(_fromClient, clientId)) {
            return;
        }

        GameMaster.instance.UnitHike(clientId, unitId);
    }

    
    [MessageHandler((ushort)ClientPackets.unitStop)]
    public static void UnitStop(int _fromClient, Packet _packet) {
        int clientId = _packet.ReadInt();
        int unitId = _packet.ReadInt();

        if (!ConfirmClient(_fromClient, clientId)) {
            return;
        }

        GameMaster.instance.UnitStop(clientId, unitId);
    }

    #region Helper Functions
    public static bool ConfirmClient(int _fromClient, int _clientId) {
        if (_fromClient != _clientId) {
            Debug.Log($"Player ID {_fromClient} has assumed the wrong client ID: ({_clientId})!");

            return false;
        }

        return true;
    }
    #endregion
}
