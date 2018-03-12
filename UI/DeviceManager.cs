using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Juniverse.RPCLibrary;
using Juniverse.Notifications;
using Juniverse.Model;
using Newtonsoft.Json;
using System;
using System.Net.Sockets;
public class DeviceManager : MonoBehaviour
{
    static DeviceManager instance = null;
    public static DeviceManager Instance
    {
        get { return DeviceManager.instance; }
    }
    public AttractionSession CurrentAttractionSession;
    [SerializeField] GameObject DeviceCanvas;
    [SerializeField] GameObject LoggingCanvas;
    [SerializeField] GameObject LoadingPanel;
    [SerializeField] ExceptionPanel _exceptionPanel;
    int _loadingCounter = 0;

    public Color textColor;
    public int UIElementRefreshRate = 10;

    RPCConnection _rpcConnection = null;
    NotificationManager _notifications = new NotificationManager();
    bool _isConnected = false;
    public bool IsConnected
    {
        get { return _isConnected; }
    }
    bool _isSessionCreated = false;

    ServiceInterface serviceInterface;

    string _staffMemberID = "";
    string _staffPassword = "";

    ImageGrabber _imageGrabber;
    public ImageGrabber ImageGrabber
    {
        get { return _imageGrabber; }
    }

    PopUpManager _popUpManager;
    public PopUpManager PopUpManager
    {
        get { return _popUpManager; }
    }

    StaffMemberProfile _profile;
    public StaffMemberProfile Profile
    {
        get
        {
            return _profile;
        }
        set
        {
            _profile = value;
        }
    }

    Juniverse.ClientLibrary.MainGame _mainGame;
    public Juniverse.ClientLibrary.MainGame MainGame
    {
        get
        {
            return _mainGame;
        }
        set
        {
            _mainGame = value;
        }
    }

    GameData _gameData;
    public GameData GameData
    {
        get
        {
            return _gameData;
        }
        set
        {
            _gameData = value;
        }
    }

    void Awake()
    {
        if (instance != null)
            Destroy(instance.gameObject);
        instance = this;

        DontDestroyOnLoad(gameObject);
    }

    void Start()
    {
        serviceInterface = GetComponent<ServiceInterface>();
        _imageGrabber = GetComponent<ImageGrabber>();
        _popUpManager = GetComponent<PopUpManager>();
        LoadingPanel.SetActive(false);
        DeviceCanvas.SetActive(false);
        LoggingCanvas.SetActive(true);
    }

    void Update()
    {
        StartCoroutine(Reactor.Loop.Enumerator());
        
        LoadingPanel.SetActive(_loadingCounter > 0);
    }

    public void StartLoading()
    {
        //Debug.Log("Start Loading: " + _loadingCounter);
        _loadingCounter++;
    }

    public void EndLoading()
    {
        if (_loadingCounter > 0)
            _loadingCounter--;

        //Debug.Log("End Loading: " + _loadingCounter);
    }

    public void ShowException(Exception ex)
    {
        //Debug.LogException(ex);
        _exceptionPanel.gameObject.SetActive(true);
        _exceptionPanel.ShowException(ex);

        ExceptionData exceptionData = null;
        foreach (KeyValuePair<string, ExceptionData> exception in _gameData.Exceptions)
        {
            if (ex.GetType().Name == exception.Value.Name)
            {
                exceptionData = exception.Value;
                break;
            }
        }

        if (exceptionData != null)
        {
            _popUpManager.ShowNotification(exceptionData.Message.GetLocalized(_profile.Language));
        }
    }

    #region Connection Handling

    bool connectionListenersAdded = false;

    public void ConnectToServer(string serverIP, Action<RPCConnection> Callback)
    {
        StartLoading();

        if (string.IsNullOrEmpty(serverIP))
            serverIP = "127.0.0.1";

        if (!connectionListenersAdded)
        {
            _rpcConnection = new RPCConnection(serverIP, 2101);
            _rpcConnection.AddCallableObject(_notifications);
            _rpcConnection.OnConnect += rpcConnection_OnConnect;
            _rpcConnection.OnConnect += Callback;
            _rpcConnection.OnReconnect += rpcConnection_OnReconnect;
            _rpcConnection.OnError += rpcConnection_OnError;
            _rpcConnection.OnEnd += _rpcConnection_OnEnd;
            _rpcConnection.OnRetry += _rpcConnection_OnRetry;
            _rpcConnection.OnLog += _rpcConnection_OnLog;

            connectionListenersAdded = true;
        }
        _rpcConnection.Connect();
    }

    //Callbacks
    void rpcConnection_OnConnect(RPCConnection RpcConnection)
    {
        Debug.Log("Connected...");
        _isConnected = true;
        MainGame = new Juniverse.ClientLibrary.MainGame(RpcConnection);
        MainGame.OnProfileChanged += ProfileChangedNotification;
        MainGame.OnLoggedOut += LoggedOutNotification;
        _popUpManager.BindNotificationsFunctions();
        Debug.Log("Fetching game data...");
        DeviceManager.instance.MainGame.GetGameDataAsync((gameData) =>
        {
            Debug.Log("Fetching game data. Done!");
            DeviceManager.instance.EndLoading();
            GameData = gameData;
        }, (ex) => DeviceManager.instance.ShowException(ex));
    }

    void ProfileChangedNotification(ProfileChanged profileChanged)
    {
        Profile.CommitChanges(profileChanged.Changes);
    }

    void LoggedOutNotification(LoggedOut loggedOut)
    {
        _isConnected = false;
        _isSessionCreated = false;

        DeviceCanvas.SetActive(false);
        LoggingCanvas.SetActive(true);
    }

    void rpcConnection_OnReconnect(RPCConnection rpcConnection)
    {
        Debug.LogWarning("Reconnecting...");
        
        DeviceManager.instance.MainGame.GetGameDataAsync((gameData) =>
        {
            Debug.Log("Fetching game data. Done!");
            DeviceManager.instance.EndLoading();
            GameData = gameData;
        }, (ex) => DeviceManager.instance.ShowException(ex));
        serviceInterface.ReconnectServer(DeviceManager.instance._staffMemberID, DeviceManager.instance._staffPassword, rpcConnection);
        EndLoading();
    }

    void rpcConnection_OnError(RPCConnection connection, Exception ex)
    {
        DeviceManager.instance.ShowException(ex);
        if (ex.GetType() == typeof(SocketException) && ((SocketException)ex).ErrorCode == 11001)
            _rpcConnection.Close();
    }

    void _rpcConnection_OnEnd(RPCConnection obj)
    {
        Debug.LogWarning("Connection end");
    }

    void _rpcConnection_OnRetry(RPCConnection obj)
    {
        Debug.LogWarning("Retrying connection");
    }

    void _rpcConnection_OnLog(RPCConnection arg1, string arg2)
    {
        ////Debug.Log(arg2);
    } 

    #endregion

    public void StartStaffMemberSession(string staffMemberID, string Password)
    {
        DeviceManager.instance._staffMemberID = staffMemberID;
        DeviceManager.instance._staffPassword = Password;
        if (DeviceManager.instance._isConnected && !DeviceManager.instance._isSessionCreated)
        {
            serviceInterface.StartStaffMemberSession(DeviceManager.instance._staffMemberID,Password, (sessionCreated) =>
            {
                _isSessionCreated = sessionCreated;
                DeviceCanvas.SetActive(_isSessionCreated);
                LoggingCanvas.SetActive(!_isSessionCreated);
            });
        }
    }

    void ProcessQueuedTasks()
    {
        int tickCount = Environment.TickCount;

        while (_rpcConnection.HasPendingCalls())
        {
            //Debug.Log("Has pending call");
            while (Reactor.Loop.Enumerator().MoveNext())
            {
                Debug.Log("Next task");
            }

            if (Environment.TickCount - tickCount > 10000)
                break;
        }
    }

    public void ReloadGameData()
    {
        _mainGame.ReloadGameDataAsync();
    }

    void OnDestroy()
    {
        if (Profile != null && _rpcConnection != null)
        {
            DeviceManager.instance.StartLoading();
            //Debug.Log("Ending session");
            DeviceManager.instance.MainGame.EndStaffSessionAsync(Profile.UserId, () =>
            {
                DeviceManager.instance.EndLoading();

                Debug.Log("Done session");

             //   _rpcConnection.Close();
            }, (ex) =>
                {
                    DeviceManager.instance.ShowException(ex);

                    Debug.LogException(ex);

                   // _rpcConnection.Close();
                });

            //Debug.Log("Processing.");

            ProcessQueuedTasks();
        }
    }
}