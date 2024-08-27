using CommunityToolkit.Mvvm.ComponentModel;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using V275_REST_Lib.Logging;
using V275_REST_Lib.Models;
using static V275_REST_Lib.Models.Report_InspectSector_Common;

namespace V275_REST_Lib
{
    public enum NodeStates
    {
        Offline,
        Idle,
        Editing,
        Running,
        Paused,
        Disconnected
    }

    [JsonObject(MemberSerialization.OptIn)]
    public partial class Controller : ObservableObject
    {
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


        [ObservableProperty][property: JsonProperty] private string host = "127.0.0.1";
        partial void OnHostChanged(string value) { Commands.URLs.Host = value; }

        [ObservableProperty][property: JsonProperty] private uint systemPort = 8080;
        partial void OnSystemPortChanged(uint value) { Commands.URLs.SystemPort = value; }

        [ObservableProperty][property: JsonProperty] private uint nodeNumber = 0;
        partial void OnNodeNumberChanged(uint value) { Commands.URLs.NodeNumber = value; }

        [ObservableProperty][property: JsonProperty] string userName = "admin";
        [ObservableProperty][property: JsonProperty] string password = "admin";
        [ObservableProperty][property: JsonProperty] string simulatorImageDirectory;

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

        [ObservableProperty] private Product? product;
        partial void OnProductChanging(Product? value) => IsProductValid = value != null && value is Product;
        [ObservableProperty] private string productJSON;
        [ObservableProperty] private bool isProductValid;

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
        public bool Is600Dpi => Dpi == 600;

        public bool IsSimulator => IsInspectionValid && Inspection.device.Equals("simulator");
        public bool IsBackupVoid => IsConfigurationCameraValid && ConfigurationCamera?.backupVoidMode?.value == "ON";
        public string? JobName => IsJobValid ? Job?.name : "";

        public Events_System SetupDetectEvent { get; set; }
        private bool SetupDetectEnd { get; set; } = false;

        [ObservableProperty] private bool labelBegin = false;
        [ObservableProperty] private bool labelEnd = false;

        public Controller(string host, uint port, uint nodeNumber, string userName, string password)
        {
            Host = host;
            SystemPort = port;
            NodeNumber = nodeNumber;
            UserName = userName;
            Password = password;

            WebSocket.MessageRecieved += WebSocketEvents_MessageRecieved;

            LoadBase();
        }

        private void LoadBase()
        {
            Task.Run(async () =>
            {
                await UpdateDevices();
                await UpdateInspection();
                await UpdateProduct();
            }).Wait();
        }
        public async Task Login(bool monitor)
        {
            if (IsLoggedIn)
            {
                LogDebug($"Logging out. {UserName} @ {Host}:{SystemPort}");
                await Logout();
                return;
            }

            if (!PreLogin())
            {
                LogDebug($"Pre-Log in FAILED. {UserName} @ {Host}:{SystemPort}");
                return;
            }

            LogDebug($"Logging in. {UserName} @ {Host}:{SystemPort}");

            if ((await Commands.Login(UserName, Password, monitor)).OK)
            {
                LogDebug($"Logged in. {(monitor ? "Monitor" : "Control")} {UserName} @ {Host}:{SystemPort}");

                PostLogin(monitor);
            }
            else
            {
                LogError($"Login FAILED. {UserName} @ {Host}:{SystemPort}");

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
                        LogError(ex);
                        return false;
                    }
                    return true;
                }
                else
                {
                    LogError($"Invalid Simulation Images Directory: '{SimulatorImageDirectory}'");
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

            // Attempt to start the WebSocket connection for receiving node events
            if (!await WebSocket.StartAsync(Commands.URLs.WS_NodeEvents))
                return; // If the WebSocket connection cannot be started, exit the method
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
            //_ = await UpdateProduct(true);

            _ = await UpdateJobs(true);
            _ = await UpdateJob(true);
            _ = await UpdateConfigurationCamera(true);
            _ = await UpdateSymbologies(true);
            _ = await UpdateCalibration(true);
            _ = await UpdateSimulation(true);
            _ = await UpdatePrint(true);
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
                    foreach (var sector in Job.sectors)
                    {
                        if (sector.type == "blemish")
                        {
                            if ((await Commands.GetMask(sector.name)).Object is Job.Mask mask)
                                sector.blemishMask = mask;
                            else
                                LogWarning($"Failed to get mask for sector {sector.name}");
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

            if (ev.name == "heartbeat")
            {
                WebSocket_Heartbeat(ev);
                return;
            }

            //if (ev.name == "setupCapture")
            //{
            //    LogDebug($"WSE: setupCapture {ev.source}; {ev.name}");
            //    SetupCapture?.Invoke(ev);
            //    return;
            //}

            if (ev.name.StartsWith("setupDetect"))
            {
                if (ev.name.EndsWith("End"))
                {
                    LogDebug($"WSE: setupDetect {ev.source}; {ev.name}");
                    WebSocket_SetupDetect(ev, true);
                    return;
                }

                LogDebug($"WSE: setupDetect {ev.source}; {ev.name}");
                WebSocket_SetupDetect(ev, false);
                return;
            }

            if (ev.name == "stateChange")
            {
                LogDebug($"WSE: stateChange : {ev.source}; {ev.name}");
                WebSocket_StateChange(ev);
                return;
            }

            //if (ev.name == "sessionStateChange")
            //{
            //    LogDebug($"WSE: sessionStateChange {ev.source}; {ev.name}");
            //    SessionStateChange?.Invoke(ev);
            //    return;
            //}

            if (ev.name == "labelEnd")
            {
                LogDebug($"WSE: labelEnd {ev.source}; {ev.name}");
                WebSocket_LabelEnd(ev);
                return;
            }

            if (ev.name == "labelBegin")
            {
                LogDebug($"WSE: labelBegin {ev.source}; {ev.name}");
                WebSocket_LabelStart(ev);
                return;
            }
        }

        private void WebSocket_SetupDetect(Models.Events_System ev, bool end)
        {
            SetupDetectEvent = ev;
            SetupDetectEnd = end;
            LabelBegin = true;
        }
        private void WebSocket_LabelStart(Events_System ev)
        {
            LabelBegin = true;
        }
        private void WebSocket_LabelEnd(Models.Events_System ev)
        {
            LabelEnd = true;
        }
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
        }

        private NodeStates GetState(string state)
        {
            if (state == "offline")
                return NodeStates.Offline;
            else if (state == "idle")
                return NodeStates.Idle;
            else if (state == "editing")
                return NodeStates.Editing;
            else if (state == "running")
                return NodeStates.Running;
            else if (state == "paused")
                return NodeStates.Paused;
            else if (state == "disconnected")
                return NodeStates.Disconnected;
            else
                return NodeStates.Offline;
        }
        public async Task ChangeJob(string name)
        {
            if ((await Commands.UnloadJob()).OK)
                if ((await Commands.LoadJob(name)).OK)
                {
                    await SwitchToEdit();
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
                        _ = await Commands.Print(false);
                        Thread.Sleep(100);
                    }

                }

                if (await UpdatePrint())
                    if (Print.enabled != enable)
                        _ = await Commands.Print(enable);
            }
            else
                _ = await SimulatorTogglePrint();
        }
        public async Task<bool> RemoveRepeat()
        {
            var repeat = await GetLatestRepeat();
            if (repeat == -9999)
                return true;

            if (!(await Commands.RemoveRepeat(repeat)).OK)
                return false;

            if (!(await Commands.ResumeJob()).OK)
                return false;

            return true;
        }
        public async Task<bool> Inspect(int repeat)
        {
            if (repeat > 0)
                if (!(await Commands.SetRepeat(repeat)).OK)
                    return false;

            LabelEnd = false;
            if (!(await Commands.Inspect()).OK)
                return false;

            await Task.Run(() =>
            {
                DateTime start = DateTime.Now;
                while (!LabelEnd)
                    if ((DateTime.Now - start) > TimeSpan.FromMilliseconds(10000))
                        return;
            });

            return LabelEnd;
        }

        public class FullReport
        {
            public Report? report;
            public Job? job;
            public byte[]? image;
        }
        public async Task<FullReport?> GetReport(int repeat, bool getImage)
        {
            FullReport report = new();
            if (State == NodeStates.Editing)
            {
                if ((report.report = (await Commands.GetReport()).Object as Report) == null)
                    return null;
            }
            else
            {
                if ((report.report = (await Commands.GetReport(repeat)).Object as Report) == null)
                    return null;
            }

            if (getImage)
                if ((report.image = (await Commands.GetRepeatsImage(repeat)).Object as byte[]) == null)
                {
                    //if (!Commands.Status.StartsWith("Gone"))
                    //{
                    //    return null;
                    //}
                }

            return report;
        }

        public async Task<bool> DeleteSectors()
        {
            Job? job;
            if ((job = (await Commands.GetJob()).Object as Job) == null)
                return false;

            foreach (var sec in job.sectors)
                if (!(await Commands.DeleteSector(sec.name)).OK)
                    return false;

            return true;
        }
        public async Task<bool> DetectSectors()
        {
            if (await Commands.GetDetect() == null)
                return false;

            if (!(await Commands.Detect()).OK)
                return false;

            SetupDetectEnd = false;
            await Task.Run(() =>
            {
                DateTime start = DateTime.Now;
                while (!SetupDetectEnd)
                    if ((DateTime.Now - start) > TimeSpan.FromMilliseconds(20000))
                        return;
            });

            return SetupDetectEnd;
        }

        public async Task<bool> AddSector(string name, string json) => (await Commands.AddSector(name, json)).OK;
        public async Task<bool> AddMask(string name, string json) => (await Commands.AddMask(name, json)).OK;

        public List<Sector_New_Verify> CreateSectors(Events_System ev, string tableID, List<Symbologies.Symbol> symbologies)
        {
            int d1 = 1, d2 = 1;
            List<Sector_New_Verify> lst = new List<Sector_New_Verify>();

            if (ev?.data != null && ev.data.detections != null)
                foreach (var val in ev.data.detections)
                {
                    if (val.region == null)
                        continue;

                    Symbologies.Symbol sym = symbologies.Find((e) => e.symbology == val.symbology);

                    if (sym == null)
                        continue;

                    Sector_New_Verify verify = new Sector_New_Verify();

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

                    verify.id = sym.regionType == "verify1D" ? d1++ : d2++;

                    verify.type = sym.regionType;
                    verify.symbology = val.symbology;
                    verify.name = $"{sym.regionType}_{verify.id}";
                    verify.username = $"{char.ToUpper(verify.name[0])}{verify.name.Substring(1)}";

                    verify.top = val.region.y;
                    verify.left = val.region.x;
                    verify.height = val.region.height;
                    verify.width = val.region.width;

                    verify.orientation = val.orientation;

                    lst.Add(verify);
                }

            return lst;
        }

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

            var rr = await Commands.GetIsRunReady();
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
            if (!(await Commands.SimulatorStart()).OK)
                return false;

            LabelBegin = false;
            await Task.Run(() =>
            {
                DateTime start = DateTime.Now;
                while (!LabelBegin)
                    if ((DateTime.Now - start) > TimeSpan.FromMilliseconds(10000))
                        return;
            });

            return (await Commands.SimulatorStop()).OK && LabelBegin;
        }

        public async Task<FullReport?> Read(int repeat, bool getImage)
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
            if ((report = await GetReport(repeat, getImage)) == null)
                return null;

            if ((report.job = (await Commands.GetJob()).Object as Job) == null)
                return null;

            if (State == NodeStates.Paused)
            {
                if (!(await Commands.RemoveRepeat(repeat)).OK)
                {
                    return null;
                }

                if (!(await Commands.ResumeJob()).OK)
                {
                    return null;
                }
            }
            return report;
        }
        public async Task<int> GetLatestRepeat()
        {
            var res = State == NodeStates.Running ? (await Commands.GetRepeatsAvailableRun()).Object as List<int> : (await Commands.GetRepeatsAvailable()).Object as List<int>;
            return res == null ? 0 : res.Count > 0 ? res.First() : 0;
        }

        #region Logging
        private readonly Logger logger = new();
        public void LogInfo(string message) => logger.LogInfo(this.GetType(), message);
        public void LogDebug(string message) => logger.LogDebug(this.GetType(), message);
        public void LogWarning(string message) => logger.LogInfo(this.GetType(), message);
        public void LogError(string message) => logger.LogError(this.GetType(), message);
        public void LogError(Exception ex) => logger.LogError(this.GetType(), ex);
        public void LogError(string message, Exception ex) => logger.LogError(this.GetType(), message, ex);
        #endregion
    }
}
