using BarcodeVerification.lib.Common;
using BarcodeVerification.lib.Extensions;
using BarcodeVerification.lib.GS1;
using CommunityToolkit.Mvvm.ComponentModel;
using Logging.lib;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using V275_REST_Lib.Enumerations;
using V275_REST_Lib.Models;
using Devices = V275_REST_Lib.Models.Devices;
using GradingStandards = V275_REST_Lib.Models.GradingStandards;

namespace V275_REST_Lib.Controllers;

/// <summary>
/// Main controller for interacting with the V275 REST API and WebSocket events.
/// This class manages system state, handles communication, and processes data.
/// </summary>
[JsonObject(MemberSerialization.OptIn)]
public partial class Controller : ObservableObject
{
    #region Enums & Constants

    private static readonly SynchronizationContext OwnerContext = SynchronizationContext.Current;

    /// <summary>
    /// Constant for converting inches to meters.
    /// </summary>
    public const double InchesPerMeter = 39.3701;

    /// <summary>
    /// Represents the possible outcomes of a sector restoration operation.
    /// </summary>
    public enum RestoreSectorsResults
    {
        Success = 1,
        Detect = 2,
        Failure = -1
    }

    /// <summary>
    /// Provides a mapping of match mode integers to their string representations.
    /// </summary>
    public Dictionary<int, string> MatchModes { get; } = new Dictionary<int, string>()
    {
        {0, "Standard" },
        {1, "Exact String" },
        {2, "Match Region" },
        {3, "Sequential Inc+" },
        {4, "Sequential Dec-" },
        {5, "Match Start" },
        {6, "File Start" },
        {7, "Duplicate Check" },

    };

    #endregion

    #region Public Properties

    #region Communication & Commands

    /// <summary>
    /// Gets the command object for sending requests to the REST API.
    /// </summary>
    public Commands Commands { get; } = new Commands();

    /// <summary>
    /// Gets the WebSocket event handler for real-time communication.
    /// </summary>
    public WebSocketEvents WebSocket { get; } = new WebSocketEvents();

    #endregion

    #region Configuration Properties

    /// <summary>
    /// The host address of the V275 system.
    /// <see cref="Host"/>
    /// </summary>
    [ObservableProperty][property: JsonProperty] private string host;
    partial void OnHostChanged(string value) { Commands.URLs.Host = value; }

    /// <summary>
    /// The system port of the V275 system.
    /// <see cref="SystemPort"/>
    /// </summary>
    [ObservableProperty][property: JsonProperty] private uint systemPort;
    partial void OnSystemPortChanged(uint value) { Commands.URLs.SystemPort = value; }

    /// <summary>
    /// The node number of the V275 system.
    /// <see cref="NodeNumber"/>
    /// </summary>
    [ObservableProperty][property: JsonProperty] private uint nodeNumber;
    partial void OnNodeNumberChanged(uint value) { Commands.URLs.NodeNumber = value; }

    /// <summary>
    /// The username for authentication.
    /// <see cref="Username"/>
    /// </summary>
    [ObservableProperty][property: JsonProperty] private string username;
    /// <summary>
    /// The password for authentication.
    /// <see cref="Password"/>
    /// </summary>
    [ObservableProperty][property: JsonProperty] private string password;
    /// <summary>
    /// The directory for simulator images.
    /// <see cref="SimulatorImageDirectory"/>
    /// </summary>
    [ObservableProperty][property: JsonProperty] private string simulatorImageDirectory;

    /// <summary>
    /// Use the simulation directory or use the API for images.
    /// <see cref="UseSimulationDirectory"/>
    /// </summary>
    [ObservableProperty][property: JsonProperty]  private bool useSimulationDirectory = false;

    #endregion

    #region State Properties

    /// <summary>
    /// The current state of the node.
    /// <see cref="State"/>
    /// </summary>
    [ObservableProperty] private NodeStates state = NodeStates.Offline;

    /// <summary>
    /// A value indicating whether the user is logged in with monitor access.
    /// <see cref="IsLoggedIn_Monitor"/>
    /// </summary>
    [ObservableProperty] private bool isLoggedIn_Monitor = false;
    partial void OnIsLoggedIn_MonitorChanged(bool value) { OnPropertyChanged(nameof(IsLoggedIn)); OnPropertyChanged(nameof(IsNotLoggedIn)); }

    /// <summary>
    /// A value indicating whether the user is logged in with control access.
    /// <see cref="IsLoggedIn_Control"/>
    /// </summary>
    [ObservableProperty] private bool isLoggedIn_Control = false;
    public bool IsNotLoggedIn_Control => !IsLoggedIn_Control;
    partial void OnIsLoggedIn_ControlChanged(bool value) { OnPropertyChanged(nameof(IsLoggedIn)); OnPropertyChanged(nameof(IsNotLoggedIn)); OnPropertyChanged(nameof(IsNotLoggedIn_Control)); }

    /// <summary>
    /// Gets a value indicating whether the user is logged in with either monitor or control access.
    /// </summary>
    public bool IsLoggedIn => IsLoggedIn_Monitor || IsLoggedIn_Control;

    /// <summary>
    /// Gets a value indicating whether the user is not logged in.
    /// </summary>
    public bool IsNotLoggedIn => !(IsLoggedIn_Monitor || IsLoggedIn_Control);

    /// <summary>
    /// The login data for the current session.
    /// <see cref="LoginData"/>
    /// </summary>
    [ObservableProperty] private Events_System.Data loginData = new();

    #endregion

    #region System API Properties

    /// <summary>
    /// The devices information for the system.
    /// <see cref="Devices"/>
    /// </summary>
    [ObservableProperty] private Devices? devices;
    partial void OnDevicesChanging(Devices? value) => IsDevicesValid = value != null && value is Devices;
    partial void OnDevicesChanged(Devices? value)
    {
        OnPropertyChanged(nameof(Node));
        OnPropertyChanged(nameof(Camera));
    }
    /// <summary>
    /// The JSON representation of the devices information.
    /// <see cref="DevicesJSON"/>
    /// </summary>
    [ObservableProperty] private string devicesJSON;
    /// <summary>
    /// A value indicating whether the devices information is valid.
    /// <see cref="IsDevicesValid"/>
    /// </summary>
    [ObservableProperty] private bool isDevicesValid;

    /// <summary>
    /// Gets the current node based on the <see cref="NodeNumber"/>.
    /// </summary>
    public Devices.Node? Node => Devices?.nodes?.FirstOrDefault((n) => n.enumeration == NodeNumber);

    /// <summary>
    /// Gets the camera associated with the current node.
    /// </summary>
    public Devices.Camera? Camera => Devices?.cameras?.FirstOrDefault((c) => c.mac == Node.cameraMAC);

    /// <summary>
    /// The product information for the system.
    /// <see cref="Product"/>
    /// </summary>
    [ObservableProperty] private Product? product;
    partial void OnProductChanging(Product? value) => IsProductValid = value != null && value is Product;
    partial void OnProductChanged(Product? value)
    {
        if (value != null)
            Version = $"{Product?.version?.major}.{Product?.version?.minor}.{Product?.version?.service}.{Product?.version?.build}";
        else
            Version = "----";
    }

    /// <summary>
    /// The JSON representation of the product information.
    /// <see cref="ProductJSON"/>
    /// </summary>
    [ObservableProperty] private string productJSON;
    /// <summary>
    /// A value indicating whether the product information is valid.
    /// <see cref="IsProductValid"/>
    /// </summary>
    [ObservableProperty] private bool isProductValid;
    /// <summary>
    /// The version of the product.
    /// <see cref="Version"/>
    /// </summary>
    [ObservableProperty] private string version = "----";

    #endregion

    #region Data Model Properties

    /// <summary>
    /// The inspection information for the system.
    /// <see cref="Inspection"/>
    /// </summary>
    [ObservableProperty] private Inspection? inspection;
    partial void OnInspectionChanging(Inspection? value) => IsInspectionValid = value != null && value is Inspection;
    partial void OnInspectionChanged(Inspection? value)
    {
        OnPropertyChanged(nameof(IsSimulator));
    }
    /// <summary>
    /// The JSON representation of the inspection information.
    /// <see cref="InspectionJSON"/>
    /// </summary>
    [ObservableProperty] private string inspectionJSON;
    /// <summary>
    /// A value indicating whether the inspection information is valid.
    /// <see cref="IsInspectionValid"/>
    /// </summary>
    [ObservableProperty] private bool isInspectionValid;

    /// <summary>
    /// The jobs information for the system.
    /// <see cref="Jobs"/>
    /// </summary>
    [ObservableProperty] private Jobs? jobs;
    partial void OnJobsChanging(Jobs? value) => IsJobsValid = value != null && value is Jobs;
    /// <summary>
    /// The JSON representation of the jobs information.
    /// <see cref="JobsJSON"/>
    /// </summary>
    [ObservableProperty] private string jobsJSON;
    /// <summary>
    /// A value indicating whether the jobs information is valid.
    /// <see cref="IsJobsValid"/>
    /// </summary>
    [ObservableProperty] private bool isJobsValid;

    /// <summary>
    /// A value indicating whether the old ISO standard is being used.
    /// <see cref="IsOldISO"/>
    /// </summary>
    [ObservableProperty] private bool isOldISO;

    /// <summary>
    /// The camera configuration for the system.
    /// <see cref="ConfigurationCamera"/>
    /// </summary>
    [ObservableProperty] private Configuration_Camera? configurationCamera;
    partial void OnConfigurationCameraChanging(Configuration_Camera? value) => IsConfigurationCameraValid = value != null && value is Configuration_Camera;
    partial void OnConfigurationCameraChanged(Configuration_Camera? value)
    {
        OnPropertyChanged(nameof(IsBackupVoid));
    }
    /// <summary>
    /// The JSON representation of the camera configuration.
    /// <see cref="ConfigurationCameraJSON"/>
    /// </summary>
    [ObservableProperty] private string configurationCameraJSON;
    /// <summary>
    /// A value indicating whether the camera configuration is valid.
    /// <see cref="IsConfigurationCameraValid"/>
    /// </summary>
    [ObservableProperty] private bool isConfigurationCameraValid;

    /// <summary>
    /// The list of symbologies for the system.
    /// <see cref="Symbologies"/>
    /// </summary>
    [ObservableProperty] private List<Models.Symbologies.Symbol>? symbologies;
    partial void OnSymbologiesChanging(List<Models.Symbologies.Symbol>? value) => IsSymbologiesValid = value != null && value is List<Models.Symbologies.Symbol>;
    /// <summary>
    /// The JSON representation of the symbologies.
    /// <see cref="SymbologiesJSON"/>
    /// </summary>
    [ObservableProperty] private string symbologiesJSON;
    /// <summary>
    /// A value indicating whether the symbologies are valid.
    /// <see cref="IsSymbologiesValid"/>
    /// </summary>
    [ObservableProperty] private bool isSymbologiesValid;

    /// <summary>
    /// The calibration information for the system.
    /// <see cref="Calibration"/>
    /// </summary>
    [ObservableProperty] private Calibration? calibration;
    partial void OnCalibrationChanging(Calibration? value) => IsCalibrationValid = value != null && value is Calibration;
    /// <summary>
    /// The JSON representation of the calibration information.
    /// <see cref="CalibrationJSON"/>
    /// </summary>
    [ObservableProperty] private string calibrationJSON;
    /// <summary>
    /// A value indicating whether the calibration information is valid.
    /// <see cref="IsCalibrationValid"/>
    /// </summary>
    [ObservableProperty] private bool isCalibrationValid;

    /// <summary>
    /// The simulation information for the system.
    /// <see cref="Simulation"/>
    /// </summary>
    [ObservableProperty] private Simulation? simulation;
    partial void OnSimulationChanging(Simulation? value) => IsSimulationValid = value != null && value is Simulation;
    /// <summary>
    /// The JSON representation of the simulation information.
    /// <see cref="SimulationJSON"/>
    /// </summary>
    [ObservableProperty] private string simulationJSON;
    /// <summary>
    /// A value indicating whether the simulation information is valid.
    /// <see cref="IsSimulationValid"/>
    /// </summary>
    [ObservableProperty] private bool isSimulationValid;

    /// <summary>
    /// The print information for the system.
    /// <see cref="Print"/>
    /// </summary>
    [ObservableProperty] private Print? print;
    partial void OnPrintChanging(Print? value) => IsPrintValid = value != null && value is Print;
    /// <summary>
    /// The JSON representation of the print information.
    /// <see cref="PrintJSON"/>
    /// </summary>
    [ObservableProperty] private string printJSON;
    /// <summary>
    /// A value indicating whether the print information is valid.
    /// <see cref="IsPrintValid"/>
    /// </summary>
    [ObservableProperty] private bool isPrintValid;

    /// <summary>
    /// The current job for the system.
    /// <see cref="Job"/>
    /// </summary>
    [ObservableProperty] private Job? job;
    partial void OnJobChanging(Job? value) => IsJobValid = value != null && value is Job;
    partial void OnJobChanged(Job? value)
    {
        OnPropertyChanged(nameof(JobName));
    }
    /// <summary>
    /// The JSON representation of the current job.
    /// <see cref="JobJSON"/>
    /// </summary>
    [ObservableProperty] private string jobJSON;
    /// <summary>
    /// A value indicating whether the current job is valid.
    /// <see cref="IsJobValid"/>
    /// </summary>
    [ObservableProperty] private bool isJobValid;

    /// <summary>
    /// The DPI for the system.
    /// <see cref="Dpi"/>
    /// </summary>
    [ObservableProperty] private int dpi = 600;
    partial void OnDpiChanged(int value) => OnPropertyChanged(nameof(Is600Dpi));

    /// <summary>
    /// Gets a value indicating whether the DPI is 600 or not set.
    /// </summary>
    public bool Is600Dpi => Dpi is <= 0 or 600;

    /// <summary>
    /// Gets a value indicating whether the system is in simulator mode.
    /// </summary>
    public bool IsSimulator => IsInspectionValid && Inspection.device.Equals("simulator");

    /// <summary>
    /// Gets a value indicating whether the backup void mode is enabled.
    /// </summary>
    public bool IsBackupVoid => IsConfigurationCameraValid && ConfigurationCamera?.backupVoidMode?.value == "ON";

    /// <summary>
    /// Gets the name of the current job.
    /// </summary>
    public string? JobName => IsJobValid ? Job?.name : "";

    #endregion

    #endregion

    #region Private Properties

    private ConcurrentDictionary<int, Repeat> RepeatBuffer { get; } = [];
    private bool LabelStart;
    private Label? ActiveLabel { get; set; }

    #endregion

    #region Constructor and Initialization

    /// <summary>
    /// Initializes a new instance of the <see cref="Controller"/> class.
    /// </summary>
    /// <param name="host">The host address of the V275 system.</param>
    /// <param name="port">The system port number.</param>
    /// <param name="nodeNumber">The node number to connect to.</param>
    /// <param name="userName">The username for authentication.</param>
    /// <param name="password">The password for authentication.</param>
    /// <param name="dir">The directory for simulator images.</param>
    public Controller(string host, uint port, uint nodeNumber, string userName, string password, string dir, bool useSimDir)
    {
        Host = host;
        SystemPort = port;
        NodeNumber = nodeNumber;
        Username = userName;
        Password = password;
        SimulatorImageDirectory = dir;
        UseSimulationDirectory = useSimDir;

        WebSocket.MessageReceived += WebSocketEvents_MessageReceived;
    }

    /// <summary>
    /// Initializes the controller by fetching initial device and inspection data.
    /// </summary>
    public void Initialize() => Task.Run(async () =>
    {
        _ = await UpdateDevices();
        _ = await UpdateInspection();
    }).Wait();

    #endregion

    #region Login and Logout

    /// <summary>
    /// Logs into the system with either monitor or control access. If already logged in, this method will log out.
    /// </summary>
    /// <param name="monitor">True for monitor access, false for control access.</param>
    public async Task Login(bool monitor)
    {
        if (IsLoggedIn)
        {
            Logger.Debug($"Logging out. {Username} @ {Host}:{SystemPort}");
            await Logout();
            return;
        }

        if (!PreLogin())
        {
            Logger.Debug($"Pre-Log in FAILED. {Username} @ {Host}:{SystemPort}");
            return;
        }

        Logger.Debug($"Logging in. {Username} @ {Host}:{SystemPort}");

        if ((await Commands.Login(Username, Password, monitor)).OK)
        {
            Logger.Debug($"Logged in. {(monitor ? "Monitor" : "Control")} {Username} @ {Host}:{SystemPort}");

            PostLogin(monitor);
        }
        else
        {
            Logger.Error($"Login FAILED. {Username} @ {Host}:{SystemPort}");

            IsLoggedIn_Control = false;
            IsLoggedIn_Monitor = false;
        }
    }

    /// <summary>
    /// Performs pre-login checks, such as verifying the simulator image directory.
    /// </summary>
    private bool PreLogin()
    {
        if (IsSimulator && UseSimulationDirectory)
        {
            if (Directory.Exists(SimulatorImageDirectory))
            {
                try
                {
                    // Test write permissions in the simulator directory.
                    File.Create(Path.Combine(SimulatorImageDirectory, "file")).Close();
                    File.Delete(Path.Combine(SimulatorImageDirectory, "file"));
                }
                catch (Exception ex)
                {
                    Logger.Error(ex);
                    return false;
                }
                return true;
            }
            else
            {
                Logger.Error($"Invalid Simulation Images Directory: '{SimulatorImageDirectory}'");
                return false;
            }
        }
        return true;
    }

    /// <summary>
    /// Executes post-login tasks such as fetching data and establishing a WebSocket connection.
    /// </summary>
    private async void PostLogin(bool isLoggedIn_Monitor)
    {
        // Set the login data based on whether the login is for monitoring or control
        LoginData.accessLevel = isLoggedIn_Monitor ? "monitor" : "control";
        LoginData.token = Commands.Token; // Store the authentication token
        LoginData.id = Username; // Store the user's ID
        LoginData.state = "0"; // Set the login state to '0' indicating a successful login

        // Update the login status properties based on the login type
        IsLoggedIn_Monitor = isLoggedIn_Monitor;
        IsLoggedIn_Control = !isLoggedIn_Monitor;

        // Fetch and store the camera configuration, symbologies, and calibration data from the server
        _ = await UpdateConfigurationCamera();
        _ = await UpdateSymbologies();
        _ = await UpdateCalibration();
        _ = await UpdateJobs();
        _ = await UpdatePrint();
        _ = await UpdateProduct();

        //If the system is in simulator mode, adjust the simulation settings
        if (IsSimulator)
        {
            if (await UpdateSimulation())
                if (!UseSimulationDirectory)
                {
                    Simulation.mode = "trigger";
                    Simulation.dwellMs = 1;
                    _ = await Commands.PutSimulation(Simulation);
                }
                else
                {
                    Simulation.mode = "continuous";
                    Simulation.dwellMs = 1000;
                    _ = await Commands.PutSimulation(Simulation);
                }

            _ = await UpdateSimulation();
        }
        else
            Simulation = null;

        // Request the server to send extended data
        _ = await Commands.SetSendExtendedData(true);

        ConvertStandards(await CompileStandardTable());

        // Attempt to start the WebSocket connection for receiving node events
        if (!await WebSocket.StartAsync(Commands.URLs.WS_NodeEvents))
            return; // If the WebSocket connection cannot be started, exit the method
    }

    /// <summary>
    /// Logs out from the system, closes the WebSocket connection, and resets state.
    /// </summary>
    public async Task Logout()
    {
        await PreLogout();

        State = NodeStates.Offline;

        _ = await Commands.Logout();

        try
        {
            await WebSocket.StopAsync();
        }
        catch { }

        // Clear login data
        LoginData.accessLevel = "";
        LoginData.token = "";
        LoginData.id = "";
        LoginData.state = "1";

        IsLoggedIn_Control = false;
        IsLoggedIn_Monitor = false;

        // Reset all data models
        _ = UpdateProduct(true);
        _ = UpdateJobs(true);
        _ = UpdateJob(true);
        _ = UpdateConfigurationCamera(true);
        _ = UpdateSymbologies(true);
        _ = UpdateCalibration(true);
        _ = UpdateSimulation(true);
        _ = UpdatePrint(true);
    }

    /// <summary>
    /// Performs tasks before logging out, such as resetting simulator settings.
    /// </summary>
    private async Task PreLogout()
    {
        //If the system is in simulator mode, adjust the simulation settings
        if (IsSimulator)
        {
            // If the current simulation mode is 'continuous', change it to 'trigger' with a dwell time of 1ms
            if (await UpdateSimulation() && Simulation?.mode != "continuous")
            {
                Simulation.mode = "continuous";
                Simulation.dwellMs = 1000;
                _ = await Commands.PutSimulation(Simulation);
            }

            // Fetch the simulation settings again to ensure they have been updated correctly
            if (await UpdateSimulation() && Simulation.mode != "continuous")
            {
                // If the mode is not 'continuous', additional handling could be implemented here
            }
        }

        _ = await Commands.SetSendExtendedData(false);
    }

    #endregion

    #region Web API Update Methods

    /// <summary>
    /// Updates the <see cref="Devices"/> property by fetching data from the API.
    /// </summary>
    /// <param name="reset">If true, clears the existing data.</param>
    /// <returns>True if the update was successful and the data is valid.</returns>
    public async Task<bool> UpdateDevices(bool reset = false)
    {
        Results res;
        if (!reset && (res = await Commands.GetDevices()).OK)
        {
            DevicesJSON = res.Json;
            if (res.Object is Devices obj)
                Devices = obj;
        }
        else
        {
            DevicesJSON = string.Empty;
            Devices = null;
        }
        return IsDevicesValid;
    }

    /// <summary>
    /// Updates the <see cref="Inspection"/> property by fetching data from the API.
    /// </summary>
    /// <param name="reset">If true, clears the existing data.</param>
    /// <returns>True if the update was successful and the data is valid.</returns>
    public async Task<bool> UpdateInspection(bool reset = false)
    {
        Results res;
        if (!reset && (res = await Commands.GetInspection()).OK)
        {
            InspectionJSON = res.Json;
            if (res.Object is Inspection obj)
                Inspection = obj;
        }
        else
        {
            InspectionJSON = string.Empty;
            Inspection = null;
        }
        return IsInspectionValid;
    }

    private async Task<bool> UpdateProduct(bool reset = false)
    {
        Results res;
        if (!reset && (res = await Commands.GetProduct()).OK)
        {
            ProductJSON = res.Json;
            if (res.Object is Product obj)
                Product = obj;
        }
        else
        {
            ProductJSON = string.Empty;
            Product = null;
        }
        return IsProductValid;
    }

    private async Task<bool> UpdateJobs(bool reset = false)
    {
        Results res;
        if (!reset && (res = await Commands.GetJobs()).OK)
        {
            JobsJSON = res.Json;
            if (res.Object is Jobs obj)
                Jobs = obj;
        }
        else
        {
            JobsJSON = string.Empty;
            Jobs = null;
        }
        return IsJobValid;
    }

    private async Task<bool> UpdateJob(bool reset = false)
    {
        Results res;
        if (!reset && (res = await Commands.GetJob()).OK)
        {
            JobJSON = res.Json;
            if (res.Object is Job obj)
                Job = obj;

            if (IsJobValid)
                foreach (var sector in Job.sectors)
                {
                    if (sector.type == "blemish")
                    {
                        if ((await Commands.GetMask(sector.name)).Object is Job.Mask mask)
                            sector.blemishMask = mask;
                        else
                            Logger.Warning($"Failed to get mask for sector {sector.name}");
                    }
                }
        }
        else
        {
            JobJSON = string.Empty;
            Job = null;
        }
        return IsJobValid;
    }

    private async Task<bool> UpdateConfigurationCamera(bool reset = false)
    {
        Results res;
        if (!reset && (res = await Commands.GetCameraConfig()).OK)
        {
            ConfigurationCameraJSON = res.Json;
            if (res.Object is Configuration_Camera obj)
                ConfigurationCamera = obj;
        }
        else
        {
            ConfigurationCameraJSON = string.Empty;
            ConfigurationCamera = null;
        }
        return IsConfigurationCameraValid;
    }

    private async Task<bool> UpdateSymbologies(bool reset = false)
    {
        Results res;
        if (!reset && (res = await Commands.GetSymbologies()).OK)
        {
            SymbologiesJSON = res.Json;
            if (res.Object is List<Models.Symbologies.Symbol> obj)
                Symbologies = obj;
        }
        else
        {
            SymbologiesJSON = string.Empty;
            Symbologies = null;
        }
        return IsSymbologiesValid;
    }

    private async Task<bool> UpdateCalibration(bool reset = false)
    {
        Results res;
        if (!reset && (res = await Commands.GetCalibration()).OK)
        {
            CalibrationJSON = res.Json;
            if (res.Object is Calibration obj)
                Calibration = obj;
        }
        else
        {
            CalibrationJSON = string.Empty;
            Calibration = null;
        }
        return IsCalibrationValid;
    }

    private async Task<bool> UpdateSimulation(bool reset = false)
    {
        Results res;
        if (!reset && (res = await Commands.GetSimulation()).OK)
        {
            SimulationJSON = res.Json;
            if (res.Object is Simulation obj)
                Simulation = obj;
        }
        else
        {
            SimulationJSON = string.Empty;
            Simulation = null;
        }
        return IsSimulationValid;
    }

    private async Task<bool> UpdatePrint(bool reset = false)
    {
        Results res;
        if (!reset && (res = await Commands.GetPrint()).OK)
        {
            PrintJSON = res.Json;
            if (res.Object is Print obj)
                Print = obj;
        }
        else
        {
            PrintJSON = string.Empty;
            Print = null;
        }
        return IsPrintValid;
    }

    #endregion

    #region WebSocket Event Handlers

    /// <summary>
    /// Handles incoming WebSocket messages, deserializes them, and routes them to the appropriate handler.
    /// </summary>
    private void WebSocketEvents_MessageReceived(string message)
    {
        // The raw message has an extra character at the start and end that needs to be removed for valid JSON.
        string tmp;
        tmp = message.Remove(2, 15);
        tmp = tmp.Remove(tmp.LastIndexOf('}'), 1);

        var ev = JsonConvert.DeserializeObject<Events_System>(tmp) ?? new Events_System();
        if (ev.source == null)
            return;

        // Ignore system heartbeats as they are frequent and noisy.
        if (ev.source == "system" && ev.name == "heartbeat")
            return;

        switch (ev.name)
        {
            case "heartbeat":
                WebSocket_Heartbeat(ev);
                break;
            case "setupCapture":
                Logger.Debug($"WSE: setupCapture {ev.source}; {ev.name}");
                WebSocket_SetupCapture(ev);
                break;
            case "setupDetectBegin":
                Logger.Debug($"WSE: setupDetectBegin {ev.source}; {ev.name}");
                //WebSocket_SetupDetect(ev, false);
                break;
            case "setupDetectStart":
                Logger.Debug($"WSE: setupDetectStart {ev.source}; {ev.name}");
                break;
            case "setupDetect":
                Logger.Debug($"WSE: setupDetect {ev.source}; {ev.name}");
                break;
            case "setupDetectEnd":
                Logger.Debug($"WSE: setupDetectEnd {ev.source}; {ev.name}");
                WebSocket_SetupDetectEnd(ev);
                break;
            case "stateChange":
                Logger.Debug($"WSE: stateChange : {ev.source}; {ev.name}");
                WebSocket_StateChange(ev);
                break;
            case "sessionStateChange":
                Logger.Debug($"WSE: sessionStateChange {ev.source}; {ev.name}");
                WebSocket_SessionStateChange(ev);
                break;
            case "labelBegin":
                Logger.Debug($"WSE: labelBegin {ev.source}; {ev.name}");
                WebSocket_LabelStart(ev);
                break;
            case "labelEnd":
                Logger.Debug($"WSE: labelEnd {ev.source}; {ev.name}");
                WebSocket_LabelEnd(ev);
                break;
            case "sectorBegin":
                Logger.Debug($"WSE: sectorBegin {ev.source}; {ev.name}");
                break;
            case "sectorEnd":
                Logger.Debug($"WSE: sectorEnd {ev.source}; {ev.name}");
                break;
            default:
                Logger.Warning($"Unknown event type: {ev.name}");
                break;
        }
    }

    /// <summary>
    /// Handles the end of a setup detection event, processing learned sectors.
    /// </summary>
    private void WebSocket_SetupDetectEnd(Models.Events_System ev)
    {
        Logger.Debug($"SetupDetect: Controller State is? {State}: ActiveLabel is null? {ActiveLabel == null}: IsLoggedIn_Control? {IsLoggedIn_Control}: Event.Data is null? {ev.data == null}");

        if (State != NodeStates.Editing)
            return;

        if (ActiveLabel != null && IsLoggedIn_Control && ev.data! != null)
            ProcessLearn(ev.data.repeat, ActiveLabel.DesiredGS1Table.GetTableName(), ev);
    }

    /// <summary>
    /// Handles the start of a label event.
    /// </summary>
    private void WebSocket_LabelStart(Events_System ev) => LabelStart = true;

    /// <summary>
    /// Handles the end of a label event, invoking the callback with the repeat data.
    /// </summary>
    private void WebSocket_LabelEnd(Models.Events_System ev)
    {
        Logger.Debug($"LabelEnd: Controller State is? {State}: ActiveLabel is null? {ActiveLabel == null}: IsLoggedIn_Control? {IsLoggedIn_Control}: Event.Data is null? {ev.data == null}");

        if (ActiveLabel != null && IsLoggedIn_Control && ev.data! != null)
            RepeatAvailableCallBack(new Repeat(ev.data.repeat, ActiveLabel));

        ActiveLabel = null;
    }

    /// <summary>
    /// Handles heartbeat events, updating the node state and DPI.
    /// </summary>
    private void WebSocket_Heartbeat(Events_System? ev)
    {
        var state = GetState(ev.data.state);

        Dpi = ev != null ? (int)ev.data.Current_dpi : 0;

        if (State != state)
        {
            State = state;

            if (State != NodeStates.Idle)
            {
                _ = UpdateJob();
                return;
            }
        }
    }

    /// <summary>
    /// Handles state change events from the WebSocket.
    /// </summary>
    private void WebSocket_StateChange(Events_System ev)
    {
        if (ev != null)
        {
            if (ev.data == null)
            {
                WebSocket_Heartbeat(null);
                return;
            }

            ev.data.state = ev.data.toState;
            WebSocket_Heartbeat(ev);
        }
        else
            WebSocket_Heartbeat(null);

        // Clear the repeat buffer when entering editing mode or starting a new run.
        if (ev != null)
            if (ev.data.toState == "editing" || (ev.data.toState == "running" && ev.data.fromState != "paused"))
                RepeatBuffer.Clear();
    }

    /// <summary>
    /// Handles session state changes, such as another user taking control.
    /// </summary>
    private void WebSocket_SessionStateChange(Events_System ev)
    {
        // If another control session is started with a different token, log out the current session.
        if (ev.data.state == "0")
            if (ev.data.accessLevel == "control")
                if (LoginData.accessLevel == "control")
                    if (ev.data.token != LoginData.token)
                        _ = Logout();
    }

    /// <summary>
    /// Handles the setup capture event, which occurs when a new image is captured.
    /// </summary>
    private void WebSocket_SetupCapture(Events_System ev)
    {
        Logger.Debug($"SetupCapture: Controller State is? {State}: ActiveLabel is null? {ActiveLabel == null}: IsLoggedIn_Control? {IsLoggedIn_Control}: Event.Data is null? {ev.data == null}");

        if (State != NodeStates.Editing)
            return;

        if (ActiveLabel != null && IsLoggedIn_Control && ev.data! != null)
            _ = ProcessLabel(ev.data.repeat, ActiveLabel);
    }

    #endregion

    #region Public Methods

    #region Job and State Management

    /// <summary>
    /// Compiles a dictionary of grading standards available on the system.
    /// </summary>
    public async Task<Dictionary<(string symbol, string type), List<(string standard, string table)>>> CompileStandardTable()
    {
        Dictionary<(string symbol, string type), List<(string standard, string table)>> dict = [];

        var res = await Commands.GetGradingStandards();
        if (!res.OK)
            return dict;

        foreach (var gs in ((GradingStandards)res.Object).gradingStandards)
        {

            if (!dict.ContainsKey((gs.symbology, gs.symbolType)))
                dict.Add((gs.symbology, gs.symbolType), []);
            dict[(gs.symbology, gs.symbolType)].Add((gs.standard, gs.tableId));

        }
        return dict;
    }

    /// <summary>
    /// Changes the currently loaded job on the system.
    /// </summary>
    /// <param name="name">The name of the job to load.</param>
    public async Task ChangeJob(string name)
    {
        if ((await Commands.UnloadJob()).OK)
            if ((await Commands.LoadJob(name)).OK)
            {
                // Optionally, switch to edit mode after loading.
                //await SwitchToEdit();
            }
    }

    /// <summary>
    /// Toggles the print functionality on or off.
    /// </summary>
    /// <param name="enable">True to enable printing, false to disable.</param>
    public async Task TogglePrint(bool enable)
    {
        if (!IsSimulator)
        {
            if (await UpdatePrint() && IsBackupVoid && enable)
            {
                if (Print.enabled)
                {
                    Print.enabled = false;
                    _ = await Commands.Print(Print);
                    Thread.Sleep(100);
                }
            }

            if (await UpdatePrint())
                if (Print.enabled != enable)
                {
                    Print.enabled = enable;
                    _ = await Commands.Print(Print);
                }

            _ = await UpdatePrint();
        }
        else
            _ = await SimulatorTogglePrint();
    }

    /// <summary>
    /// Switches the system to edit mode.
    /// </summary>
    /// <returns>True if the system is in edit mode.</returns>
    public async Task<bool> SwitchToEdit()
    {
        if (State == NodeStates.Idle)
            return false;

        if (State == NodeStates.Editing)
            return true;

        if (!(await Commands.StopJob()).OK)
            return false;

        // Wait for the state to change to Editing.
        await Task.Run(() =>
        {
            var start = DateTime.Now;
            while (State != NodeStates.Editing)
                if ((DateTime.Now - start) > TimeSpan.FromMilliseconds(10000))
                    return;
        });

        return State == NodeStates.Editing;
    }

    /// <summary>
    /// Switches the system to run mode.
    /// </summary>
    /// <returns>True if the system is in run mode.</returns>
    public async Task<bool> SwitchToRun()
    {
        if (State == NodeStates.Idle)
            return false;

        if (State == NodeStates.Running)
            return true;

        if (string.IsNullOrEmpty(JobName))
            return false;

        var rr = await Commands.GetIsRunReady();
        if (!rr.OK || (string?)rr.Object != "OK")
            return false;

        if (!(await Commands.RunJob(JobName)).OK)
            return false;

        if (!(await Commands.StartJob()).OK)
            return false;

        // Wait for the state to change to Running.
        await Task.Run(() =>
        {
            var start = DateTime.Now;
            while (State != NodeStates.Running)
                if ((DateTime.Now - start) > TimeSpan.FromMilliseconds(30000))
                    return;
        });

        return State == NodeStates.Running;
    }

    #endregion

    #region Inspection, Reports, and Repeats

    /// <summary>
    /// Removes the latest available repeat from the job.
    /// </summary>
    public async Task<bool> RemoveRepeat()
    {
        var repeat = await GetLatestRepeatNumber();
        return repeat == -9999 || ((await Commands.RemoveRepeat(repeat)).OK && (await Commands.ResumeJob()).OK);
    }

    /// <summary>
    /// Triggers an inspection for a specific repeat number.
    /// </summary>
    /// <param name="repeat">The repeat number to inspect. If 0, the system decides.</param>
    public async Task<bool> Inspect(int repeat)
    {
        if (repeat > 0)
            if (!(await Commands.SetRepeat(repeat)).OK)
                return false;

        return (await Commands.Inspect()).OK;
    }

    /// <summary>
    /// Retrieves a full report for a given repeat, optionally including the image.
    /// </summary>
    /// <param name="repeat">The repeat number. If 0, the latest is used.</param>
    /// <param name="getImage">Whether to include the image in the report.</param>
    /// <returns>A <see cref="FullReport"/> object.</returns>
    public async Task<FullReport> GetFullReport(int repeat, bool getImage)
    {
        if (repeat == 0)
            repeat = await GetLatestRepeatNumber();

        var res = State == NodeStates.Editing ? await Commands.GetReport() : await Commands.GetReport(repeat);

        FullReport report = new();
        if (res == null || !res.OK)
            return report;

        report.Report = JObject.Parse(res.Json);

        if (getImage)
            if ((report.Image = (await Commands.GetRepeatsImage(repeat)).Object as byte[]) != null)
            {
                report.Image = AddDPIToBitmap(report.Image, Dpi);
            }

        if (await UpdateJob())
        {
            report.Job = JObject.Parse(JobJSON);
            report.Job["jobVersion"] = Version;
        }

        return report;
    }

    /// <summary>
    /// Triggers an inspection and retrieves the full report.
    /// </summary>
    /// <param name="repeat">The repeat number. If 0, the latest is used.</param>
    /// <param name="getImage">Whether to include the image in the report.</param>
    /// <returns>A <see cref="FullReport"/> object, or null on failure.</returns>
    public async Task<FullReport?> InspectGetReport(int repeat, bool getImage)
    {
        if (repeat == 0)
        {
            var res = State == NodeStates.Running ? (await Commands.GetRepeatsAvailableRun()).Object as List<int> : (await Commands.GetRepeatsAvailable()).Object as List<int>;
            repeat = res == null ? 0 : res.Count > 0 ? res.First() : 0;
        }

        if (State == NodeStates.Editing && repeat != 0)
            if (!await Inspect(repeat))
                return null;

        FullReport report;
        if ((report = await GetFullReport(repeat, getImage)) == null)
            return null;

        if ((report.Job = JObject.Parse((await Commands.GetJob()).Json)) == null)
            return null;

        if (State == NodeStates.Paused)
        {
            if (!(await Commands.RemoveRepeat(repeat)).OK)
                return null;

            if (!(await Commands.ResumeJob()).OK)
                return null;
        }
        return report;
    }

    /// <summary>
    /// Gets the number of the most recent repeat available.
    /// </summary>
    public async Task<int> GetLatestRepeatNumber()
    {
        var res = State == NodeStates.Running ? (await Commands.GetRepeatsAvailableRun()).Object as List<int> : (await Commands.GetRepeatsAvailable()).Object as List<int>;
        return res == null ? 0 : res.Count > 0 ? res.First() : 0;
    }

    #endregion

    #region Sector Management

    /// <summary>
    /// Deletes all sectors from the current job.
    /// </summary>
    public async Task<bool> DeleteSectors()
    {
        if (!await UpdateJob())
            return false;

        foreach (var sec in Job.sectors)
            if (!(await Commands.DeleteSector(sec.name)).OK)
                return false;
        return true;
    }

    /// <summary>
    /// Initiates sector detection on the system.
    /// </summary>
    public async Task<bool> DetectSectors() => await Commands.GetDetect() != null && (await Commands.Detect()).OK;

    /// <summary>
    /// Adds a new sector to the job.
    /// </summary>
    /// <param name="name">The name of the sector.</param>
    /// <param name="json">The JSON definition of the sector.</param>
    public async Task<bool> AddSector(string name, string json) => (await Commands.AddSector(name, json)).OK;

    /// <summary>
    /// Adds a blemish mask to a sector.
    /// </summary>
    /// <param name="name">The name of the sector.</param>
    /// <param name="json">The JSON definition of the mask layer.</param>
    public async Task<bool> AddMask(string name, string json) => (await Commands.AddMask(name, json)).OK;

    #endregion

    #region Simulator and Printing

    /// <summary>
    /// Toggles the simulator's printing/triggering mechanism.
    /// </summary>
    public async Task<bool> SimulatorTogglePrint()
    {
        LabelStart = false;

        if (!(await Commands.SimulatorStart()).OK)
            return false;

        // Wait for the label start event.
        await Task.Run(() =>
        {
            var start = DateTime.Now;
            while (!LabelStart)
                if ((DateTime.Now - start) > TimeSpan.FromMilliseconds(10000))
                    return;
        });

        return (await Commands.SimulatorStop()).OK && LabelStart;
    }

    /// <summary>
    /// Processes a label for printing.
    /// </summary>
    /// <param name="label">The label to process.</param>
    /// <param name="count">The number of copies to print.</param>
    /// <param name="printerName">The name of the target printer.</param>
    public async Task<bool> ProcessLabel_Printer(Label label, int count, string printerName) => await Task.Run(() =>
    {
        if (label.Image == null)
        {
            Logger.Error("Can not print a null image.");
            return false;
        }
        ActiveLabel = label;

        ProcessImage_Print(label.Image, count, printerName);

        return true;
    });

    /// <summary>
    /// Processes a label for the simulator.
    /// </summary>
    /// <param name="label">The label to process.</param>
    public async Task<bool> ProcessLabel_Simulator(Label label)
    {
        ActiveLabel = label;

        // Use the API for remote hosts, or the filesystem for localhost.
        if (!UseSimulationDirectory)
            return await ProcessImage_API();
        else
        {
            if (!ProcessImage_FileSystem())
                return false;

            if (!IsLoggedIn_Control)
            {
                if (!(await Commands.SimulationTrigger()).OK)
                {
                    Logger.Error("Error triggering the simulator.");
                    return false;
                }
            }
            else
            {
                if (!await SimulatorTogglePrint())
                {
                    Logger.Error("Error triggering the simulator.");
                    return false;
                }
            }

            return true;
        }
    }

    /// <summary>
    /// Processes a label for inspection.
    /// </summary>
    /// <param name="repeat">The repeat number associated with the label.</param>
    /// <param name="label">The label to process.</param>
    public async Task ProcessLabel(int repeat, Label? label)
    {
        if (label == null)
        {
            Logger.Error("The label is null.");
            return;
        }

        ActiveLabel = label;

        if (repeat == 0)
            repeat = await GetLatestRepeatNumber();

        if (repeat > 0)
            if (!(await Commands.SetRepeat(repeat)).OK)
            {
                Logger.Error("Error setting the repeat.");
                return;
            }

        if (label.Handler is not LabelHandlers.CameraTrigger && label.Handler is not LabelHandlers.SimulatorTrigger)
        {
            var res = await DetectRestoreSectors(label);

            if (res != RestoreSectorsResults.Success)
                return;
        }

        if (!await Inspect(repeat))
        {
            Logger.Error("Error inspecting the repeat.");
            return;
        }
    }

    #endregion

    #region Reading and Data Creation

    /// <summary>
    /// Creates a list of new verification sectors based on detection events.
    /// </summary>
    /// <param name="ev">The system event containing detection data.</param>
    /// <param name="tableID">The grading standard table ID to apply.</param>
    /// <param name="symbologies">The list of available symbologies.</param>
    /// <returns>A list of new sectors.</returns>
    public List<Sector_New_Verify> CreateSectors(Events_System ev, string tableID, List<Models.Symbologies.Symbol> symbologies)
    {
        int d1 = 1, d2 = 1; // Counters for 1D and 2D barcodes
        List<Sector_New_Verify> lst = [];

        if (ev?.data != null && ev.data.detections != null)
            foreach (var val in ev.data.detections)
            {
                if (val.region == null || string.IsNullOrEmpty(val.symbology))
                    continue;

                var sym = val.symbology.GetSymbology(BarcodeVerification.lib.Common.Devices.V275);
                var sym1 = symbologies.Find((e) => e.symbology == val.symbology);

                if (sym1 == null)
                    continue;

                // Ensure the detected region type matches the symbology type (1D vs 2D)
                if (sym.GetSymbologySpecificationType(BarcodeVerification.lib.Common.Devices.V275) == SymbologySpecificationTypes.D1)
                {
                    if (sym1.regionType != "verify1D")
                        continue;
                }
                else
                {
                    if (sym1.regionType != "verify2D")
                        continue;
                }

                Sector_New_Verify verify = new();

                if (tableID != "Unknown")
                {
                    verify.gradingStandard.enabled = true;
                    verify.gradingStandard.tableId = tableID;
                }
                else
                {
                    verify.gradingStandard.enabled = false;
                    verify.gradingStandard.tableId = "1";
                }

                verify.id = sym.GetSymbologySpecificationType(BarcodeVerification.lib.Common.Devices.V275) == SymbologySpecificationTypes.D1 ? d1++ : d2++;
                verify.type = sym.GetSymbologySpecificationType(BarcodeVerification.lib.Common.Devices.V275) == SymbologySpecificationTypes.D1 ? "verify1D" : "verify2D";
                verify.symbology = val.symbology;
                verify.name = $"{verify.type}_{verify.id}";
                verify.username = $"{char.ToUpper(verify.name[0])}{verify.name[1..]}";

                verify.top = val.region.y;
                verify.left = val.region.x;
                verify.height = val.region.height;
                verify.width = val.region.width;

                verify.orientation = val.orientation;

                lst.Add(verify);
            }

        return lst;
    }

    /// <summary>
    /// Reads the full report for a given repeat.
    /// </summary>
    public async Task<bool> ReadTask(Repeat repeat)
    {
        if ((repeat.FullReport = await GetFullReport(repeat.Number, true)) == null)
        {
            Logger.Error("Unable to read the repeat report from the node.");
            return false;
        }

        return true;
    }

    /// <summary>
    /// Reads the full report for a given repeat number.
    /// </summary>
    /// <param name="repeat">The repeat number. Use -1 for the latest.</param>
    public async Task<FullReport?> ReadTask(int repeat)
    {
        repeat = repeat == -1 ? await GetLatestRepeatNumber() : repeat;

        FullReport report;
        if ((report = await GetFullReport(repeat, true)) == null)
        {
            Logger.Error("Unable to read the repeat report from the node.");
            return report;
        }

        return report;
    }

    #endregion

    #endregion

    #region Private Helper Methods

    private void ConvertStandards(Dictionary<(string symbol, string type), List<(string standard, string table)>> dict)
    {
        Dictionary<BarcodeVerification.lib.Common.Symbologies, List<GS1Tables>> standards = [];
        foreach (var standard in dict.Keys)
        {
            var data = standard.type.GetSymbology(BarcodeVerification.lib.Common.Devices.V275);

            if (!standards.ContainsKey(data))
                standards.Add(data, []);

            foreach (var table in dict[standard])
            {
                var tableData = table.table.GetGS1Table(BarcodeVerification.lib.Common.Devices.V275);
                standards[data].Add(tableData);
            }
        }

        File.WriteAllText("V275_GS1Tables.json", JsonConvert.SerializeObject(standards, Formatting.Indented));
    }

    /// <summary>
    /// Converts a string state from the API to a <see cref="NodeStates"/> enum.
    /// </summary>
    private NodeStates GetState(string state) => state switch
    {
        "idle" => NodeStates.Idle,
        "editing" => NodeStates.Editing,
        "running" => NodeStates.Running,
        "paused" => NodeStates.Paused,
        "disconnected" => NodeStates.Disconnected,
        _ => NodeStates.Offline,
    };

    /// <summary>
    /// Embeds DPI information into a bitmap image byte array.
    /// </summary>
    private byte[] AddDPIToBitmap(byte[] image, int dpi)
    {
        var dpiXInMeters = Dpi2Dpm(dpi);
        var dpiYInMeters = Dpi2Dpm(dpi);
        // The horizontal and vertical resolution fields are at byte offsets 38 and 42 in the BMP header.
        // Set the horizontal DPI
        for (var i = 38; i < 42; i++)
            image[i] = BitConverter.GetBytes(dpiXInMeters)[i - 38];
        // Set the vertical DPI
        for (var i = 42; i < 46; i++)
            image[i] = BitConverter.GetBytes(dpiYInMeters)[i - 42];
        return image;
    }

    /// <summary>
    /// Converts dots per inch (DPI) to dots per meter (DPM).
    /// </summary>
    public static int Dpi2Dpm(int dpi) => (int)Math.Round(dpi * InchesPerMeter);

    private void ProcessImage_Print(byte[] image, int count, string printerName) => Task.Run(async () =>
    {
        Printer.Controller printer = new();
        printer.Print(image, count, printerName, "");

        if (IsLoggedIn_Control)
            await TogglePrint(true);
    });

    private async Task<bool> ProcessImage_API()
    {
        if (ActiveLabel == null)
        {
            Logger.Error($"The image is null.");
            return false;
        }

        if (!(await Commands.SimulationTriggerImage(
            new SimulationTrigger()
            {
                image = ActiveLabel.Image,
                dpi = (uint)ActiveLabel.Dpi
            })).OK)
        {
            Logger.Error("Error triggering the simulator.");
            return false;
        }

        return true;
    }

    private bool ProcessImage_FileSystem()
    {
        try
        {
            var verRes = 1;
            var prepend = "";

            Simulator.SimulatorFileHandler sim = new(SimulatorImageDirectory);

            if (!sim.DeleteAllImages())
            {
                if (System.Version.TryParse(Version, out var ver))
                {
                    var verMin = System.Version.Parse("1.1.0.3009");
                    verRes = ver.CompareTo(verMin);
                }

                if (verRes > 0)
                {
                    Logger.Error("Could not delete all simulator images. The version is less than 1.1.0.3009 which does not allow the first image to be deleted.");
                    return false;
                }
                else
                {
                    sim.UpdateImageList();
                    prepend = "_";
                    foreach (var imgFile in sim.Images)
                    {
                        var name = Path.GetFileName(imgFile);
                        while (name.StartsWith(prepend))
                        {
                            prepend += "_";
                        }
                    }
                }
            }

            if (!sim.SaveImage(prepend + "simulatorImage.bmp", ActiveLabel.Image))
            {
                Logger.Error("Could not copy the image to the simulator images directory.");
                return false;
            }

            return true;

        }
        catch (Exception ex)
        {
            Logger.Error(ex.Message);
            return false;
        }
    }

    private async void ProcessLearn(int repeat, string gs1TableName, Events_System ev)
    {
        var sectors = CreateSectors(ev, gs1TableName, Symbologies);

        Logger.Info("Creating sectors.");

        foreach (var sec in sectors)
            if (!await AddSector(sec.name, JsonConvert.SerializeObject(sec)))
                continue;

        if (!await Inspect(repeat))
        {
            Logger.Error("Error inspecting the repeat.");
            return;
        }
    }

    private async Task<RestoreSectorsResults> DetectRestoreSectors(Label label)
    {
        if (!await DeleteSectors())
            return RestoreSectorsResults.Failure;

        if (label.Handler is LabelHandlers.CameraDetect or LabelHandlers.SimulatorDetect)
            return !await DetectSectors() ? RestoreSectorsResults.Failure : RestoreSectorsResults.Detect;

        if (label.Sectors == null)
            return RestoreSectorsResults.Success;

        foreach (var sec in label.Sectors)
        {
            if (!await AddSector(sec["name"].ToString(), JsonConvert.SerializeObject(sec)))
                return RestoreSectorsResults.Failure;

            if (sec["blemishMask"]?["layers"] != null)
                foreach (var layer in sec["blemishMask"]["layers"])

                    if (!await AddMask(sec["name"].ToString(), JsonConvert.SerializeObject(layer)))
                        if (layer["value"].Value<int>() != 0)
                            return RestoreSectorsResults.Failure;
        }

        return RestoreSectorsResults.Success;
    }

    private async void RepeatAvailableCallBack(Repeat repeat)
    {
        if (repeat == null)
        {
            Logger.Error("The repeat is null.");
            return;
        }

        repeat.FullReport = await GetFullReport(repeat.Number, true);
        if (repeat.FullReport == null)
        {
            Logger.Error("Unable to read the repeat report from the node.");
            return;
        }
        if (repeat.FullReport.Job == null)
        {
            Logger.Error("The job is null.");
            return;
        }

        repeat.FullReport.Job["jobVersion"] = Version;

        repeat.Label.RepeatAvailable?.Invoke(repeat);
    }

    #endregion
}