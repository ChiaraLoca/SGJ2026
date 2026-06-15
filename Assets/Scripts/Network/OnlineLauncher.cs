using System;
using UnityEngine;
#if PHOTON_UNITY_NETWORKING
using System.Collections.Generic;
using ExitGames.Client.Photon;
using Photon.Pun;
using Photon.Realtime;
#endif

namespace FourE.Network
{
    /// <summary>
    /// Gestisce la connessione online e l'accoppiamento per codice stanza (PUN2), lato menu.
    /// Compila sempre: la logica Photon è attiva solo con PUN2 importato; senza, segnala il
    /// fallimento così il menu può ripiegare sull'hotseat. L'host crea la stanza (diventa
    /// MasterClient autoritativo), l'ospite la raggiunge col codice; a due giocatori l'host
    /// carica la scena di gioco e Photon la sincronizza anche sull'ospite.
    /// </summary>
    public sealed class OnlineLauncher : MonoBehaviour
#if PHOTON_UNITY_NETWORKING
        , IConnectionCallbacks, IMatchmakingCallbacks, IInRoomCallbacks
#endif
    {
        [Tooltip("Scena di selezione comandanti caricata (sincronizzata) quando la stanza è al completo.")]
        [SerializeField] private string _selectionSceneName = "CommanderSelectUI";

        /// <summary>Numero di giocatori per stanza in un 1v1.</summary>
        private const byte MaxPlayersPerRoom = 2;

        /// <summary>Testo di stato leggibile per la UI del menu (connessione, attesa, errore).</summary>
        public event Action<string> StatusChanged;

        /// <summary>Sollevato quando la procedura online fallisce in modo definitivo.</summary>
        public event Action Failed;

        private bool _isHost;
        private string _roomCode;

        /// <summary>
        /// Crea una stanza col codice indicato e attende l'avversario (diventa host autoritativo).
        /// </summary>
        /// <param name="roomCode">Codice stanza da condividere con l'avversario.</param>
        public void HostRoom(string roomCode)
        {
            _isHost = true;
            _roomCode = roomCode;
            BeginConnect();
        }

        /// <summary>
        /// Si unisce a una stanza esistente tramite il suo codice (diventa ospite).
        /// </summary>
        /// <param name="roomCode">Codice della stanza creata dall'host.</param>
        public void JoinExistingRoom(string roomCode)
        {
            _isHost = false;
            _roomCode = roomCode;
            BeginConnect();
        }

        /// <summary>Notifica un cambio di stato alla UI e lo logga.</summary>
        private void Report(string message)
        {
            StatusChanged?.Invoke(message);
        }

#if PHOTON_UNITY_NETWORKING
        /// <summary>Registra le callback Photon all'abilitazione.</summary>
        private void OnEnable()
        {
            PhotonNetwork.AddCallbackTarget(this);
        }

        /// <summary>Rimuove le callback Photon alla disabilitazione.</summary>
        private void OnDisable()
        {
            PhotonNetwork.RemoveCallbackTarget(this);
        }

        /// <summary>Avvia la connessione al cloud; se già connesso, va dritto alla stanza.</summary>
        private void BeginConnect()
        {
            PhotonNetwork.AutomaticallySyncScene = true;
            Report("Connessione…");

            if (PhotonNetwork.IsConnectedAndReady)
            {
                EnterRoom();
            }
            else
            {
                PhotonNetwork.ConnectUsingSettings();
            }
        }

        /// <summary>Crea o raggiunge la stanza in base al ruolo scelto.</summary>
        private void EnterRoom()
        {
            if (_isHost)
            {
                Report("Creazione stanza…");
                RoomOptions options = new() { MaxPlayers = MaxPlayersPerRoom };
                PhotonNetwork.CreateRoom(_roomCode, options);
            }
            else
            {
                Report("Ingresso nella stanza…");
                PhotonNetwork.JoinRoom(_roomCode);
            }
        }

        /// <summary>Se host e la stanza è al completo, carica la selezione comandanti (sincronizzata).</summary>
        private void TryStartWhenRoomFull()
        {
            if (PhotonNetwork.IsMasterClient && PhotonNetwork.CurrentRoom.PlayerCount >= MaxPlayersPerRoom)
            {
                Report("Avversario trovato. Selezione secchioni…");
                PhotonNetwork.LoadLevel(_selectionSceneName);
            }
        }

        /// <inheritdoc />
        public void OnConnectedToMaster()
        {
            EnterRoom();
        }

        /// <inheritdoc />
        public void OnJoinedRoom()
        {
            Report(_isHost ? "Stanza creata. In attesa dell'avversario…" : "Stanza raggiunta. Avvio…");
            TryStartWhenRoomFull();
        }

        /// <inheritdoc />
        public void OnPlayerEnteredRoom(Player newPlayer)
        {
            TryStartWhenRoomFull();
        }

        /// <inheritdoc />
        public void OnCreateRoomFailed(short returnCode, string message)
        {
            Debug.LogWarning($"CreateRoom failed ({returnCode}): {message}");
            Report("Impossibile creare la stanza.");
            Failed?.Invoke();
        }

        /// <inheritdoc />
        public void OnJoinRoomFailed(short returnCode, string message)
        {
            Debug.LogWarning($"JoinRoom failed ({returnCode}): {message}");
            Report("Stanza non trovata. Controlla il codice.");
            Failed?.Invoke();
        }

        /// <inheritdoc />
        public void OnDisconnected(DisconnectCause cause)
        {
            Debug.LogWarning($"Disconnected from Photon: {cause}");
            Report("Connessione persa.");
            Failed?.Invoke();
        }

        // --- Callback non utilizzate (richieste dalle interfacce) ---
        /// <inheritdoc />
        public void OnConnected() { }
        /// <inheritdoc />
        public void OnRegionListReceived(RegionHandler regionHandler) { }
        /// <inheritdoc />
        public void OnCustomAuthenticationResponse(Dictionary<string, object> data) { }
        /// <inheritdoc />
        public void OnCustomAuthenticationFailed(string debugMessage) { }
        /// <inheritdoc />
        public void OnFriendListUpdate(List<FriendInfo> friendList) { }
        /// <inheritdoc />
        public void OnCreatedRoom() { }
        /// <inheritdoc />
        public void OnJoinRandomFailed(short returnCode, string message) { }
        /// <inheritdoc />
        public void OnLeftRoom() { }
        /// <inheritdoc />
        public void OnPlayerLeftRoom(Player otherPlayer) { }
        /// <inheritdoc />
        public void OnRoomPropertiesUpdate(Hashtable propertiesThatChanged) { }
        /// <inheritdoc />
        public void OnPlayerPropertiesUpdate(Player targetPlayer, Hashtable changedProps) { }
        /// <inheritdoc />
        public void OnMasterClientSwitched(Player newMasterClient) { }
#else
        /// <summary>Senza PUN2 importato l'online non è disponibile: segnala il fallimento.</summary>
        private void BeginConnect()
        {
            Debug.LogWarning("Online mode requested but PUN2 is not installed (PHOTON_UNITY_NETWORKING undefined).");
            Report("Online non disponibile: PUN2 non installato.");
            Failed?.Invoke();
        }
#endif
    }
}
