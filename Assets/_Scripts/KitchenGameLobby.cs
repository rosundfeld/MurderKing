using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;

using UnityEngine;
using Unity.Services.Core;
using Unity.Services.Authentication;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;

using UnityEngine.SceneManagement;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Networking.Transport.Relay;
using System;

public class KitchenGameLobby : MonoBehaviour
{

    private const string KEY_RELAY_JOIN_CODE = "RelayJoinCode";

    public static KitchenGameLobby Instance { get; private set; }

    private float heartbeatTimer;
    private float listLobbiesTimer;
    private Lobby joinedLobby;

    public event EventHandler OnCreateLobbyStarted;
    public event EventHandler OnCreateLobbyFailed;
    public event EventHandler OnQuickJoinLobbyFailed;
    public event EventHandler OnJoinStarted;
    public event EventHandler OnJoinFailed;
    public event EventHandler<OnLobbyListChangedEventArgs> OnLobbyListChanged;
    public class OnLobbyListChangedEventArgs : EventArgs
    {
        public List<Lobby> lobbyList;
    }

    private void Awake()
    {
        Instance = this;

        DontDestroyOnLoad(gameObject);

        InitializeUnityAuthentication();
    }

    private async void InitializeUnityAuthentication()
    {
        if (UnityServices.State != ServicesInitializationState.Initialized)
        {
            InitializationOptions initializationOptions = new InitializationOptions();
            //initializationOptions.SetProfile(UnityEngine.Random.Range(0, 10000).ToString());

            await UnityServices.InitializeAsync(initializationOptions);
            await AuthenticationService.Instance.SignInAnonymouslyAsync();
        }
    }

    private async Task<Allocation> AllocateRelay() //Tasks sao necessárias para retornos assíncronos em C#
    {
        try
        {
            Allocation allocation = await RelayService.Instance.CreateAllocationAsync(GameManagerMultiplayer.MAX_PLAYER_AMOUNT - 1);
            return allocation;
        }
        catch (RelayServiceException e)
        {
            Debug.LogError($"Failed to allocate relay: {e.Message}");
            return default;
        }
    }

    private async Task<string> GetRelayJoinCode(Allocation allocation)
    {
        try
        {
            string relayJoinCode = await RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);
            return relayJoinCode;
        }
        catch (RelayServiceException e)
        {
            Debug.LogError($"Failed to get relay join code: {e.Message}");
            return default;
        }
    }

    private async Task<JoinAllocation> JoinRelay(string relayJoinCode)
    {
        try
        {
            JoinAllocation joinAllocation = await RelayService.Instance.JoinAllocationAsync(relayJoinCode);
            return joinAllocation;
        }
        catch (RelayServiceException e)
        {
            Debug.LogError($"Failed to join relay: {e.Message}");
            return default;
        }
    }

    public async void CreateLobby(string lobbyName, bool isPrivate)
    {
        OnCreateLobbyStarted?.Invoke(this, EventArgs.Empty);
        try
        {
            joinedLobby = await LobbyService.Instance.CreateLobbyAsync(lobbyName, GameManagerMultiplayer.MAX_PLAYER_AMOUNT, new CreateLobbyOptions
            {
                IsPrivate = isPrivate
            });

            Allocation allocation = await AllocateRelay();

            string relayJoinCode = await GetRelayJoinCode(allocation);

            await LobbyService.Instance.UpdateLobbyAsync(joinedLobby.Id, new UpdateLobbyOptions
            {
                Data = new Dictionary<string, DataObject>
                {
                    { KEY_RELAY_JOIN_CODE, new DataObject(DataObject.VisibilityOptions.Member, relayJoinCode) }
                },

            });
            NetworkManager.Singleton.GetComponent<UnityTransport>().SetRelayServerData(new RelayServerData(allocation, "dtls"));

            GameManagerMultiplayer.Instance.StartHost();
            Loader.LoadNetwork(Loader.Scene.CharacterSelectionScene);
        }
        catch (LobbyServiceException e)
        {
            Debug.LogError($"Failed to create lobby: {e.Message}");
            OnCreateLobbyFailed?.Invoke(this, EventArgs.Empty);
        }
    }

    public async void QuickJoinLobby()
    {
        OnJoinStarted?.Invoke(this, EventArgs.Empty);
        try
        {
            joinedLobby = await LobbyService.Instance.QuickJoinLobbyAsync();

            string relayJoinCode = joinedLobby.Data[KEY_RELAY_JOIN_CODE].Value;

            JoinAllocation joinAllocation = await JoinRelay(relayJoinCode);

            NetworkManager.Singleton.GetComponent<UnityTransport>().SetRelayServerData(new RelayServerData(joinAllocation, "dtls"));
            GameManagerMultiplayer.Instance.StartClient();
        }
        catch (LobbyServiceException e)
        {
            Debug.LogError($"Failed to quick join lobby: {e.Message}");
            OnQuickJoinLobbyFailed?.Invoke(this, EventArgs.Empty);
        }
    }

    private async void ListLobbies()
    {
        try
        {
            QueryLobbiesOptions queryOptions = new QueryLobbiesOptions
            {
                Filters = new List<QueryFilter>
                {
                    new QueryFilter(QueryFilter.FieldOptions.AvailableSlots, "0", QueryFilter.OpOptions.GT)
                }
            };
            QueryResponse queryResponse = await LobbyService.Instance.QueryLobbiesAsync(queryOptions);
            OnLobbyListChanged?.Invoke(this, new OnLobbyListChangedEventArgs { lobbyList = queryResponse.Results });
        }
        catch (LobbyServiceException e)
        {
            Debug.LogError($"Failed to list lobbies: {e.Message}");
        }
    }

    public Lobby GetLobby()
    {
        return joinedLobby;
    }

    public async void JoinWithCode(string lobbyCode)
    {
        OnJoinStarted?.Invoke(this, EventArgs.Empty);
        try
        {
            joinedLobby = await LobbyService.Instance.JoinLobbyByCodeAsync(lobbyCode);

            string relayJoinCode = joinedLobby.Data[KEY_RELAY_JOIN_CODE].Value;

            JoinAllocation joinAllocation = await JoinRelay(relayJoinCode);

            NetworkManager.Singleton.GetComponent<UnityTransport>().SetRelayServerData(new RelayServerData(joinAllocation, "dtls"));
            GameManagerMultiplayer.Instance.StartClient();
        }
        catch (LobbyServiceException e)
        {
            Debug.LogError($"Failed to join lobby by code: {e.Message}");
            OnJoinFailed?.Invoke(this, EventArgs.Empty);
        }
    }

    public async void JoinWithId(string lobbyId)
    {
        OnJoinStarted?.Invoke(this, EventArgs.Empty);
        try
        {
            joinedLobby = await LobbyService.Instance.JoinLobbyByIdAsync(lobbyId);

            string relayJoinCode = joinedLobby.Data[KEY_RELAY_JOIN_CODE].Value;

            JoinAllocation joinAllocation = await JoinRelay(relayJoinCode);

            NetworkManager.Singleton.GetComponent<UnityTransport>().SetRelayServerData(new RelayServerData(joinAllocation, "dtls"));
            GameManagerMultiplayer.Instance.StartClient();
        }
        catch (LobbyServiceException e)
        {
            Debug.LogError($"Failed to join lobby by id: {e.Message}");
            OnJoinFailed?.Invoke(this, EventArgs.Empty);
        }
    }

    private void Update()
    {
        HandleLobbyHeartbeat();
        HandlePeriodicListLobbies();
    }

    private void HandlePeriodicListLobbies()
    {
        if (joinedLobby == null && AuthenticationService.Instance.IsSignedIn && SceneManager.GetActiveScene().name == Loader.Scene.LobbyScene.ToString())
        {
            listLobbiesTimer -= Time.deltaTime;
            if (listLobbiesTimer <= 0f)
            {
                float listLobbiesTimerMax = 3f;
                listLobbiesTimer = listLobbiesTimerMax;
                ListLobbies();
            }
        }
    }

    private async void HandleLobbyHeartbeat()
    {
        if (IsLobbyHost())
        {
            heartbeatTimer += Time.deltaTime;
            if (heartbeatTimer <= 0f)
            {
                float heartbeatInterval = 15f;
                heartbeatTimer = heartbeatInterval;

                await LobbyService.Instance.SendHeartbeatPingAsync(joinedLobby.Id);
            }
        }
    }

    private bool IsLobbyHost()
    {
        return joinedLobby != null && joinedLobby.HostId == AuthenticationService.Instance.PlayerId;
    }

    public async void DeleteLobby()
    {
        try
        {
            if (joinedLobby != null)
            {
                await LobbyService.Instance.DeleteLobbyAsync(joinedLobby.Id);
                joinedLobby = null;
            }
        }
        catch (LobbyServiceException e)
        {
            Debug.LogError($"Failed to delete lobby: {e.Message}");
        }
    }

    public async void LeaveLobby()
    {
        if (joinedLobby != null)
        {
            try
            {
                await LobbyService.Instance.RemovePlayerAsync(joinedLobby.Id, AuthenticationService.Instance.PlayerId);
                joinedLobby = null;
            }
            catch (LobbyServiceException e)
            {
                Debug.LogError($"Failed to leave lobby: {e.Message}");
            }
        }
    }

    public async void KickPlayer(string playerId)
    {
        if (IsLobbyHost())
        {
            try
            {
                await LobbyService.Instance.RemovePlayerAsync(joinedLobby.Id, playerId);
            }
            catch (LobbyServiceException e)
            {
                Debug.LogError($"Failed to kick player: {e.Message}");
            }
        }
    }

}
