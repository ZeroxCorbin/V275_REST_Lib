using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using V275_REST_Lib.Logging;
using V275_REST_Lib.Models;

namespace V275_REST_Lib
{
    public class Results
    {
        /// <summary>
        /// The HTTP request was successful and the JSON response was deserialized.
        /// </summary>
        public bool OK { get; set; } = false;
        /// <summary>
        /// If there is a JSON response, it will be stored here.
        /// </summary>
        public string Json { get; set; } = string.Empty;
        /// <summary>
        /// This is the response from the request. This could be a string, byte[], or object (T) depending on the request method.
        /// If there is a JSON response, it will be deserialized into this object.
        /// </summary>
        public object? Object { get; set; }
        /// <summary>
        /// The HttpResponseMessage from the request.
        /// </summary>
        public HttpResponseMessage? HttpResponseMessage { get; set; }
        
    }

    public class Commands
    {
        public URLs URLs { get; } = new URLs();

        private Connection Connection { get; } = new Connection();
        public string Token { get; set; } = string.Empty;

        public async Task<Results> Login(string user, string pass, bool monitor, bool temporary = false)
        {
            Results results = new()
            {
                Object = await Connection.Get_Token(URLs.Login(monitor, temporary), user, pass)
            };
            if (results.OK = CheckResults())
            {
                Token = (string)results.Object;
            }
            else
                Token = string.Empty;
            return results;
        }
        public async Task<Results> Logout()
        {
            Results results = new()
            {
                OK = await Connection.Put(URLs.Logout(), string.Empty, Token)
            };
            Token = string.Empty;
            return results;
        }

        public async Task<Results> GetDevices() => CheckResults<Models.Devices>(await Connection.Get(URLs.Devices(), string.Empty));
        public async Task<Results> GetInspection() => CheckResults<Models.Inspection>(await Connection.Get(URLs.Inspection(), string.Empty));
        public async Task<Results> GetProduct() => CheckResults<Models.Product>(await Connection.Get(URLs.Product(), string.Empty));

        public async Task<Results> GetGradingStandards() => CheckResults<Models.GradingStandards>(await Connection.Get(URLs.GradingStandards(), Token));
        public async Task<Results> GetSymbologies() => CheckResults<List<Models.Symbologies.Symbol>>(await Connection.Get(URLs.VerifySymbologies(), Token));
        public async Task<Results> GetJob() => CheckResults<Models.Job>(await Connection.Get(URLs.Job(), Token));
        public async Task<Results> GetJobs() => CheckResults<Models.Jobs>(await Connection.Get(URLs.Jobs(), Token));
        public async Task<Results> GetMask(string sectorName) => CheckResults<Models.Job.Mask>(await Connection.Get(URLs.Mask(sectorName), Token));
        public async Task<Results> GetReport() => CheckResults<Models.Report>(await Connection.Get(URLs.Report(), Token));
        public async Task<Results> GetReport(int repeat) => CheckResults<Models.Report>(await Connection.Get(URLs.Report(repeat), Token));
        public async Task<Results> GetCalibration() => CheckResults<Models.Calibration>(await Connection.Get(URLs.Calibrate(), Token));
        public async Task<Results> GetRepeatsAvailable() => CheckResults<List<int>>(await Connection.Get(URLs.Available(), Token));
        public async Task<Results> GetRepeatsAvailableRun() => CheckResults<List<int>>(await Connection.Get(URLs.AvailableRun(), Token));
        public async Task<Results> GetRepeatsImage(int repeat) => CheckResults(await Connection.GetBytes(URLs.RepeatImage(repeat), Token));
        public async Task<Results> GetCameraConfig() => CheckResults<Models.Configuration_Camera>(await Connection.Get(URLs.CameraConfiguration(), Token));
        public async Task<Results> GetSendExtendedData() => CheckResults(await Connection.Get(URLs.GetSendExtendedData(), Token));
        public async Task<Results> GetIsRunReady() => CheckResults(await Connection.Get(URLs.IsRunReady(), Token));
        public async Task<Results> GetSimulation() => CheckResults<Models.Simulation>(await Connection.Get(URLs.Simulation(), Token));
        public async Task<Results> GetDetect() => CheckResults<DetectResponse>(await Connection.Get(URLs.Detect(), Token));
        public async Task<Results> GetPrint() => CheckResults<Models.Print>(await Connection.Get(URLs.Print(), Token));
        public async Task<Results> GetDPI() => CheckResults(await Connection.Get(URLs.SimulationTriggerImage(), Token));
        public async Task<Results> GetCameraCommand() => CheckResults(await Connection.Get(URLs.CameraCommand(), Token));


        public async Task<Results> SetSendExtendedData(bool enable) => CheckResults(await Connection.Put(URLs.SetSendExtendedData(enable), string.Empty, Token));
        public async Task<Results> RunJob(string jobName) => CheckResults(await Connection.Put(URLs.RunJob(jobName), string.Empty, Token));
        public async Task<Results> StartJob() => CheckResults(await Connection.Put(URLs.StartJob(), string.Empty, Token));
        public async Task<Results> ResumeJob() => CheckResults(await Connection.Put(URLs.ResumeJob(), string.Empty, Token));
        public async Task<Results> StopJob() => CheckResults(await Connection.Put(URLs.StopJob(), string.Empty, Token));
        public async Task<Results> PauseJob() => CheckResults(await Connection.Put(URLs.PauseJob(), string.Empty, Token));
        public async Task<Results> UnloadJob() => CheckResults(await Connection.Put(URLs.UnloadJob(), string.Empty, Token));
        public async Task<Results> LoadJob(string name) => CheckResults(await Connection.Put(URLs.LoadJob(), $"design/{name}", Token));
        public async Task<Results> DeleteSector(string sectorName) => CheckResults(await Connection.Delete(URLs.DeleteSector(sectorName), Token));
        public async Task<Results> AddSector(string sectorName, string json) => CheckResults(await Connection.Post(URLs.AddSector(sectorName), json, Token));
        public async Task<Results> AddMask(string sectorName, string json) => CheckResults(await Connection.Patch(URLs.Mask(sectorName), json, Token));
        public async Task<Results> Inspect() => CheckResults(await Connection.Put(URLs.Inspect(), string.Empty, Token));
        public async Task<Results> Detect() => CheckResults(await Connection.Put(URLs.Detect(), string.Empty, Token));
        public async Task<Results> RemoveRepeat(int repeat) => CheckResults(await Connection.Put(URLs.Remove(repeat), string.Empty, Token));
        public async Task<Results> Print(Print print) => CheckResults(await Connection.Put(URLs.Print(), JsonConvert.SerializeObject(print), Token));
        public async Task<Results> PutSimulation(Simulation simulation) => CheckResults(await Connection.Put(URLs.Simulation(), JsonConvert.SerializeObject(simulation), Token));
        public async Task<Results> SimulationTriggerImage(SimulationTrigger simulationTrigger) => CheckResults(await Connection.Put(URLs.SimulationTriggerImage(simulationTrigger.dpi), simulationTrigger.image, Token));
        public async Task<Results> SimulationTrigger() => CheckResults(await Connection.Put(URLs.SimulationTrigger(), string.Empty, Token));
        public async Task<Results> SetDPI(uint dpi) => CheckResults(await Connection.Put(URLs.SimulationTriggerImage(dpi), new byte[0], Token));
        public async Task<Results> SimulatorStart() => CheckResults(await Connection.Put(URLs.StartSimulation(), string.Empty, Token));
        public async Task<Results> SimulatorStop() => CheckResults(await Connection.Put(URLs.StopSimulation(), string.Empty, Token));
        public async Task<Results> SetRepeat(int repeat) => CheckResults(await Connection.Put(URLs.History(repeat.ToString()), string.Empty, Token));
        public async Task<Results> PutCameraCommand(string cmd) => CheckResults(await Connection.Put(URLs.CameraCommand(), cmd, Token));

        #region Results Checking

        /// <summary>
        /// Check the HttpResponseMessage for success.
        /// Used for Login.
        /// </summary>
        /// <returns></returns>
        private bool CheckResults()
        {
            if (Connection.IsException)
            {
                if (Connection.Exception != null)
                    LogError(Connection.Exception);
                return false;
            }
            else if (Connection.HttpResponseMessage != null && !Connection.HttpResponseMessage.IsSuccessStatusCode)
            {
                LogWarning($"{Connection.HttpResponseMessage.StatusCode}: {Connection.HttpResponseMessage.ReasonPhrase}");
                return false;
            }
            return true;
        }

        /// <summary>
        /// Check the HttpResponseMessage for success and attempt to deserialze the JSON into a new object (T).
        /// This is used for GET requests that return JSON.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="json"></param>
        /// <returns></returns>
        private Results CheckResults<T>(string json)
        {
            Results results = new()
            {
                HttpResponseMessage = Connection.HttpResponseMessage
            };

            if (Connection.IsException)
            {
                if (Connection.Exception != null)
                    LogError(Connection.Exception);
                results.Object = json;
                return results;
            }
            else if (Connection.HttpResponseMessage != null && !Connection.HttpResponseMessage.IsSuccessStatusCode)
            {
                LogError($"{Connection.HttpResponseMessage.StatusCode}: {Connection.HttpResponseMessage.ReasonPhrase}");
                results.Object = json;
                return results;
            }

            if (!string.IsNullOrEmpty(json))
                results.Json = json;
            else
                return results;

            try
            {
                results.Object = JsonConvert.DeserializeObject<T>(results.Json);
                results.OK = true;
            }
            catch (Exception ex)
            {
                LogError(ex);
                results.Object = null;
            }

            return results;
        }

        /// <summary>
        /// Check the HttpResponseMessage for success and return the string.
        /// This is used for GET requests that return a string that it is not JSON or 
        /// if it is JSON, it should not be deserialized.
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        private Results CheckResults(string data)
        {
            Results results = new()
            {
                HttpResponseMessage = Connection.HttpResponseMessage,
                Object = data
            };

            if (Connection.IsException)
            {
                if (Connection.Exception != null)
                    LogError(Connection.Exception);
                else
                    LogError("Unknown Connection Exception");

                return results;
            }
            else if (Connection.HttpResponseMessage != null && !Connection.HttpResponseMessage.IsSuccessStatusCode)
            {
                LogWarning($"{Connection.HttpResponseMessage.StatusCode}: {Connection.HttpResponseMessage.ReasonPhrase}");
                return results;
            }

            if (data != null)
                results.OK = true;

            return results;
        }

        /// <summary>
        /// Check the HttpResponseMessage for success and return the byte[] data.
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        private Results CheckResults(byte[] data)
        {
            Results results = new()
            {
                HttpResponseMessage = Connection.HttpResponseMessage,
                Object = data
            };

            if (Connection.IsException)
            {
                if (Connection.Exception != null)
                    LogError(Connection.Exception);
                return results;
            }
            else if (Connection.HttpResponseMessage != null && !Connection.HttpResponseMessage.IsSuccessStatusCode)
            {
                LogWarning($"{Connection.HttpResponseMessage.StatusCode}: {Connection.HttpResponseMessage.ReasonPhrase}");
                return results;
            }

            if (data != null)
                results.OK = true;

            return results;
        }

        /// <summary>
        /// Check the HttpResponseMessage for success and return the Content (BODY) of the message.
        /// This is mainly used for PUT requests.
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        private Results CheckResults(bool data)
        {
            Results results = new()
            {
                HttpResponseMessage = Connection.HttpResponseMessage
            };

            if (Connection.IsException)
            {
                if (Connection.Exception != null)
                    LogError(Connection.Exception);
                return results;
            }
            else if (Connection.HttpResponseMessage != null && !Connection.HttpResponseMessage.IsSuccessStatusCode)
            {
                LogWarning($"{Connection.HttpResponseMessage.StatusCode}: {Connection.HttpResponseMessage.ReasonPhrase}");
                return results;
            }

            results.OK = data;
            var tsk = Connection.HttpResponseMessage?.Content.ReadAsStringAsync();
            if (tsk != null && tsk.Wait(1000))
                results.Object = tsk.Result;

            return results;
        }
        #endregion

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
