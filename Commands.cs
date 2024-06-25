using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using V275_REST_lib.Models;
using V275_REST_Lib.Models;

namespace V275_REST_lib
{
    public class Commands
    {
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

        private class Results
        {
            public bool OK { get; set; }
            public string Data { get; set; }
        }

        private Connection Connection { get; set; } = new Connection();
        public URLs URLs { get; private set; } = new URLs();

        //public bool IsLoggedIn { get; set; }
        //public bool IsMonitor { get; set; }
        public string Token { get; set; }
        public string Host { get => URLs.Host; set => URLs.Host = value; }
        public uint SystemPort { get => URLs.SystemPort; set => URLs.SystemPort = value; }
        public uint NodeNumber { get => URLs.NodeNumber; set => URLs.NodeNumber = value; }

        //public bool IsException => Connection.IsException ? true : string.IsNullOrEmpty(Status) ? true : false;
        //public string Exception => Connection.IsException ? Connection.Exception.Message : Status;
        public string Status { get; private set; }

        //public Devices Devices { get; private set; }
        //public Inspection Inspection { get; private set; }

        //public Product Product { get; private set; }
        //public GradingStandards GradingStandards { get; private set; }
        //public List<Symbologies.Symbol> Symbologies { get; private set; }
        //public Job Job { get; private set; }
        //public Job.Mask Mask { get; private set; }
        
        //public Report Report { get; private set; }
        //public Configuration_Camera ConfigurationCamera { get; private set; }
        //public DetectResponse Detected { get; private set; }
        //public List<int> Available { get; private set; }
        //public byte[] RepeatImage { get; private set; }
        //public Calibration Calibration { get; private set; }

        private bool CheckResults(string json, bool ignoreJson = false)
        {
            Status = "";

            if (!ignoreJson)
                if (json != null)
                    if (!json.StartsWith("{"))
                        if (!json.StartsWith("["))
                        {
                            Status = $"Return data is not JSON: \"{json}\"";
                            Logger.Warn($"Return data is not JSON: \"{json}\"");
                        }

            if (Connection.IsException)
            {
                Status = Connection.Exception.Message;
                Logger.Error(Connection.Exception);
            }

            else if (Connection.HttpResponseMessage != null && !Connection.HttpResponseMessage.IsSuccessStatusCode)
            {
                Status = $"{Connection.HttpResponseMessage.StatusCode}: {Connection.HttpResponseMessage.ReasonPhrase}";
                Logger.Warn($"{Connection.HttpResponseMessage.StatusCode}: {Connection.HttpResponseMessage.ReasonPhrase}");
            }

            return string.IsNullOrEmpty(Status);
        }

        public async Task<Devices> GetDevices()
        {
            Logger.Info("GET: {url}", URLs.Devices());
            string data = await Connection.Get(URLs.Devices(), "");
            return CheckResults(data) ? JsonConvert.DeserializeObject<Devices>(data) : null;
        }

        public async Task<Inspection> GetInspection()
        {
            Logger.Info("GET: {url}", URLs.Inspection());
            string data = await Connection.Get(URLs.Inspection(), "");
            return CheckResults(data) ? JsonConvert.DeserializeObject<Inspection>(data) : null;
        }

        public async Task<Product> GetProduct()
        {
            Logger.Info("GET: {url}", URLs.Product());
            string data = await Connection.Get(URLs.Product(), "");
            return CheckResults(data) ? JsonConvert.DeserializeObject<Product>(data) : null;
        }

        public async Task<bool> Login(string user, string pass, bool monitor, bool temporary = false)
        {
            Logger.Info("LOGIN {user}: {url}", user, URLs.Login(monitor, temporary));
            Token = await Connection.Get_Token(URLs.Login(monitor, temporary), user, pass);
            return CheckResults(Token, true);
        }
        public async Task<bool> Logout()
        {
            Logger.Info("LOGOUT: {url}", URLs.Logout());
            _ = await Connection.Put(URLs.Logout(), "", Token);
            return CheckResults("", true);
        }

        public async Task<GradingStandards> GetGradingStandards()
        {
            Logger.Info("GET: {url}", URLs.GradingStandards());
            string result = await Connection.Get(URLs.GradingStandards(), Token);
            return CheckResults(result) ? JsonConvert.DeserializeObject<GradingStandards>(result) : null;
        }
        public async Task<List<Symbologies.Symbol>> GetSymbologies()
        {
            Logger.Info("GET: {url}", URLs.VerifySymbologies());
            string result = await Connection.Get(URLs.VerifySymbologies(), Token);
            return CheckResults(result) ? JsonConvert.DeserializeObject<List<Symbologies.Symbol>>(result) : null;
        }

        public async Task<Jobs> GetJobs()
        {
            Logger.Info("GET: {url}", URLs.Jobs());

            string result = await Connection.Get(URLs.Jobs(), Token);

            return CheckResults(result) ? JsonConvert.DeserializeObject<Jobs>(result) : null;
        }
        public async Task<Job> GetJob()
        {
            Logger.Info("GET: {url}", URLs.Job());

            string result = await Connection.Get(URLs.Job(), Token);

            return CheckResults(result) ? JsonConvert.DeserializeObject<Job>(result) : null;
        }

        public async Task<Job.Mask> GetMask(string sectorName)
        {
            Logger.Info("GET: {url}", URLs.Mask(sectorName));

            string result = await Connection.Get(URLs.Mask(sectorName), Token);

            return CheckResults(result) ? JsonConvert.DeserializeObject<Job.Mask>(result) : null;
        }

        public async Task<Report> GetReport()
        {
            Logger.Info("GET: {url}", URLs.Report());

            string result = await Connection.Get(URLs.Report(), Token);

            return CheckResults(result) ? JsonConvert.DeserializeObject<Report>(result) : null;
        }
        public async Task<Report> GetReport(int repeat)
        {
            Logger.Info("GET: {url}", URLs.Report(repeat));

            string result = await Connection.Get(URLs.Report(repeat), Token);

            return CheckResults(result) ? JsonConvert.DeserializeObject<Report>(result) : null;
        }
        public async Task<Calibration> GetCalibration()
        {
            Logger.Info("GET: {url}", URLs.Calibrate());

            string result = await Connection.Get(URLs.Calibrate(), Token);

            return CheckResults(result) ? JsonConvert.DeserializeObject<Calibration>(result) : null;
        }

        public async Task<List<int>> GetRepeatsAvailable()
        {
            Logger.Info("GET: {url}", URLs.Available());

            string result = await Connection.Get(URLs.Available(), Token);

            return CheckResults(result) ? JsonConvert.DeserializeObject<int[]>(result).ToList() : null;
        }
        public async Task<List<int>> GetRepeatsAvailableRun()
        {
            Logger.Info("GET: {url}", URLs.Available());

            string result = await Connection.Get(URLs.AvailableRun(), Token);

            return CheckResults(result) ? JsonConvert.DeserializeObject<int[]>(result).ToList() : null;
        }

        public async Task<byte[]> GetRepeatsImage(int repeat)
        {
            Logger.Info("GET: {url}", URLs.RepeatImage(repeat));

            var result = await Connection.GetBytes(URLs.RepeatImage(repeat), Token);

            return CheckResults("", true) ? result : null;
        }
        public async Task<Configuration_Camera> GetCameraConfig()
        {
            Logger.Info("GET: {url}", URLs.CameraConfiguration());

            string result = await Connection.Get(URLs.CameraConfiguration(), Token);

            return CheckResults(result) ? JsonConvert.DeserializeObject<Configuration_Camera>(result) : null;
        }

        public async Task<bool> GetSendExtendedData()
        {
            Logger.Info("GET: {url}", URLs.GetSendExtendedData());

            string result = await Connection.Get(URLs.GetSendExtendedData(), Token);

            return CheckResults(result, true) && result == "true";
        }
        public async Task<bool> SetSendExtendedData(bool enable)
        {
            Logger.Info("PUT: {url}", URLs.SetSendExtendedData(enable));

            _ = await Connection.Put(URLs.SetSendExtendedData(enable), "", Token);

            return CheckResults("", true);
        }

        public async Task<bool> GetIsRunReady()
        {
            Logger.Info("GET: {url}", URLs.IsRunReady());

            string result = await Connection.Get(URLs.IsRunReady(), Token);

            return CheckResults(result, true) && result == "OK";
        }
        public async Task<bool> RunJob(string jobName)
        {
            Logger.Info("PUT: {url}", URLs.RunJob(jobName));

            _ = await Connection.Put(URLs.RunJob(jobName), "", Token);

            return CheckResults("", true);
        }

        public async Task<bool> StartJob()
        {
            Logger.Info("PUT: {url}", URLs.StartJob());

            _ = await Connection.Put(URLs.StartJob(), "", Token);

            return CheckResults("", true);
        }
        public async Task<bool> ResumeJob()
        {
            Logger.Info("PUT: {url}", URLs.ResumeJob());

            _ = await Connection.Put(URLs.ResumeJob(), "", Token);

            return CheckResults("", true);
        }
        public async Task<bool> StopJob()
        {
            Logger.Info("PUT: {url}", URLs.StopJob());

            _ = await Connection.Put(URLs.StopJob(), "", Token);

            return CheckResults("", true);
        }
        public async Task<bool> PauseJob()
        {
            Logger.Info("PUT: {url}", URLs.PauseJob());

            _ = await Connection.Put(URLs.PauseJob(), "", Token);

            return CheckResults("", true);
        }
        public async Task<bool> UnloadJob()
        {
            Logger.Info("PUT: {url}", URLs.UnloadJob());

            _ = await Connection.Put(URLs.UnloadJob(), "", Token);

            return CheckResults("", true);
        }
        public async Task<bool> LoadJob(string name)
        {
            Logger.Info("PUT: {url}", URLs.LoadJob());

            _ = await Connection.Put(URLs.LoadJob(), $"design/{name}", Token);

            return CheckResults("", true);
        }

        public async Task<bool> DeleteSector(string sectorName)
        {
            Logger.Info("DELETE: {url}", URLs.DeleteSector(sectorName));

            _ = await Connection.Delete(URLs.DeleteSector(sectorName), Token);

            return CheckResults("", true);
        }
        public async Task<bool> AddSector(string sectorName, string json)
        {
            Logger.Info("POST: {url}", URLs.AddSector(sectorName));

            _ = await Connection.Post(URLs.AddSector(sectorName), json, Token);

            return CheckResults("", true);
        }

        public async Task<bool> AddMask(string sectorName, string json)
        {
            Logger.Info("PATCH: {url}", URLs.Mask(sectorName));

            _ = await Connection.Patch(URLs.Mask(sectorName), json, Token);

            return CheckResults("", true);
        }

        public async Task<bool> Inspect()
        {
            Logger.Info("PUT: {url}", URLs.Inspect());

            _ = await Connection.Put(URLs.Inspect(), "", Token);

            return CheckResults("", true);
        }
        public async Task<bool> Detect()
        {
            Logger.Info("PUT: {url}", URLs.Detect());

            _ = await Connection.Put(URLs.Detect(), "", Token);

            return CheckResults("", true);
        }
        public async Task<DetectResponse> GetDetect()
        {
            Logger.Info("GET: {url}", URLs.Detect());

            string result = await Connection.Get(URLs.Detect(), Token);

            return CheckResults(result) ? JsonConvert.DeserializeObject<DetectResponse>(result) : null;
        }
        public async Task<bool> RemoveRepeat(int repeat)
        {
            Logger.Info("PUT: {url}", URLs.Remove(repeat));

            _ = await Connection.Put(URLs.Remove(repeat), "", Token);

            return CheckResults("", true);
        }

        public async Task<Print> GetPrint()
        {
            Logger.Info("GET: {url}", URLs.Print());

            string result = await Connection.Get(URLs.Print(), Token);

            return CheckResults(result) ? JsonConvert.DeserializeObject<Print>(result) : null;
        }

        public async Task<bool> Print(bool enabled)
        {
            Logger.Info("PUT: {url}", URLs.Print());

            await Connection.Put(URLs.Print(), URLs.Print_Body(enabled), Token);

            return CheckResults("", true);
        }


        public async Task<Simulation> GetSimulation()
        {
            Logger.Info("GET: {url}", URLs.Simulation());

            string result = await Connection.Get(URLs.Simulation(), Token);

            return CheckResults(result) ? JsonConvert.DeserializeObject<Simulation>(result) : null;
        }
        public async Task<bool> PutSimulation(Simulation simulation)
        {
            Logger.Info("PUT: {url}", URLs.Simulation());

            _ = await Connection.Put(URLs.Simulation(), JsonConvert.SerializeObject(simulation), Token);

            return CheckResults("", true);
        }

        public async Task<bool> TriggerSimulator(SimulationTrigger simulationTrigger)
        {
            Logger.Info("PUT: {url}", URLs.TriggerSimulation(simulationTrigger.size, simulationTrigger.dpi));

            _ = await Connection.Put(URLs.TriggerSimulation(simulationTrigger.size, simulationTrigger.dpi), simulationTrigger.image, Token);

            return CheckResults("", true);
        }

        public async Task<bool> TriggerSimulator()
        {
            Logger.Info("PUT: {url}", URLs.TriggerSimulation());

            _ = await Connection.Put(URLs.TriggerSimulation(), "", Token);

            return CheckResults("", true);
        }

        public async Task<bool> SimulatorStart()
        {
            Logger.Info("PUT: {url}", URLs.StartSimulation());

            _ = await Connection.Put(URLs.StartSimulation(), "", Token);

            return CheckResults("", true);
        }

        public async Task<bool> SimulatorStop()
        {
            Logger.Info("PUT: {url}", URLs.StopSimulation());

            _ = await Connection.Put(URLs.StopSimulation(), "", Token);

            return CheckResults("", true);
        }

        public async Task<bool> SetRepeat(int repeat)
        {
            Logger.Info("PUT: {url}", URLs.History(repeat.ToString()));

            _ = await Connection.Put(URLs.History(repeat.ToString()), "", Token);

            return CheckResults("", true);
        }

        public async Task<bool> GetCameraCommand()
        {
            Logger.Info("GET: {url}", URLs.CameraCommand());

            string result = await Connection.Get(URLs.CameraCommand(), Token);

            bool res;
            //if (res = CheckResults(result))
            //    Detected = JsonConvert.DeserializeObject<DetectResponse>(result);

            return true;
        }

        public async Task<bool> PutCameraCommand(string cmd)
        {
            Logger.Info("PUT: {url}", URLs.CameraCommand());

            var result = await Connection.Put(URLs.CameraCommand(), cmd, Token);

            bool res;
            //if (res = CheckResults(result))
            //    Detected = JsonConvert.DeserializeObject<DetectResponse>(result);

            return true;
        }

    }
}
