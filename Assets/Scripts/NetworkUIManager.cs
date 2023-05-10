using ExitGames.Client.Photon;
using Photon.Pun;
using Photon.Realtime;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;

public class NetworkUIManager : MonoBehaviourPunCallbacks, IInRoomCallbacks
{
    [Header("Login Panel")]
    public GameObject loginPanel;
    public TMP_InputField usernameInput;

    [Header("Register Panel")]

    public GameObject registerPanel;
    public TMP_InputField registerEmailInput;
    public TMP_InputField registerUsernameInput;
    public TMP_InputField registerPasswordInput;
    public TMP_InputField registerConfirmPasswordInput;
    public Button createAccountButton;

    [Header("Choice Panel")]
    public GameObject choicePanel;

    [Header("Create Room Panel")]
    public GameObject createRoomPanel;
    public TMP_InputField roomnameInput;
    public TMP_InputField maxPlayerInput;

    [Header("Random Room Join Panel")]
    public GameObject randomRoomJoinPanel;

    [Header("Room List Panel")]
    public GameObject roomlistPanel;
    public GameObject roomListInfo;
    public GameObject roomlistContent;
    public GameObject roomlistRowPrefab;

    [Header("Room Panel")]
    public GameObject insideRoomPanel;
    public Transform playersListTransform;
    public Button startgameButton;
    public GameObject insideRoomInfoPanel;
    public GameObject playerlistRowPrefab;

    private Dictionary<string, RoomInfo> roomCacheList;
    private Dictionary<string, GameObject> roomlistElements;
    private Dictionary<int, GameObject> playerlistElements;

    [Header("Loading Panel")]
    public GameObject LoadingPanel;
    public RawImage LoadingImage;
    public TextMeshProUGUI ConnectinStateText;

    [Header("CHAT System")]
    public GameObject ChatSistemi;
    public ChatGui chatgui;

    public void Awake()
    {
        PhotonNetwork.AutomaticallySyncScene = true;
        roomCacheList = new Dictionary<string, RoomInfo>();
        roomlistElements = new Dictionary<string, GameObject>();    
    }


    public override void OnConnectedToMaster()
    {
        LoadingImage.transform.DOKill();
        LoadingPanel.SetActive(false);
        ConnectinStateText.text = "Connected";
        PhotonNetwork.AutomaticallySyncScene = true;
        SetActivePanel(choicePanel.name);
    }

    public override void OnJoinedLobby()
    {
        roomCacheList.Clear();
        ClearRoomListView();
        ConnectinStateText.text = "Connecting to Lobby";
    }

    public override void OnLeftLobby()
    {
        roomCacheList.Clear();
        ClearRoomListView();
    }

    public override void OnCreateRoomFailed(short returnCode, string message)
    {
        SetActivePanel(choicePanel.name);
    }

    public override void OnJoinRoomFailed(short returnCode, string message)
    {
        SetActivePanel(choicePanel.name);
    }

    public override void OnJoinRandomFailed(short returnCode, string message)
    {
        string roomName = "Room " + Random.Range(100, 90000);

        RoomOptions options = new RoomOptions { MaxPlayers = 4 };
        PhotonNetwork.CreateRoom(roomName, options, null);
    }

    public override void OnJoinedRoom()
    {
        // Chat sistemine kullan�c� ad� ve oda ad�n� g�nderiyoruz ve ileti�imi ba�lat�yoruz
        ChatSistemi.SetActive(true);
        chatgui = FindObjectOfType<ChatGui>();
        chatgui.UserNickName = PhotonNetwork.LocalPlayer.NickName;
        chatgui.RoomName = PhotonNetwork.CurrentRoom.Name;
        chatgui.Connect();

        //// Ses sistemine kullan�c� ad� ve oda ad�n� g�nderiyoruz ve ileti�imi ba�lat�yoruz
        //VoiceSistemi.SetActive(true);
        //voice = FindObjectOfType<voicesistemi>();
        //voice.KullaniciAdi = PhotonNetwork.LocalPlayer.NickName;
        //voice.Odadi = PhotonNetwork.CurrentRoom.Name;

        roomCacheList.Clear();

        SetActivePanel(insideRoomPanel.name);

        if (playerlistElements == null)
        {
            playerlistElements = new Dictionary<int, GameObject>();
        }

        foreach (Player player in PhotonNetwork.PlayerList)
        {
            GameObject entry = Instantiate(playerlistRowPrefab, playersListTransform);
            entry.transform.localScale = Vector3.one;

            entry.GetComponent<UIPlayerListEntry>().Initialize(player.ActorNumber, player.NickName);

            insideRoomInfoPanel.transform.GetChild(0).GetComponent<TextMeshProUGUI>().text = "Room Name: " + PhotonNetwork.CurrentRoom.Name;
            insideRoomInfoPanel.transform.GetChild(1).GetComponent<TextMeshProUGUI>().text = "Room Owner: " + PhotonNetwork.MasterClient.NickName;
            insideRoomInfoPanel.transform.GetChild(2).GetComponent<TextMeshProUGUI>().text = PhotonNetwork.CurrentRoom.PlayerCount.ToString() + "/" + PhotonNetwork.CurrentRoom.MaxPlayers.ToString();

            if (player.CustomProperties.TryGetValue("IsPlayerReady", out object isPlayerReady))
            {
                entry.GetComponent<UIPlayerListEntry>().SetPlayerReady((bool) isPlayerReady);
            }

            if (player.CustomProperties.TryGetValue("SelectedTeamID", out object selectedTeamID))
            {
                entry.GetComponent<UIPlayerListEntry>().SetPlayerTeamColor((TeamID)selectedTeamID);
            }

            playerlistElements.Add(player.ActorNumber, entry);
        }

        startgameButton.gameObject.SetActive(CheckPlayersReady());

        Hashtable props = new Hashtable
        {
            {"PlayerLoadedLevel", false }
        };
        PhotonNetwork.LocalPlayer.SetCustomProperties(props);

    }

    public override void OnLeftRoom()
    {
        ChatSistemi.SetActive(false);
        //VoiceSistemi.SetActive(false);
        SetActivePanel(choicePanel.name);

        foreach (GameObject entry in playerlistElements.Values)
        {
            Destroy(entry);
        }

        playerlistElements.Clear();
        playerlistElements = null;
    }

    public override void OnPlayerEnteredRoom(Player newPlayer)
    {
        GameObject entry = Instantiate(playerlistRowPrefab, playersListTransform);
        entry.transform.localScale = Vector3.one;

        entry.GetComponent<UIPlayerListEntry>().Initialize(newPlayer.ActorNumber, newPlayer.NickName);
        playerlistElements.Add(newPlayer.ActorNumber, entry);

        startgameButton.gameObject.SetActive(CheckPlayersReady());
        insideRoomInfoPanel.transform.GetChild(0).GetComponent<TextMeshProUGUI>().text = "Room Name: " + PhotonNetwork.CurrentRoom.Name;
        insideRoomInfoPanel.transform.GetChild(1).GetComponent<TextMeshProUGUI>().text = "Room Owner: " + PhotonNetwork.MasterClient.NickName;
        insideRoomInfoPanel.transform.GetChild(2).GetComponent<TextMeshProUGUI>().text = PhotonNetwork.CurrentRoom.PlayerCount.ToString() + "/" + PhotonNetwork.CurrentRoom.MaxPlayers.ToString();

    }

    public override void OnPlayerLeftRoom(Player otherPlayer)
    {
        Destroy(playerlistElements[otherPlayer.ActorNumber].gameObject);
        playerlistElements.Remove(otherPlayer.ActorNumber);
        startgameButton.gameObject.SetActive(CheckPlayersReady());
        insideRoomInfoPanel.transform.GetChild(0).GetComponent<TextMeshProUGUI>().text = "Room Name: " + PhotonNetwork.CurrentRoom.Name;
        insideRoomInfoPanel.transform.GetChild(1).GetComponent<TextMeshProUGUI>().text = "Room Owner: " + PhotonNetwork.MasterClient.NickName;
        insideRoomInfoPanel.transform.GetChild(2).GetComponent<TextMeshProUGUI>().text = PhotonNetwork.CurrentRoom.PlayerCount.ToString() + "/" + PhotonNetwork.CurrentRoom.MaxPlayers.ToString();


    }

    public override void OnMasterClientSwitched(Player newMasterClient)
    {
        if (PhotonNetwork.LocalPlayer.ActorNumber == newMasterClient.ActorNumber)
        {
            startgameButton.gameObject.SetActive(CheckPlayersReady());
        }
        insideRoomInfoPanel.transform.GetChild(0).GetComponent<TextMeshProUGUI>().text = "Room Name: " + PhotonNetwork.CurrentRoom.Name;
        insideRoomInfoPanel.transform.GetChild(1).GetComponent<TextMeshProUGUI>().text = "Room Owner: " + PhotonNetwork.MasterClient.NickName;
        insideRoomInfoPanel.transform.GetChild(2).GetComponent<TextMeshProUGUI>().text = PhotonNetwork.CurrentRoom.PlayerCount.ToString() + "/" + PhotonNetwork.CurrentRoom.MaxPlayers.ToString();

    }

    public override void OnPlayerPropertiesUpdate(Player targetPlayer, Hashtable changedProps)
    {
        if (playerlistElements == null)
        {
            playerlistElements = new Dictionary<int, GameObject>();
        }

        if (playerlistElements.TryGetValue(targetPlayer.ActorNumber, out GameObject entry))
        {
            if (changedProps.TryGetValue("IsPlayerReady", out object isPlayerReady))
            {
                entry.GetComponent<UIPlayerListEntry>().SetPlayerReady((bool)isPlayerReady);
            }
            if (changedProps.TryGetValue("SelectedTeamID", out object selectedTeamID))
            {
                entry.GetComponent<UIPlayerListEntry>().SetPlayerTeamColor((TeamID)selectedTeamID);
            }
        }

        startgameButton.gameObject.SetActive(CheckPlayersReady());
    }
    public override void OnRoomListUpdate(List<RoomInfo> roomList)
    {
        ClearRoomListView();
        UpdateCachedRoomList(roomList);
        UpdateRoomListView();

    }

    public void OnBackButtonClicked()
    {
        if (PhotonNetwork.InLobby)
        {
            PhotonNetwork.LeaveLobby();
        }
        SetActivePanel(choicePanel.name);
    }

    public void OnCreateRoomButtonClicked()
    {
        string roomName = roomnameInput.text;

        roomName = (roomName.Equals(string.Empty)) ? "Room " + Random.Range(100, 90000) : roomName;

        byte.TryParse(maxPlayerInput.text, out byte maxPlayer);
        maxPlayer = (byte)Mathf.Clamp(maxPlayer, 1, 8);

        RoomOptions options = new RoomOptions { MaxPlayers = maxPlayer };
        PhotonNetwork.CreateRoom(roomName, options, null);
        
    }

    public void OnJoinRandomRoomButtonClicked()
    {
        SetActivePanel(randomRoomJoinPanel.name);
        PhotonNetwork.JoinRandomRoom();
    }

    public void OnLeaveGameButtonClicked()
    {
        ChatSistemi.SetActive(false);
        PhotonNetwork.LeaveRoom();
    }

    public void OnLoginButtonClicked()
    {
        string playerName = usernameInput.text;

        if (!playerName.Equals(""))
        {
            PhotonNetwork.LocalPlayer.NickName = playerName;
            PhotonNetwork.ConnectUsingSettings();
            LoadingPanel.SetActive(true);
            LoadingImage.transform.DORotate(new Vector3(0f, 0f, -360f), 0.5f, RotateMode.FastBeyond360).SetLoops(-1); ;
            ConnectinStateText.text = "Connecting...";
        }
        else
        {
            Debug.LogError("Username not suitable");
        }
    }

    public void OnRoomListButtonClicked()
    {
        if (!PhotonNetwork.InLobby)
        {
            PhotonNetwork.JoinLobby();
        }
        SetActivePanel(roomlistPanel.name);

        InvokeRepeating(nameof(UpdateRoomListInfos), 0, 2);
    }

    void UpdateRoomListInfos()
    {
        if (roomlistPanel.activeSelf)
        {
            roomListInfo.transform.GetChild(0).GetComponent<TextMeshProUGUI>().text = "Total Room: " + PhotonNetwork.CountOfRooms.ToString();
            roomListInfo.transform.GetChild(1).GetComponent<TextMeshProUGUI>().text = "Players In Room: " + PhotonNetwork.CountOfPlayersInRooms.ToString();
            roomListInfo.transform.GetChild(2).GetComponent<TextMeshProUGUI>().text = "Players In Lobby: " + PhotonNetwork.CountOfPlayersOnMaster.ToString();
            roomListInfo.transform.GetChild(3).GetComponent<TextMeshProUGUI>().text = "Online Players: " + PhotonNetwork.CountOfPlayers.ToString();
        }
        else
        {
            CancelInvoke(nameof(UpdateRoomListInfos));
        }
    }

    public void OnStartGameButtonClicked()
    {
        PhotonNetwork.CurrentRoom.IsOpen = false;
        PhotonNetwork.CurrentRoom.IsVisible = false;
        PhotonNetwork.LoadLevel("GameScene");
    }

    private bool CheckPlayersReady()
    {
        if (!PhotonNetwork.IsMasterClient) 
        {   
            return false;
        }

        foreach (Player player in PhotonNetwork.PlayerList)
        {
            if (player.CustomProperties.TryGetValue("IsPlayerReady", out object isPlayerReady))
            {
                if (!(bool) isPlayerReady)
                {
                    return false;
                }
            }
            else // if player has npt isPlayerReady feature.
            {
                return false;
            }
        }
        Debug.Log("Checkpalyersready: " + PhotonNetwork.IsMasterClient);
        return true;
    }

    private void ClearRoomListView()
    {
        foreach (GameObject entry in roomlistElements.Values)
        {
            Destroy(entry);
        }

        roomlistElements.Clear();
    }

    public void LocalPlayerPropertiesUpdated()
    {
        startgameButton.gameObject.SetActive(CheckPlayersReady());
    }

    public void SetActivePanel(string activePanel)
    { 
        loginPanel.SetActive(activePanel.Equals(loginPanel.name));
        choicePanel.SetActive(activePanel.Equals(choicePanel.name));
        createRoomPanel.SetActive(activePanel.Equals(createRoomPanel.name));
        randomRoomJoinPanel.SetActive(activePanel.Equals(randomRoomJoinPanel.name));
        roomlistPanel.SetActive(activePanel.Equals(roomlistPanel.name));
        insideRoomPanel.SetActive(activePanel.Equals(insideRoomPanel.name));
        registerPanel.SetActive(activePanel.Equals(registerPanel.name));
    }

    private void UpdateCachedRoomList(List<RoomInfo> roomList)
    {
        foreach (RoomInfo info in roomList)
        {
            if (!info.IsOpen || !info.IsVisible || info.RemovedFromList) 
            {
                if (roomCacheList.ContainsKey(info.Name))
                {
                    roomCacheList.Remove(info.Name);
                }

                continue; // continue with next room
            }

            if (roomCacheList.ContainsKey(info.Name))
            {
                roomCacheList[info.Name] = info;
            }
            else
            {
                roomCacheList.Add(info.Name, info);
            }
        }
    }

    private void UpdateRoomListView()
    {
        foreach (RoomInfo info in roomCacheList.Values)
        {
            GameObject entry = Instantiate(roomlistRowPrefab);//, roomlistContent.transform.localPosition + new Vector3(x, y, z), Quaternion.identity, roomlistContent.transform);
            entry.transform.SetParent(roomlistContent.transform);
            entry.transform.localScale = Vector3.one;

            entry.GetComponent<UIRoomListEntry>().Initialize(info.Name, (byte)info.PlayerCount, info.MaxPlayers, (info.MaxPlayers == info.PlayerCount));

            roomlistElements.Add(info.Name, entry);
        }
    }
}
