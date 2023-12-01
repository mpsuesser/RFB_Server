using System;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using UnityEngine;

public class Server
{
    public static int MaxPlayers { get; private set; }
    public static int Port { get; private set; }
    public static Dictionary<int, Client> clients = new Dictionary<int, Client>();
    public delegate void PacketHandler(int _fromClient, Packet _packet);
    public static Dictionary<int, PacketHandler> packetHandlers;

    // lobby
    public static Lobby lobby;

    private static TcpListener tcpListener;
    private static UdpClient udpListener;

    public static void Start(int _maxPlayers, int _port) {
        MaxPlayers = _maxPlayers;
        Port = _port;

        Debug.Log("Starting server...");
        InitializeServerData();

        tcpListener = new TcpListener(IPAddress.Any, Port);
        tcpListener.Start();
        tcpListener.BeginAcceptTcpClient(new AsyncCallback(TCPConnectCallback), null);

        udpListener = new UdpClient(Port);
        udpListener.BeginReceive(UDPReceiveCallback, null);

        Debug.Log($"Server started on {Port}.");
    }

    public static void Stop() {
        tcpListener.Stop();
        udpListener.Close();
    }

    private static void TCPConnectCallback(IAsyncResult _result) {
        TcpClient _client = tcpListener.EndAcceptTcpClient(_result);
        tcpListener.BeginAcceptTcpClient(new AsyncCallback(TCPConnectCallback), null);
        Debug.Log($"Incoming connection from {_client.Client.RemoteEndPoint}...");

        for (int i = 1; i <= MaxPlayers; i++) {
            if (clients[i].tcp.socket == null) {
                clients[i].tcp.Connect(_client);
                return;
            }
        }

        Debug.Log($"{_client.Client.RemoteEndPoint} failed to connect: Server full!");
    }

    private static void UDPReceiveCallback(IAsyncResult _result) {
        try {
            IPEndPoint _clientEndPoint = new IPEndPoint(IPAddress.Any, 0);
            byte[] _data = udpListener.EndReceive(_result, ref _clientEndPoint);
            udpListener.BeginReceive(UDPReceiveCallback, null);

            if (_data.Length < 4) {
                return;
            }

            using (Packet _packet = new Packet(_data)) {
                int _clientId = _packet.ReadInt();

                if (_clientId == 0) {
                    return;
                }

                if (clients[_clientId].udp.endPoint == null) {
                    clients[_clientId].udp.Connect(_clientEndPoint);
                    return;
                }

                if (clients[_clientId].udp.endPoint.ToString() == _clientEndPoint.ToString()) {
                    clients[_clientId].udp.HandleData(_packet);
                }
            }
        } catch (Exception _ex) {
            Debug.Log($"Error receiving UDP data: {_ex}");
        }
    }

    public static void SendUDPData(IPEndPoint _clientEndPoint, Packet _packet) {
        try {
            if (_clientEndPoint != null) {
                // Debug.Log($"Sending UDP data to client at endpoint {_clientEndPoint}");
                // TODO: check if callback is even touched when the packet fails to send
                udpListener.BeginSend(_packet.ToArray(), _packet.Length(), _clientEndPoint,
                    (asyncResult) => {
                        try {
                            int bytes = udpListener.EndSend(asyncResult);
                            if (bytes == 0) {
                                Debug.Log("[UDP SEND] 0 bytes were sent!");
                            }
                        } catch (SocketException e) {
                            Debug.Log("[SOCKET EXCEPTION]");
                            Debug.Log(e);
                        } catch (Exception e) {
                            Debug.Log("[GENERAL EXCEPTION]");
                            Debug.Log(e);
                        }
                    }, null);
            } else {
                // Debug.Log("Client endpoint was null.");
            }
        } catch (Exception _ex) {
            Debug.Log($"Error sending data to {_clientEndPoint} via UDP: {_ex}");
        }
    }

    private static void InitializeServerData() {
        for (int i = 1; i <= MaxPlayers; i++) {
            clients.Add(i, new Client(i));
        }

        lobby = new Lobby();

        packetHandlers = new Dictionary<int, PacketHandler>() {
            { (int)ClientPackets.udpHandshakeRequest, ServerHandle.UDPHandshakeRequest },
            { (int)ClientPackets.requestToJoinLobby, ServerHandle.RequestToJoinLobby },
            { (int)ClientPackets.updateLobbySlot, ServerHandle.UpdateLobbySlot },
            { (int)ClientPackets.beginGame, ServerHandle.BeginGame },
            { (int)ClientPackets.moveToIssued, ServerHandle.MoveToIssued },
            { (int)ClientPackets.unitRightClicked, ServerHandle.UnitRightClicked },
            { (int)ClientPackets.broadcastMessageToTeam, ServerHandle.BroadcastMessageToTeam },
            { (int)ClientPackets.broadcastMessageToAll, ServerHandle.BroadcastMessageToAll },
            { (int)ClientPackets.unitCharge, ServerHandle.UnitCharge },
            { (int)ClientPackets.unitJuke, ServerHandle.UnitJuke },
            { (int)ClientPackets.unitTackle, ServerHandle.UnitTackle },
            { (int)ClientPackets.unitStiff, ServerHandle.UnitStiff },
            { (int)ClientPackets.unitThrow, ServerHandle.UnitThrow },
            { (int)ClientPackets.unitHike, ServerHandle.UnitHike },
            { (int)ClientPackets.unitStop, ServerHandle.UnitStop }
        };

        Debug.Log("Initialized packets.");
    }
}
