using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ServerSend
{
    private static void SendTCPData(int _toClient, Packet _packet) {
        _packet.WriteLength();
        Server.clients[_toClient].tcp.SendData(_packet);
    }

    private static void SendUDPData(int _toClient, Packet _packet) {
        _packet.WriteLength();
        Server.clients[_toClient].udp.SendData(_packet);
    }

    private static void SendTCPDataToAll(Packet _packet) {
        _packet.WriteLength();
        for (int i = 1; i <= Server.MaxPlayers; i++) {
            Server.clients[i].tcp.SendData(_packet);
        }
    }

    private static void SendTCPDataToAll(int _exceptClient, Packet _packet) {
        _packet.WriteLength();
        for (int i = 1; i < Server.MaxPlayers; i++) {
            if (i != _exceptClient) {
                Server.clients[i].tcp.SendData(_packet);
            }
        }
    }

    private static void SendUDPDataToAll(Packet _packet) {
        _packet.WriteLength();
        for (int i = 1; i <= Server.MaxPlayers; i++) {
            // Debug.Log($"Calling client.udp.SendData to client {i}");
            Server.clients[i].udp.SendData(_packet);
        }
    }

    private static void SendUDPDataToAll(int _exceptClient, Packet _packet) {
        Debug.Log($"Except client specified as: {_exceptClient}");

        _packet.WriteLength();
        for (int i = 1; i < Server.MaxPlayers; i++) {
            if (i != _exceptClient) {
                // Debug.Log($"Sending udp data in except poly to client.udp.SendData {i}");
                Server.clients[i].udp.SendData(_packet);
            }
        }
    }

    public static void WelcomeToServer(int _toClient, string _msg) {
        using (Packet _packet = new Packet((int)ServerPackets.welcomeToServer)) {
            _packet.Write(_msg);
            _packet.Write(_toClient);

            Debug.Log("Sending WelcomeToServer");
            SendTCPData(_toClient, _packet);
        }
    }

    public static void UDPHandshakeReceived(int _toClient, string _msg) {
        CoroutineRunner coroutineRunner = GameObject.Find("CoroutineRunner").GetComponent<CoroutineRunner>();

        coroutineRunner.StartCoroutine(SendUDPHandshakeReceived(_toClient, _msg));
    }

    private static IEnumerator SendUDPHandshakeReceived(int _toClient, string _msg) {
        int count = 3;
        Debug.Log($"Sending {count} UDPHandshakeReceived packets.");

        while (count > 0) {
            using (Packet _packet = new Packet((int)ServerPackets.udpHandshakeReceived)) {
                _packet.Write(_msg);

                SendUDPData(_toClient, _packet);
            }

            count--;
            yield return new WaitForSeconds(0.1f);
        }
    }

    public static void WelcomeToLobby(int _toClient) {
        // gather current state of slots stored in Server.slots
        Dictionary<int, string> slotsState = new Dictionary<int, string>();
        for (int i = 0; i < Server.lobby.Slots.Length; i++) {
            int clientId = Server.lobby.Slots[i];
            if (clientId != 0) { // there is a client ID in this slot
                slotsState.Add(i + 1, Server.clients[clientId].Player.username);
            }
        }

        if (slotsState.Count < 1) {
            Debug.Log("When sending WelcomeToLobby message, slotsState count was 0. This should not happen.");
            return;
        }

        using (Packet _packet = new Packet((int)ServerPackets.welcomeToLobby)) {
            _packet.Write(slotsState.Count); // write total number of slots occupied

            foreach(int slot in slotsState.Keys) {
                _packet.Write(slot); // write slot number occupied
                _packet.Write(slotsState[slot]); // write username of client in that slot
            }

            SendTCPData(_toClient, _packet);
        }
    }

    public static void GameInProgress(int _toClient) {
        using (Packet _packet = new Packet((int)ServerPackets.gameInProgress)) {
            SendTCPData(_toClient, _packet);
        }
    }

    public static void VersionOutOfDate(int _toClient) {
        using (Packet _packet = new Packet((int)ServerPackets.versionOutOfDate)) {
            SendTCPData(_toClient, _packet);
        }
    }

    public static void UpdateLobby(int _toClient) {
        // gather current state of slots stored in Server.slots
        Dictionary<int, string> slotsState = new Dictionary<int, string>();
        for (int i = 0; i < Server.lobby.Slots.Length; i++) {
            int clientId = Server.lobby.Slots[i];
            if (clientId != 0) { // there is a client ID in this slot
                slotsState.Add(i + 1, Server.clients[clientId].Player.username);
            }
        }

        if (slotsState.Count < 1) {
            Debug.Log("When sending UpdateLobby message, slotsState count was 0. This should not happen.");
            return;
        }

        using (Packet _packet = new Packet((int)ServerPackets.updateLobby)) {
            _packet.Write(slotsState.Count); // write total number of slots occupied

            foreach (int slot in slotsState.Keys) {
                _packet.Write(slot); // write slot number occupied
                _packet.Write(slotsState[slot]); // write username of client in that slot
            }

            SendTCPData(_toClient, _packet);
        }
    }

    public static void UpdateLobbyToAll() {
        foreach (Client client in Server.clients.Values) {
            UpdateLobby(client.id);
        }
    }

    public static void PlayerDisconnected(int _toClient, int _clientId) {
        using (Packet _packet = new Packet((int)ServerPackets.playerDisconnected)) {
            _packet.Write(_clientId);

            SendTCPData(_toClient, _packet);
        }
    }

    public static void BroadcastMessageToTeam(int _fromSlot, string _msg, int _team) {
        using (Packet _packet = new Packet((int)ServerPackets.teamMessage)) {
            _packet.Write(_fromSlot);
            _packet.Write(_msg);

            List<PlayerInfo> playerList = Server.lobby.GetPlayerInfoList();
            foreach(PlayerInfo player in playerList) {
                Debug.Log($"Looking at client {player.id} in slot number {player.slotNumber}, team is {_team}");
                if ((player.slotNumber < 7 && _team == 1)
                    || (player.slotNumber >= 7 && _team == 2)) {
                    Debug.Log("Sending TCP data");
                    SendTCPData(player.id, _packet);
                }
            }
        }
    }

    public static void BroadcastMessageToAll(int _fromSlot, string _msg) {
        using (Packet _packet = new Packet((int)ServerPackets.allMessage)) {
            _packet.Write(_fromSlot);
            _packet.Write(_msg);

            SendTCPDataToAll(_packet);
        }
    }

    public static void UnitPositionUpdate(int _unit, Vector3 _pos) {
        using (Packet _packet = new Packet((int)ServerPackets.unitPositionUpdate)) {
            _packet.Write(_unit);
            _packet.Write(_pos);

            // Debug.Log($"Sending unit position update packet for unit #{_unit}");
            SendUDPDataToAll(_packet);
        }
    }

    public static void UnitRotationUpdate(int _unit, Quaternion _rotation) {
        using (Packet _packet = new Packet((int)ServerPackets.unitRotationUpdate)) {
            _packet.Write(_unit);
            _packet.Write(_rotation);

            SendUDPDataToAll(_packet);
        }
    }

    public static void GameStarting() {
        using (Packet _packet = new Packet((int)ServerPackets.gameStarting)) {


            SendTCPDataToAll(_packet);
        }
    }

    public static void GameState_Quarter(int _quarter) {
        using (Packet _packet = new Packet((int)ServerPackets.gsQuarter)) {
            _packet.Write(_quarter);

            SendTCPDataToAll(_packet);
        }
    }

    public static void GameState_StopClock(bool _stopClock) {
        using (Packet _packet = new Packet((int)ServerPackets.gsStopClock)) {
            _packet.Write(_stopClock);

            SendTCPDataToAll(_packet);
        }
    }

    public static void GameState_TimeLeftInQuarter(float _timeLeftInQuarter) {
        using (Packet _packet = new Packet((int)ServerPackets.gsTimeLeftInQuarter)) {
            _packet.Write(_timeLeftInQuarter);

            SendTCPDataToAll(_packet);
        }
    }

    public static void GameState_Score(int _redScore, int _greenScore) {
        using (Packet _packet = new Packet((int)ServerPackets.gsScore)) {
            _packet.Write(_redScore);
            _packet.Write(_greenScore);

            SendTCPDataToAll(_packet);
        }
    }

    public static void GameState_Possession(GameState.Team _possession) {
        using (Packet _packet = new Packet((int)ServerPackets.gsPossession)) {
            _packet.Write((int)_possession);

            SendTCPDataToAll(_packet);
        }
    }

    public static void GameState_Down(int _down) {
        using (Packet _packet = new Packet((int)ServerPackets.gsDown)) {
            _packet.Write(_down);

            SendTCPDataToAll(_packet);
        }
    }

    public static void GameState_FieldPosition(float _lineOfScrimmageLocation, float _firstDownLocation) {
        using (Packet _packet = new Packet((int)ServerPackets.gsFieldPosition)) {
            _packet.Write(_lineOfScrimmageLocation);
            _packet.Write(_firstDownLocation);

            SendTCPDataToAll(_packet);
        }
    }

    public static void NewSnapFormation(float _lineOfScrimmageLocation, float _firstDownLocation, int _team) {
        using (Packet _packet = new Packet((int)ServerPackets.newSnapFormation)) {
            _packet.Write(_lineOfScrimmageLocation);
            _packet.Write(_firstDownLocation);
            _packet.Write(_team);

            SendTCPDataToAll(_packet);
        }
    }

    public static void Hiked(int _unitId, Vector3 _origin, Vector3 _dest) {
        using (Packet _packet = new Packet((int)ServerPackets.hiked)) {
            _packet.Write(_unitId);
            _packet.Write(_origin);
            _packet.Write(_dest);

            SendTCPDataToAll(_packet);
        }
    }

    public static void TurnoverOnDowns() {
        using (Packet _packet = new Packet((int)ServerPackets.turnoverOnDowns)) {
            SendTCPDataToAll(_packet);
        }
    }

    public static void UnitCharged(int _unitId, float _cooldown, float _duration) {
        using (Packet _packet = new Packet((int)ServerPackets.unitCharged)) {
            _packet.Write(_unitId);
            _packet.Write(_cooldown);
            _packet.Write(_duration);

            SendTCPDataToAll(_packet);
        }
    }

    public static void UnitJuked(int _unitId, float _cooldown, float _duration) {
        using (Packet _packet = new Packet((int)ServerPackets.unitJuked)) {
            _packet.Write(_unitId);
            _packet.Write(_cooldown);
            _packet.Write(_duration);

            SendTCPDataToAll(_packet);
        }
    }

    public static void UnitTackled(int _unitTackling, int _unitBeingTackled, float _cooldown) {
        using (Packet _packet = new Packet((int)ServerPackets.unitTackled)) {
            _packet.Write(_unitTackling);
            _packet.Write(_unitBeingTackled);
            _packet.Write(_cooldown);

            SendTCPDataToAll(_packet);
        }
    }

    public static void UnitStiffed(int _unitStiffing, int _unitBeingStiffed, float _cooldown) {
        using (Packet _packet = new Packet((int)ServerPackets.unitStiffed)) {
            _packet.Write(_unitStiffing);
            _packet.Write(_unitBeingStiffed);
            _packet.Write(_cooldown);

            SendTCPDataToAll(_packet);
        }
    }

    public static void BallThrown(int _unitThrowingBall, Vector3 _origin, Vector3 _dest) {
        using (Packet _packet = new Packet((int)ServerPackets.ballThrown)) {
            _packet.Write(_unitThrowingBall);
            _packet.Write(_origin);
            _packet.Write(_dest);

            SendTCPDataToAll(_packet);
        }
    }

    public static void UpdateBall(Vector3 _position, Quaternion _rotation, int _frame) {
        using (Packet _packet = new Packet((int)ServerPackets.ballUpdate)) {
            _packet.Write(_position);
            _packet.Write(_rotation);
            _packet.Write(_frame);

            SendUDPDataToAll(_packet);
        }
    }

    public static void BallCaught(int _catcher, int _thrower) {
        using (Packet _packet = new Packet((int)ServerPackets.ballCaught)) {
            _packet.Write(_catcher);
            _packet.Write(_thrower);

            SendTCPDataToAll(_packet);
        }
    }

    public static void HikeCaught(int _unit) {
        using (Packet _packet = new Packet((int)ServerPackets.hikeCaught)) {
            _packet.Write(_unit);

            SendTCPDataToAll(_packet);
        }
    }

    public static void BallIncomplete(int _thrower, Vector3 _pos) {
        using (Packet _packet = new Packet((int)ServerPackets.ballIncomplete)) {
            _packet.Write(_thrower);
            _packet.Write(_pos);

            SendTCPDataToAll(_packet);
        }
    }

    public static void BallIntercepted(int _catcher, int _thrower) {
        using (Packet _packet = new Packet((int)ServerPackets.ballIntercepted)) {
            _packet.Write(_catcher);
            _packet.Write(_thrower);

            SendTCPDataToAll(_packet);
        }
    }

    public static void UnitCantThrow(int _unitId) {
        using (Packet _packet = new Packet((int)ServerPackets.unitCantThrow)) {
            _packet.Write(_unitId);

            SendTCPDataToAll(_packet);
        }
    }

    public static void UnitScoredTouchdown(int _unitId) {
        using (Packet _packet = new Packet((int)ServerPackets.unitScoredTouchdown)) {
            _packet.Write(_unitId);

            SendTCPDataToAll(_packet);
        }
    }

    public static void UnitTackledWithBall(int _tackledId, int _tacklerId) {
        using (Packet _packet = new Packet((int)ServerPackets.unitTackledWithBall)) {
            _packet.Write(_tackledId);
            _packet.Write(_tacklerId);

            SendTCPDataToAll(_packet);
        }
    }

    public static void UnitWentOutWithBall(int _unitId) {
        using (Packet _packet = new Packet((int)ServerPackets.unitWentOutWithBall)) {
            _packet.Write(_unitId);

            SendTCPDataToAll(_packet);
        }
    }

    public static void QuarterEnding() {
        using (Packet _packet = new Packet((int)ServerPackets.quarterEnding)) {
            SendTCPDataToAll(_packet);
        }
    }

    public static void Touchback(int _unitId) {
        using (Packet _packet = new Packet((int)ServerPackets.touchback)) {
            _packet.Write(_unitId);

            SendTCPDataToAll(_packet);
        }
    }

    public static void Safety(int _unitId) {
        using (Packet _packet = new Packet((int)ServerPackets.safety)) {
            _packet.Write(_unitId);

            SendTCPDataToAll(_packet);
        }
    }

    public static void FalseStart(int _unitId) {
        using (Packet _packet = new Packet((int)ServerPackets.falseStart)) {
            _packet.Write(_unitId);

            SendTCPDataToAll(_packet);
        }
    }
}
