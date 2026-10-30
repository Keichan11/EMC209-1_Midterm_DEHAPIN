using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

public class NetworkMainMenuController : NetworkBehaviour
{
    [Header("Menu UI Panels")]
    [SerializeField] private GameObject titleScreenPanel; // title screen panel
    [SerializeField] private GameObject hostAndClientButtonsPanel; // host and client buttons panel
    [SerializeField] private GameObject clientPanel; // client panel
    [SerializeField] private GameObject loadingScreenPanel; // loading screen panel
    [SerializeField] private Button startGameButton; // start game button
    [SerializeField] private GameObject loadingAnimation; // GameObject with loading animation
    
    [Header("Host and Button Buttons")]
    [SerializeField] private Button hostButton; // host button
    [SerializeField] private Button clientButton; // client button
    [SerializeField] private TMP_InputField IPInputField; // Input Field for typing IP Address
    [SerializeField] private TMP_Text IPAddressField; // Text Field to place IP Address when host started server "Clicking Host Button"
    [SerializeField] private TMP_Text StatusField; // Text Field for error messages
    [SerializeField] private TMP_Text IPAddressTextOnly; // "IP Address Text"

    [Header("Game Start Settings")]
    private string gameSceneName = "MainGameScene"; // Main Game Scene name
    private int requiredPlayers = 2; // number of players expected to make the game start

    void Awake()
    {
        if (hostButton != null) hostButton.onClick.AddListener(HostButtonClick); 
        if (clientButton != null) clientButton.onClick.AddListener(ClientButtonClick);
        if (startGameButton != null) startGameButton.onClick.AddListener(StartGameButtonClick);
    }

    void Start()
    {
        InitializeMenuState();

        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
            NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnected;
        }

        UpdateHostUIState();
    }

    private void RefocusInputField()
    {
        if (IPInputField != null)
        {
            IPInputField.ActivateInputField();
        }
    }

    public override void OnDestroy()
    {
        base.OnDestroy();

        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnClientConnectedCallback -= OnClientConnected;
            NetworkManager.Singleton.OnClientDisconnectCallback -= OnClientDisconnected;
        }
    }

    void Update()
    {
        HandleInput();
    }

    #region Menu State Management

    private void InitializeMenuState()
    {
        SetMenuPanelActive(titleScreenPanel, true);
        SetMenuPanelActive(hostAndClientButtonsPanel, false);
        SetMenuPanelActive(clientPanel, false);
        SetMenuPanelActive(loadingScreenPanel, false);
        SetMenuPanelActive(loadingAnimation, false);

        if (startGameButton != null)
        {
            SetMenuPanelActive(startGameButton.gameObject, false);
        }

        if (StatusField != null)
        {
            StatusField.text = "";
        }
    }

    private void HandleInput()
    {
        if (Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame)
        {
            if (titleScreenPanel != null && titleScreenPanel.activeSelf)
            {
                TransitionToHostAndClientMenu();
            }
        }
    }

    private void TransitionToHostAndClientMenu()
    {
        SetMenuPanelActive(titleScreenPanel, false);
        SetMenuPanelActive(hostAndClientButtonsPanel, true);
    }

    private void SetMenuPanelActive(GameObject panel, bool isActive)
    {
        if (panel != null)
        {
            panel.SetActive(isActive);
        }
        else
        {
            Debug.LogWarning($"A menu panel reference is missing on {gameObject.name}!", this);
        }
    }

    #endregion
    
    #region Button Click Handlers

    public void ClientButtonClick()
    {
        // If a failed host attempt left the transport/manager listening, shut down cleanly first
        if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening)
        {
            NetworkManager.Singleton.Shutdown();
        }

        SetMenuPanelActive(hostAndClientButtonsPanel, false);
        SetMenuPanelActive(clientPanel, true);

        if (IPInputField != null)
        {
            IPInputField.ActivateInputField();
        }
    }

    public void HostButtonClick()
    {
        StartHost();
    }

    public void EnterButtonClick()
    {
        StartClient();
    }

    public void StartGameButtonClick()
    {
        if (startGameButton != null)
        {
            SetMenuPanelActive(startGameButton.gameObject, false);
        }
        SetMenuPanelActive(loadingAnimation, true);

        RequestStartGameRpc();
    }

    #endregion

    #region Network Logic

    private void StartHost()
    {
        if (NetworkManager.Singleton == null) return;

        // Clean up any stale background sessions before attempting to bind to Port 7777
        if (NetworkManager.Singleton.IsListening)
        {
            NetworkManager.Singleton.Shutdown();
        }

        var transport = NetworkManager.Singleton.GetComponent<UnityTransport>();
        if (transport != null)
        {
            transport.SetConnectionData("127.0.0.1", 7777, "0.0.0.0");
        }

        bool success = false;
        try
        {
            success = NetworkManager.Singleton.StartHost();
        }
        catch (System.Exception ex)
        {
            Debug.LogWarning($"Host failed to bind/start: {ex.Message}");
            success = false;
        }

        if (success)
        {
            // ONLY transition panels if host succeeded
            SetMenuPanelActive(titleScreenPanel, false);
            SetMenuPanelActive(hostAndClientButtonsPanel, false);
            SetMenuPanelActive(clientPanel, false);
            SetMenuPanelActive(loadingScreenPanel, true);

            if (IPAddressField != null)
            {
                SetMenuPanelActive(IPAddressField.gameObject, true);
                IPAddressField.text = GetLocalIPAddress();
            }

            if (IPAddressTextOnly != null)
            {
                SetMenuPanelActive(IPAddressTextOnly.gameObject, true);
            }

            UpdateHostUIState();
        }
        else
        {
            // If failed (e.g. Host already exists on Port 7777), shutdown Netcode safely
            NetworkManager.Singleton.Shutdown();

            if (StatusField != null)
            {
                StatusField.text = "Host already exists or Port 7777 is busy!";
            }

            // DO NOT switch panels. hostAndClientButtonsPanel active so Client button remains clickable
            SetMenuPanelActive(hostAndClientButtonsPanel, true);
            SetMenuPanelActive(loadingScreenPanel, false);
        }
    }

    void StartClient()
    {
        if (NetworkManager.Singleton == null) return;

        string ipAddress = IPInputField != null ? IPInputField.text.Trim() : "";

        if (string.IsNullOrEmpty(ipAddress) || !System.Net.IPAddress.TryParse(ipAddress, out _))
        {
            Debug.LogWarning("Invalid IP Address entered.");
            if (StatusField != null)
            {
                StatusField.text = "Please enter a valid IP Address!";
            }
            
            if (IPInputField != null)
            {
                // IPInputField.ActivateInputField();
                Invoke(nameof(RefocusInputField), 0.01f);
            }
            return; 
        }

        if (NetworkManager.Singleton.IsListening)
        {
            NetworkManager.Singleton.Shutdown();
        }

        var transport = NetworkManager.Singleton.GetComponent<UnityTransport>();
        if (transport != null)
        {
            transport.SetConnectionData(ipAddress, 7777);
        }

        bool success = NetworkManager.Singleton.StartClient();

        if (success)
        {
            SetMenuPanelActive(titleScreenPanel, false);
            SetMenuPanelActive(hostAndClientButtonsPanel, false);
            SetMenuPanelActive(clientPanel, false);
            SetMenuPanelActive(loadingScreenPanel, true);
            SetMenuPanelActive(loadingAnimation, true);

            if (IPAddressField != null)
            {
                SetMenuPanelActive(IPAddressField.gameObject, false);
            }

            if (IPAddressTextOnly != null)
            {
                SetMenuPanelActive(IPAddressTextOnly.gameObject, false);
            }

            if (StatusField != null)
            {
                StatusField.text = $"Connecting to Host at: {ipAddress}:7777";
            }
        }
        else
        {
            if (StatusField != null)
            {
                StatusField.text = "Status: Failed to start Client!";
            }
            
            SetMenuPanelActive(loadingScreenPanel, false);
            SetMenuPanelActive(clientPanel, true);
            if (IPInputField != null) IPInputField.ActivateInputField();
        }
    }

    public void BackToMenu()
    {
        if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening)
        {
            NetworkManager.Singleton.Shutdown();
        }
        InitializeMenuState();
        TransitionToHostAndClientMenu();
    }

    private string GetLocalIPAddress()
    {
        var host = System.Net.Dns.GetHostEntry(System.Net.Dns.GetHostName());
        foreach (var ip in host.AddressList)
        {
            if (ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
            {
                string ipString = ip.ToString();
                if (ipString.StartsWith("192.168.") || ipString.StartsWith("10."))
                {
                    return ipString;
                }
            }
        }
        return "127.0.0.1";
    }

    #endregion

    #region Network Callbacks & RPCs

    private void OnClientConnected(ulong clientId)
    {
        UpdateHostUIState();

        if (NetworkManager.Singleton != null && NetworkManager.Singleton.LocalClientId == clientId)
        {
            Debug.Log("Connected to server successfully!");
        }
    }

    private void OnClientDisconnected(ulong clientId)
    {
        UpdateHostUIState();

        if (NetworkManager.Singleton != null && NetworkManager.Singleton.LocalClientId == clientId)
        {
            SetMenuPanelActive(loadingScreenPanel, false);
            SetMenuPanelActive(clientPanel, true);

            if (IPInputField != null)
            {
                IPInputField.ActivateInputField();
            }

            string errorReason = NetworkManager.Singleton.DisconnectReason;

            if (StatusField != null)
            {
                if (string.IsNullOrEmpty(errorReason))
                {
                    StatusField.text = "Status: Connection Failed. Check IP or Firewall!";
                }
                else
                {
                    StatusField.text = $"Status: Disconnected - {errorReason}";
                }
            }
        }
    }

    private void UpdateHostUIState()
    {
        if (NetworkManager.Singleton == null) return;

        if (NetworkManager.Singleton.IsServer || NetworkManager.Singleton.IsHost)
        {
            int currentPlayers = NetworkManager.Singleton.ConnectedClients.Count;

            if (startGameButton != null)
            {
                SetMenuPanelActive(startGameButton.gameObject, true);
                startGameButton.interactable = (currentPlayers >= requiredPlayers);
            }
        }
        else
        {
            if (startGameButton != null)
            {
                SetMenuPanelActive(startGameButton.gameObject, false);
            }
        }
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    private void RequestStartGameRpc()
    {
        if (NetworkManager.Singleton != null && NetworkManager.Singleton.ConnectedClients.Count >= requiredPlayers)
        {
            ShowClientLoadingRpc();
            Debug.Log("Server received start request. Loading scene for everyone...");
            NetworkManager.Singleton.SceneManager.LoadScene(gameSceneName, LoadSceneMode.Single);
        }
        else
        {
            Debug.LogWarning("Not enough players to start the game yet!");
        }
    }

    [Rpc(SendTo.ClientsAndHost)]
    private void ShowClientLoadingRpc()
    {
        SetMenuPanelActive(titleScreenPanel, false);
        SetMenuPanelActive(hostAndClientButtonsPanel, false);
        SetMenuPanelActive(clientPanel, false);
        SetMenuPanelActive(loadingScreenPanel, true);
        SetMenuPanelActive(loadingAnimation, true);

        if (startGameButton != null)
        {
            SetMenuPanelActive(startGameButton.gameObject, false);
        }
    }

    #endregion
}