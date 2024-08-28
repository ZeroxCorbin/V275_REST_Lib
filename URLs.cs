using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace V275_REST_Lib
{
    public class URLs
    {
        public string? Host { get; set; } = "127.0.0.1";
        public uint SystemPort { get; set; } = 8080;
        public uint NodeNumber { get; set; } = 0;

        private string NodePort => $"{SystemPort + NodeNumber}";

        /// <summary>
        /// Events
        /// </summary>
        public string WS_NodeEvents => $"ws://{Host}:{NodePort}/api/printinspection/{NodeNumber}/inspection/events";
        public string WS_SystemEvents => $"ws://{Host}:{SystemPort}/api/printinspection/event";

        /// <summary>
        /// System API
        /// </summary>
        private string SystemBase => $"http://{Host}:{SystemPort}/api/printinspection";
        
        public string Product() => $"{SystemBase}/product";
        public string Devices() => $"{SystemBase}/devices";

        /// <summary>
        /// Node API
        /// </summary>
        private string NodeBase => $"http://{Host}:{NodePort}/api/printinspection";

        public string CameraConfiguration() => $"{NodeBase}/{NodeNumber}/configuration/camera";
        public string Inspection() => $"{NodeBase}/{NodeNumber}/inspection";


        public string Login(bool monitor = false, bool temporary = false) => $"{NodeBase}/{NodeNumber}/security/login?monitor={(monitor ? "1" : "0")}&temporary={(temporary ? "1" : "0")}";
        public string Logout() => $"{NodeBase}/{NodeNumber}/security/logout";

        public string GradingStandards() => $"{NodeBase}/{NodeNumber}/gradingstandards";

        public string Job() => $"{NodeBase}/{NodeNumber}/inspection/job";

        public string DeleteSector(string sectorName) => $"{NodeBase}/{NodeNumber}/inspection/job/sectors/{sectorName}";
        public string AddSector(string sectorName) => $"{NodeBase}/{NodeNumber}/inspection/job/sectors/{sectorName}";

        public string Mask(string sectorName) => $"{NodeBase}/{NodeNumber}/inspection/job/sectors/{sectorName}/goldenImage/mask";

        public string Print() => $"{NodeBase}/{NodeNumber}/inspection/print";
        public string Print_Body(bool enabled) => JsonConvert.SerializeObject(new Models.Print() { enabled = enabled, state = enabled, _override = false });

        public string History(string repeatNumber) => $"{NodeBase}/{NodeNumber}/inspection/setup/image?source=history&repeat={repeatNumber}";
        public string History() => $"{NodeBase}/{NodeNumber}/inspection/setup/image/history";

        public string Available() => $"{NodeBase}/{NodeNumber}/inspection/setup/image/available";
        public string AvailableRun() => $"{NodeBase}/{NodeNumber}/inspection/repeat/images/available";

        public string IsRunReady() => $"{NodeBase}/{NodeNumber}/inspection/job/isrunready";
        public string RunJob(string jobName) => $"{NodeBase}/{NodeNumber}/repository/jobs/design/{jobName}?source=inspection";
        public string Jobs() => $"{NodeBase}/{NodeNumber}/repository/jobs/design/";
        public string StartJob() => $"{NodeBase}/{NodeNumber}/inspection/job/start";
        public string ResumeJob() => $"{NodeBase}/{NodeNumber}/inspection/job/resume";
        public string PauseJob() => $"{NodeBase}/{NodeNumber}/inspection/job/pause";
        public string UnloadJob() => $"{NodeBase}/{NodeNumber}/inspection/job/unload";
        public string LoadJob() => $"{NodeBase}/{NodeNumber}/inspection/job/load";
        public string StopJob() => $"{NodeBase}/{NodeNumber}/inspection/job/stop?finalizeActive=0";

        public string Simulation() => $"{NodeBase}/{NodeNumber}/simulation";

        public string SimulationTrigger() => $"{NodeBase}/{NodeNumber}/simulation/trigger";
        public string SimulationTriggerImage(uint dpi) => $"{NodeBase}/{NodeNumber}/simulation/triggerimage?dpi={dpi}";
        public string SimulationTriggerImage() => $"{NodeBase}/{NodeNumber}/simulation/triggerimage";


        public string StartSimulation() => $"{NodeBase}/{NodeNumber}/simulation/start";
        public string StopSimulation() => $"{NodeBase}/{NodeNumber}/simulation/stop";

        public string Inspect() => $"{NodeBase}/{NodeNumber}/inspection/setup/inspect";
        public string Report() => $"{NodeBase}/{NodeNumber}/inspection/setup/report";
        public string Report(int repeat) => $"{NodeBase}/{NodeNumber}/inspection/repeat/reports/{repeat}";
        public string Remove(int repeat) => $"{NodeBase}/{NodeNumber}/inspection/stopevent/failure/removed/{repeat}";
        public string Detect() => $"{NodeBase}/{NodeNumber}/inspection/setup/detect";
        public string Calibrate() => $"{NodeBase}/{NodeNumber}/calibrate/video?showAll=true";


        public string SetSendExtendedData(bool enable) => $"{NodeBase}/{NodeNumber}/labelval?sendExtendedData={enable}";
        public string GetSendExtendedData() => $"{NodeBase}/{NodeNumber}/labelval";

        public string RepeatImage(int repeatNumber) => $"{NodeBase}/{NodeNumber}/inspection/repeat/images/{repeatNumber}?scale=1.0";

        public string VerifySymbologies() => $"{NodeBase}/{NodeNumber}/inspection/verify/symbologies";


        public string CameraCommand() => $"{NodeBase}/{NodeNumber}/camera/command";
    }
}
