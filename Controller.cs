using BarcodeVerification.lib.Common;
using BarcodeVerification.lib.GS1;
using CommunityToolkit.Mvvm.ComponentModel;
using Logging.lib;
using Newtonsoft.Json;
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

namespace V275_REST_Lib;

public class Label
{
    public byte[] Image { get; set; }
    public List<Job.Sector> Sectors { get; set; }
    public AvailableTables? Table { get; set; }
    public int Dpi { get; set; }

    public Action<Repeat> RepeatAvailable { get; set; }
    /// <summary>
    /// 
    /// </summary>
    /// <param name="image"></param>
    /// <param name="dpi">Must be set if using the simulator API.</param>
    /// <param name="sectors">If null, ignore. If empty, auto detect. If not empty, restore.</param>
    /// <param name="table"></param>
    public Label(Action<Repeat> repeatAvailable, byte[] image, int dpi, List<Job.Sector> sectors, AvailableTables? table = null)
    {
        RepeatAvailable = repeatAvailable;
        Table = table;
        Dpi = dpi;
        Image = image;
        Sectors = sectors;
    }
    public Label() { }
}

public class FullReport
{
    public Report? Report { get; set; }
    public Job? Job { get; set; }
    public byte[]? Image { get; set; }
}

public class Repeat
{
    public string Version { get; set; }
    public int Number { get; set; } = -1;
    public FullReport? FullReport { get; set; }
    public Label Label { get; set; }
    public Events_System? SetupDetectEvent { get; set; }

    public Repeat(int number, Label label, string version)
    {
        Number = number;
        Label = label;
        Version = version;
    }
}

[JsonObject(MemberSerialization.OptIn)]
public partial class Controller : ObservableObject
{
    private static readonly SynchronizationContext OwnerContext = SynchronizationContext.Current;

    public const double InchesPerMeter = 39.3701;
    public enum RestoreSectorsResults
    {
        Success = 1,
        Detect = 2,
        Failure = -1
    }

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

    public Commands Commands { get; } = new Commands();
    public WebSocketEvents WebSocket { get; } = new WebSocketEvents();

    private ConcurrentDictionary<int, Repeat> RepeatBuffer { get; } = [];

    [ObservableProperty][property: JsonProperty] private string host;
    partial void OnHostChanged(string value) { Commands.URLs.Host = value; }

    [ObservableProperty][property: JsonProperty] private uint systemPort;
    partial void OnSystemPortChanged(uint value) { Commands.URLs.SystemPort = value; }

    [ObservableProperty][property: JsonProperty] private uint nodeNumber;
    partial void OnNodeNumberChanged(uint value) { Commands.URLs.NodeNumber = value; }

    [ObservableProperty][property: JsonProperty] private string userName;
    [ObservableProperty][property: JsonProperty] private string password;
    [ObservableProperty][property: JsonProperty] private string simulatorImageDirectory;

    [ObservableProperty] private NodeStates state = NodeStates.Offline;

    [ObservableProperty] private bool isLoggedIn_Monitor = false;
    partial void OnIsLoggedIn_MonitorChanged(bool value) { OnPropertyChanged(nameof(IsLoggedIn)); OnPropertyChanged(nameof(IsNotLoggedIn)); }

    [ObservableProperty] private bool isLoggedIn_Control = false;
    public bool IsNotLoggedIn_Control => !IsLoggedIn_Control;
    partial void OnIsLoggedIn_ControlChanged(bool value) { OnPropertyChanged(nameof(IsLoggedIn)); OnPropertyChanged(nameof(IsNotLoggedIn)); OnPropertyChanged(nameof(IsNotLoggedIn_Control)); }
    public bool IsLoggedIn => IsLoggedIn_Monitor || IsLoggedIn_Control;
    public bool IsNotLoggedIn => !(IsLoggedIn_Monitor || IsLoggedIn_Control);

    [ObservableProperty] private Events_System.Data loginData = new();

    #region System API Properties

    [ObservableProperty] private Devices? devices;
    partial void OnDevicesChanging(Devices? value) => IsDevicesValid = value != null && value is Devices;
    partial void OnDevicesChanged(Devices? value)
    {
        OnPropertyChanged(nameof(Node));
        OnPropertyChanged(nameof(Camera));
    }
    [ObservableProperty] private string devicesJSON;
    [ObservableProperty] private bool isDevicesValid;

    public Devices.Node? Node => Devices?.nodes?.FirstOrDefault((n) => n.enumeration == NodeNumber);
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

    [ObservableProperty] private string productJSON;
    [ObservableProperty] private bool isProductValid;
    [ObservableProperty] private string version = "----";

    #endregion

    [ObservableProperty] private Inspection? inspection;
    partial void OnInspectionChanging(Inspection? value) => IsInspectionValid = value != null && value is Inspection;
    partial void OnInspectionChanged(Inspection? value)
    {
        OnPropertyChanged(nameof(IsSimulator));
    }
    [ObservableProperty] private string inspectionJSON;
    [ObservableProperty] private bool isInspectionValid;

    [ObservableProperty] private Jobs? jobs;
    partial void OnJobsChanging(Jobs? value) => IsJobsValid = value != null && value is Jobs;
    [ObservableProperty] private string jobsJSON;
    [ObservableProperty] private bool isJobsValid;

    [ObservableProperty] private bool isOldISO;

    [ObservableProperty] private Configuration_Camera? configurationCamera;
    partial void OnConfigurationCameraChanging(Configuration_Camera? value) => IsConfigurationCameraValid = value != null && value is Configuration_Camera;
    partial void OnConfigurationCameraChanged(Configuration_Camera? value)
    {
        OnPropertyChanged(nameof(IsBackupVoid));
    }
    [ObservableProperty] private string configurationCameraJSON;
    [ObservableProperty] private bool isConfigurationCameraValid;

    [ObservableProperty] private List<Symbologies.Symbol>? symbologies;
    partial void OnSymbologiesChanging(List<Symbologies.Symbol>? value) => IsSymbologiesValid = value != null && value is List<Symbologies.Symbol>;
    [ObservableProperty] private string symbologiesJSON;
    [ObservableProperty] private bool isSymbologiesValid;

    [ObservableProperty] private Calibration? calibration;
    partial void OnCalibrationChanging(Calibration? value) => IsCalibrationValid = value != null && value is Calibration;
    [ObservableProperty] private string calibrationJSON;
    [ObservableProperty] private bool isCalibrationValid;

    [ObservableProperty] private Simulation? simulation;
    partial void OnSimulationChanging(Simulation? value) => IsSimulationValid = value != null && value is Simulation;
    [ObservableProperty] private string simulationJSON;
    [ObservableProperty] private bool isSimulationValid;

    [ObservableProperty] private Print? print;
    partial void OnPrintChanging(Print? value) => IsPrintValid = value != null && value is Print;
    [ObservableProperty] private string printJSON;
    [ObservableProperty] private bool isPrintValid;

    [ObservableProperty] private Job? job;
    partial void OnJobChanging(Job? value) => IsJobValid = value != null && value is Job;
    partial void OnJobChanged(Job? value)
    {
        OnPropertyChanged(nameof(JobName));
    }
    [ObservableProperty] private string jobJSON;
    [ObservableProperty] private bool isJobValid;

    [ObservableProperty] private int dpi = 600;
    partial void OnDpiChanged(int value) => OnPropertyChanged(nameof(Is600Dpi));
    public bool Is600Dpi => Dpi is <= 0 or 600;

    public bool IsSimulator => IsInspectionValid && Inspection.device.Equals("simulator");
    public bool IsBackupVoid => IsConfigurationCameraValid && ConfigurationCamera?.backupVoidMode?.value == "ON";
    public string? JobName => IsJobValid ? Job?.name : "";

    private bool LabelStart;

    public Controller(string host, uint port, uint nodeNumber, string userName, string password, string dir)
    {
        Host = host;
        SystemPort = port;
        NodeNumber = nodeNumber;
        UserName = userName;
        Password = password;
        SimulatorImageDirectory = dir;

        WebSocket.MessageRecieved += WebSocketEvents_MessageRecieved;
    }
    public void Initialize() => Task.Run(async () =>
                                         {
                                             _ = await UpdateDevices();
                                             _ = await UpdateInspection();
                                         }).Wait();

    public async Task Login(bool monitor)
    {
        if (IsLoggedIn)
        {
            Logger.LogDebug($"Logging out. {UserName} @ {Host}:{SystemPort}");
            await Logout();
            return;
        }

        if (!PreLogin())
        {
            Logger.LogDebug($"Pre-Log in FAILED. {UserName} @ {Host}:{SystemPort}");
            return;
        }

        Logger.LogDebug($"Logging in. {UserName} @ {Host}:{SystemPort}");

        if ((await Commands.Login(UserName, Password, monitor)).OK)
        {
            Logger.LogDebug($"Logged in. {(monitor ? "Monitor" : "Control")} {UserName} @ {Host}:{SystemPort}");

            PostLogin(monitor);
        }
        else
        {
            Logger.LogError($"Login FAILED. {UserName} @ {Host}:{SystemPort}");

            IsLoggedIn_Control = false;
            IsLoggedIn_Monitor = false;
        }
    }
    private bool PreLogin()
    {
        if (IsSimulator && Host.Equals("127.0.0.1"))
        {
            if (Directory.Exists(SimulatorImageDirectory))
            {
                try
                {
                    File.Create(Path.Combine(SimulatorImageDirectory, "file")).Close();
                    File.Delete(Path.Combine(SimulatorImageDirectory, "file"));
                }
                catch (Exception ex)
                {
                    Logger.LogError(ex);
                    return false;
                }
                return true;
            }
            else
            {
                Logger.LogError($"Invalid Simulation Images Directory: '{SimulatorImageDirectory}'");
                return false;
            }
        }
        return true;
    }
    private async void PostLogin(bool isLoggedIn_Monitor)
    {
        // Set the login data based on whether the login is for monitoring or control
        LoginData.accessLevel = isLoggedIn_Monitor ? "monitor" : "control";
        LoginData.token = Commands.Token; // Store the authentication token
        LoginData.id = UserName; // Store the user's ID
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
                if (!Host.Equals("127.0.0.1"))
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

    private void ConvertStandards(Dictionary<(string symbol, string type), List<(string standard, string table)>> dict)
    {
        Dictionary<AvailableSymbologies, List<AvailableTables>> standards = [];
        foreach ((string symbol, string type) standard in dict.Keys)
        {
            AvailableSymbologies data = standard.type.GetSymbology(AvailableDevices.V275);

            if (!standards.ContainsKey(data))
                standards.Add(data, []);

            foreach ((string standard, string table) table in dict[standard])
            {
                AvailableTables tableData = table.table.GetTable(AvailableDevices.V275);
                standards[data].Add(tableData);
            }
        }

        File.WriteAllText("V275_GS1Tables.json", JsonConvert.SerializeObject(standards, Formatting.Indented));
    }

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

        LoginData.accessLevel = "";
        LoginData.token = "";
        LoginData.id = "";
        LoginData.state = "1";

        IsLoggedIn_Control = false;
        IsLoggedIn_Monitor = false;

        //_ = await UpdateDevices(true);
        //_ = await UpdateInspection(true);
        _ = UpdateProduct(true);

        _ = UpdateJobs(true);
        _ = UpdateJob(true);
        _ = UpdateConfigurationCamera(true);
        _ = UpdateSymbologies(true);
        _ = UpdateCalibration(true);
        _ = UpdateSimulation(true);
        _ = UpdatePrint(true);
    }
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
                foreach (Job.Sector sector in Job.sectors)
                {
                    if (sector.type == "blemish")
                    {
                        if ((await Commands.GetMask(sector.name)).Object is Job.Mask mask)
                            sector.blemishMask = mask;
                        else
                            Logger.LogWarning($"Failed to get mask for sector {sector.name}");
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
            if (res.Object is List<Symbologies.Symbol> obj)
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

    private void WebSocketEvents_MessageRecieved(string message)
    {
        string tmp;
        tmp = message.Remove(2, 15);
        tmp = tmp.Remove(tmp.LastIndexOf('}'), 1);

        Events_System ev = JsonConvert.DeserializeObject<Events_System>(tmp) ?? new Events_System();
        if (ev.source == null)
            return;

        if (ev.source == "system")
            if (ev.name == "heartbeat")
                return;

        switch (ev.name)
        {
            case "heartbeat":
                WebSocket_Heartbeat(ev);
                break;
            case "setupCapture":
                Logger.LogDebug($"WSE: setupCapture {ev.source}; {ev.name}");
                WebSocket_SetupCapture(ev);
                break;
            case "setupDetectBegin":
                Logger.LogDebug($"WSE: setupDetectBegin {ev.source}; {ev.name}");
                //WebSocket_SetupDetect(ev, false);
                break;
            case "setupDetectStart":
                Logger.LogDebug($"WSE: setupDetectStart {ev.source}; {ev.name}");
                break;
            case "setupDetect":
                Logger.LogDebug($"WSE: setupDetect {ev.source}; {ev.name}");
                break;
            case "setupDetectEnd":
                Logger.LogDebug($"WSE: setupDetectEnd {ev.source}; {ev.name}");
                WebSocket_SetupDetectEnd(ev);
                break;
            case "stateChange":
                Logger.LogDebug($"WSE: stateChange : {ev.source}; {ev.name}");
                WebSocket_StateChange(ev);
                break;
            case "sessionStateChange":
                Logger.LogDebug($"WSE: sessionStateChange {ev.source}; {ev.name}");
                WebSocket_SessionStateChange(ev);
                break;
            case "labelBegin":
                Logger.LogDebug($"WSE: labelBegin {ev.source}; {ev.name}");
                WebSocket_LabelStart(ev);
                break;
            case "labelEnd":
                Logger.LogDebug($"WSE: labelEnd {ev.source}; {ev.name}");
                WebSocket_LabelEnd(ev);
                break;
            case "sectorBegin":
                Logger.LogDebug($"WSE: sectorBegin {ev.source}; {ev.name}");
                break;
            case "sectorEnd":
                Logger.LogDebug($"WSE: sectorEnd {ev.source}; {ev.name}");
                break;
            default:
                Logger.LogWarning($"Unknown event type: {ev.name}");
                break;
        }
    }

    /// <summary>
    /// <see langword="this"/> occurs when the Setup Detection completes.
    /// The detected regions will be in the <see cref="Events_System.data"/> property.
    /// </summary>
    /// <param name="ev"></param>
    /// <param name="end"></param>
    private void WebSocket_SetupDetectEnd(Models.Events_System ev)
    {
        Logger.LogDebug($"SetupDetect: Controller State is? {State}: ActiveLabel is null? {ActiveLabel == null}: IsLoggedIn_Control? {IsLoggedIn_Control}: Event.Data is null? {ev.data == null}");

        if (State != NodeStates.Editing)
            return;

        if (ActiveLabel != null && IsLoggedIn_Control && ev.data! != null)
            ProcessLabel(ev.data.repeat, ActiveLabel, ev);
    }
    private void WebSocket_LabelStart(Events_System ev) => LabelStart = true;
    private void WebSocket_LabelEnd(Models.Events_System ev)
    {
        Logger.LogDebug($"LabelEnd: Controller State is? {State}: ActiveLabel is null? {ActiveLabel == null}: IsLoggedIn_Control? {IsLoggedIn_Control}: Event.Data is null? {ev.data == null}");

        if (ActiveLabel != null && IsLoggedIn_Control && ev.data! != null)
            RepeatAvailableCallBack(new Repeat(ev.data.repeat, ActiveLabel, Version));

        ActiveLabel = null;
    }
    private void WebSocket_Heartbeat(Events_System? ev)
    {
        NodeStates state = GetState(ev.data.state);

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

        if (ev != null)
            if (ev.data.toState == "editing" || (ev.data.toState == "running" && ev.data.fromState != "paused"))
                RepeatBuffer.Clear();
    }
    private void WebSocket_SessionStateChange(Events_System ev)
    {
        //if (ev.data.id == LoginData.id)
        if (ev.data.state == "0")
            if (ev.data.accessLevel == "control")
                if (LoginData.accessLevel == "control")
                    if (ev.data.token != LoginData.token)
                        _ = Logout();
    }
    /// <summary>
    /// A new image has been captured. After sim trigger or repeat image from slab.
    /// </summary>
    /// <param name="ev"></param>
    private void WebSocket_SetupCapture(Events_System ev)
    {
        Logger.LogDebug($"SetupCapture: Controller State is? {State}: ActiveLabel is null? {ActiveLabel == null}: IsLoggedIn_Control? {IsLoggedIn_Control}: Event.Data is null? {ev.data == null}");

        if (State != NodeStates.Editing)
            return;

        if (ActiveLabel != null && IsLoggedIn_Control && ev.data! != null)
            ProcessLabel(ev.data.repeat, ActiveLabel, null);
    }

    public async Task<Dictionary<(string symbol, string type), List<(string standard, string table)>>> CompileStandardTable()
    {
        Dictionary<(string symbol, string type), List<(string standard, string table)>> dict = [];

        Results res = await Commands.GetGradingStandards();
        if (!res.OK)
            return dict;

        foreach (GradingStandards.GradingStandard gs in ((GradingStandards)res.Object).gradingStandards)
        {

            if (!dict.ContainsKey((gs.symbology, gs.symbolType)))
                dict.Add((gs.symbology, gs.symbolType), []);
            dict[(gs.symbology, gs.symbolType)].Add((gs.standard, gs.tableId));

        }
        return dict;
    }

    private NodeStates GetState(string state)
    {
        if (state == "offline")
            return NodeStates.Offline;
        else return state == "idle"
            ? NodeStates.Idle
            : state == "editing"
            ? NodeStates.Editing
            : state == "running"
            ? NodeStates.Running
            : state == "paused" ? NodeStates.Paused : state == "disconnected" ? NodeStates.Disconnected : NodeStates.Offline;
    }
    public async Task ChangeJob(string name)
    {
        if ((await Commands.UnloadJob()).OK)
            if ((await Commands.LoadJob(name)).OK)
            {
                //await SwitchToEdit();
            }
    }
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
    public async Task<bool> RemoveRepeat()
    {
        int repeat = await GetLatestRepeatNumber();
        return repeat == -9999 || ((await Commands.RemoveRepeat(repeat)).OK && (await Commands.ResumeJob()).OK);
    }

    public async Task<bool> Inspect(int repeat)
    {
        if (repeat > 0)
            if (!(await Commands.SetRepeat(repeat)).OK)
                return false;

        //LabelEnd = false;
        return (await Commands.Inspect()).OK;
        //await Task.Run(() =>
        //{
        //    DateTime start = DateTime.Now;
        //    while (!LabelEnd)
        //        if ((DateTime.Now - start) > TimeSpan.FromMilliseconds(10000))
        //            return;
        //});

        //return LabelEnd;
    }

    public async Task<FullReport?> GetFullReport(int repeat, bool getImage)
    {
        if (repeat == 0)
            repeat = await GetLatestRepeatNumber();

        FullReport report = new();
        if (State == NodeStates.Editing)
        {
            if ((report.Report = (await Commands.GetReport()).Object as Report) == null)
                return null;
        }
        else
        {
            if ((report.Report = (await Commands.GetReport(repeat)).Object as Report) == null)
                return null;
        }

        if (getImage)
            if ((report.Image = (await Commands.GetRepeatsImage(repeat)).Object as byte[]) != null)
            {
                report.Image = AddDPIToBitmap(report.Image, Dpi);
            }

        if (await UpdateJob())
            report.Job = Job;

        return report;
    }
    private byte[] AddDPIToBitmap(byte[] image, int dpi)
    {
        int dpiXInMeters = Dpi2Dpm(dpi);
        int dpiYInMeters = Dpi2Dpm(dpi);
        // Set the horizontal DPI
        for (int i = 38; i < 42; i++)
            image[i] = BitConverter.GetBytes(dpiXInMeters)[i - 38];
        // Set the vertical DPI
        for (int i = 42; i < 46; i++)
            image[i] = BitConverter.GetBytes(dpiYInMeters)[i - 42];
        return image;
    }

    public static int Dpi2Dpm(int dpi) => (int)Math.Round(dpi * InchesPerMeter);

    public async Task<bool> DeleteSectors()
    {
        if (!await UpdateJob())
            return false;

        foreach (Job.Sector sec in Job.sectors)
            if (!(await Commands.DeleteSector(sec.name)).OK)
                return false;
        return true;
    }
    public async Task<bool> DetectSectors() => await Commands.GetDetect() != null && (await Commands.Detect()).OK;

    public async Task<bool> AddSector(string name, string json) => (await Commands.AddSector(name, json)).OK;
    public async Task<bool> AddMask(string name, string json) => (await Commands.AddMask(name, json)).OK;

    public async Task<bool> SwitchToEdit()
    {
        if (State == NodeStates.Idle)
            return false;

        if (State == NodeStates.Editing)
            return true;

        if (!(await Commands.StopJob()).OK)
            return false;

        await Task.Run(() =>
        {
            DateTime start = DateTime.Now;
            while (State != NodeStates.Editing)
                if ((DateTime.Now - start) > TimeSpan.FromMilliseconds(10000))
                    return;
        });

        return State == NodeStates.Editing;
    }
    public async Task<bool> SwitchToRun()
    {
        if (State == NodeStates.Idle)
            return false;

        if (State == NodeStates.Running)
            return true;

        if (string.IsNullOrEmpty(JobName))
            return false;

        Results rr = await Commands.GetIsRunReady();
        if (!rr.OK)
            return false;
        if ((string?)rr.Object != "OK")
            return false;

        if (!(await Commands.RunJob(JobName)).OK)
            return false;

        if (!(await Commands.StartJob()).OK)
            return false;

        await Task.Run(() =>
        {
            DateTime start = DateTime.Now;
            while (State != NodeStates.Running)
                if ((DateTime.Now - start) > TimeSpan.FromMilliseconds(30000))
                    return;
        });

        return State == NodeStates.Running;
    }

    public async Task<bool> SimulatorTogglePrint()
    {
        LabelStart = false;

        if (!(await Commands.SimulatorStart()).OK)
            return false;

        await Task.Run(() =>
        {
            DateTime start = DateTime.Now;
            while (!LabelStart)
                if ((DateTime.Now - start) > TimeSpan.FromMilliseconds(10000))
                    return;
        });

        return (await Commands.SimulatorStop()).OK && LabelStart;
    }

    public async Task<FullReport?> InspectGetReport(int repeat, bool getImage)
    {
        if (repeat == 0)
        {
            List<int>? res = State == NodeStates.Running ? (await Commands.GetRepeatsAvailableRun()).Object as List<int> : (await Commands.GetRepeatsAvailable()).Object as List<int>;
            repeat = res == null ? 0 : res.Count > 0 ? res.First() : 0;
        }

        if (State == NodeStates.Editing && repeat != 0)
            if (!await Inspect(repeat))
                return null;

        FullReport report;
        if ((report = await GetFullReport(repeat, getImage)) == null)
            return null;

        if ((report.Job = (await Commands.GetJob()).Object as Job) == null)
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
    public async Task<int> GetLatestRepeatNumber()
    {
        List<int>? res = State == NodeStates.Running ? (await Commands.GetRepeatsAvailableRun()).Object as List<int> : (await Commands.GetRepeatsAvailable()).Object as List<int>;
        return res == null ? 0 : res.Count > 0 ? res.First() : 0;
    }
    private Label? ActiveLabel { get; set; }

    public bool ProcessLabel_Printer(Label label, int count, string printerName)
    {
        ActiveLabel = label;

        ProcessImage_Print(label.Image, count, printerName);

        return true;
    }
    public async Task<bool> ProcessLabel_Simulator(Label label)
    {
        ActiveLabel = label;

        if (!Host.Equals("127.0.0.1"))
            return await ProcessImage_API();
        else
        {
            if (!ProcessImage_FileSystem())
                return false;

            if (!IsLoggedIn_Control)
            {
                if (!(await Commands.SimulationTrigger()).OK)
                {
                    Logger.LogError("Error triggering the simulator.");
                    return false;
                }
            }
            else
            {
                if (!await SimulatorTogglePrint())
                {
                    Logger.LogError("Error triggering the simulator.");
                    return false;
                }
            }

            return true;
        }
    }

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
            Logger.LogError($"The image is null.");
            return false;
        }

        if (!(await Commands.SimulationTriggerImage(
            new SimulationTrigger()
            {
                image = ActiveLabel.Image,
                dpi = (uint)ActiveLabel.Dpi
            })).OK)
        {
            Logger.LogError("Error triggering the simulator.");
            return false;
        }

        return true;
    }
    private bool ProcessImage_FileSystem()
    {
        try
        {
            int verRes = 1;
            string prepend = "";

            Simulator.SimulatorFileHandler sim = new(SimulatorImageDirectory);

            if (!sim.DeleteAllImages())
            {

                if (System.Version.TryParse(Version, out Version ver))
                {
                    Version verMin = System.Version.Parse("1.1.0.3009");
                    verRes = ver.CompareTo(ver);

                }

                if (verRes > 0)
                {
                    Logger.LogError("Could not delete all simulator images. The version is less than 1.1.0.3009 which does not allow the first image to be deleted.");
                    return false;
                }
                else
                {
                    sim.UpdateImageList();

                    prepend = "_";

                    foreach (string imgFile in sim.Images)
                    {
                        string name = Path.GetFileName(imgFile);

                        for (; ; )
                        {
                            if (name.StartsWith(prepend))
                                prepend += prepend;
                            else
                                break;
                        }
                    }
                }
            }

            if (!sim.SaveImage(prepend + "simulatorImage", ActiveLabel.Image))
            {
                Logger.LogError("Could not copy the image to the simulator images directory.");
                return false;
            }

            return true;

        }
        catch (Exception ex)
        {
            Logger.LogError(ex.Message);
            return false;
        }
    }

    private async void ProcessLabel(int repeat, Label? label, Events_System? ev)
    {
        if (label == null)
        {
            Logger.LogError("The label is null.");
            return;
        }

        if (ev == null)
        {
            if (repeat == 0)
                repeat = await GetLatestRepeatNumber();

            if (repeat > 0)
                if (!(await Commands.SetRepeat(repeat)).OK)
                {
                    Logger.LogError("Error setting the repeat.");
                    return;
                }

            RestoreSectorsResults res = await RetoreSectors(label.Sectors);

            if (res != RestoreSectorsResults.Success)
                return;
        }
        else
        {
            List<Sector_New_Verify> sectors = CreateSectors(ev, ActiveLabel.Table?.GetTableName(), Symbologies);

            Logger.LogInfo("Creating sectors.");

            foreach (Sector_New_Verify sec in sectors)
                if (!await AddSector(sec.name, JsonConvert.SerializeObject(sec)))
                    return;
        }

        if (!await Inspect(repeat))
        {
            Logger.LogError("Error inspecting the repeat.");
            return;
        }
    }
    private async Task<RestoreSectorsResults> RetoreSectors(List<Job.Sector>? sectors)
    {
        if (sectors == null)
            return RestoreSectorsResults.Success;

        if (!await DeleteSectors())
            return RestoreSectorsResults.Failure;

        if (sectors.Count == 0)
            return !await DetectSectors() ? RestoreSectorsResults.Failure : RestoreSectorsResults.Detect;

        foreach (Job.Sector sec in sectors)
        {
            if (!await AddSector(sec.name, JsonConvert.SerializeObject(sec)))
                return RestoreSectorsResults.Failure;

            if (sec.blemishMask?.layers != null)
                foreach (Job.Layer layer in sec.blemishMask.layers)

                    if (!await AddMask(sec.name, JsonConvert.SerializeObject(layer)))
                        if (layer.value != 0)
                            return RestoreSectorsResults.Failure;
        }

        return RestoreSectorsResults.Success;
    }
    private async void RepeatAvailableCallBack(Repeat repeat)
    {
        if (repeat == null)
        {
            Logger.LogError("The repeat is null.");
            return;
        }

        repeat.FullReport = await GetFullReport(repeat.Number, true);
        if (repeat.FullReport == null)
        {
            Logger.LogError("Unable to read the repeat report from the node.");
            return;
        }
        if (repeat.FullReport.Job == null)
        {
            Logger.LogError("The job is null.");
            return;
        }

        repeat.FullReport.Job.jobVersion = Version;

        repeat.Label.RepeatAvailable?.Invoke(repeat);
    }

    public List<Sector_New_Verify> CreateSectors(Events_System ev, string tableID, List<Symbologies.Symbol> symbologies)
    {
        int d1 = 1, d2 = 1;
        List<Sector_New_Verify> lst = [];

        if (ev?.data != null && ev.data.detections != null)
            foreach (Events_System.Detection val in ev.data.detections)
            {
                if (val.region == null || string.IsNullOrEmpty(val.symbology))
                    continue;

                AvailableSymbologies sym = val.symbology.GetSymbology(AvailableDevices.V275);

                Symbologies.Symbol sym1 = symbologies.Find((e) => e.symbology == val.symbology);

                if (sym1 == null)
                    continue;

                if (sym.GetRegionTypeName(AvailableDevices.V275) != sym1.regionType)
                    continue;

                Sector_New_Verify verify = new();

                if (!string.IsNullOrEmpty(tableID))
                {
                    verify.gradingStandard.enabled = true;
                    verify.gradingStandard.tableId = tableID;
                }
                else
                {
                    verify.gradingStandard.enabled = false;
                    verify.gradingStandard.tableId = "1";
                }

                verify.id = sym.GetRegionType(AvailableDevices.V275) == AvailableRegionTypes._1D ? d1++ : d2++;

                verify.type = sym.GetRegionTypeName(AvailableDevices.V275);
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

    public async Task<bool> ReadTask(Repeat repeat)
    {
        if ((repeat.FullReport = await GetFullReport(repeat.Number, true)) == null)
        {
            Logger.LogError("Unable to read the repeat report from the node.");
            return false;
        }

        return true;
    }
}
