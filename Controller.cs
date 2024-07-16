using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using V275_REST_lib.Models;

namespace V275_REST_lib
{
    public class Controller
    {
        public delegate void StateChangedDel(string state, string? jobName, int dpi);
        public event StateChangedDel StateChanged;

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

        public string V275_State { get; set; } = "";
        public string V275_JobName { get; set; } = "";
        public int V275_DPI { get; set; } = 0;

        public Events_System SetupDetectEvent { get; set; }
        private bool SetupDetectEnd { get; set; } = false;

        bool LabelBegin { get; set; } = false;
        bool LabelEnd { get; set; } = false;

        public string? Status
        {
            get { return _Status; }
            set { _Status = value; }
        }
        private string? _Status;

        public Controller(string host, uint systemPort, uint nodeNumber)
        {
            Commands.Host = host;
            Commands.SystemPort = systemPort;
            Commands.NodeNumber = nodeNumber;

            //WebSocket.SetupCapture += WebSocket_SetupCapture;
            //WebSocket.SessionStateChange += WebSocket_SessionStateChange;
            WebSocket.Heartbeat += WebSocket_Heartbeat;
            //WebSocket.SetupDetect += WebSocket_SetupDetect;
            WebSocket.LabelStart += WebSocket_LabelStart;
            WebSocket.LabelEnd += WebSocket_LabelEnd;
            WebSocket.SetupDetect += WebSocket_SetupDetect;
            WebSocket.StateChange += WebSocket_StateChange;
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
            string state;
            if (ev != null)
                state = char.ToUpper(ev.data.state[0]) + ev.data.state.Substring(1);
            else
                state = "";

            if (ev != null)
                V275_DPI = (int)ev.data.Current_dpi;
            else
                V275_DPI = 0;

            if (V275_State != state)
            {
                V275_State = state;

                if (V275_State != "Idle" && V275_State != "")
                {
                    new Task(async () =>
                    {
                        Job job;
                        V275_JobName = (job = await Commands.GetJob()) != null ? job.name : "";
                        StateChanged?.Invoke(V275_State, V275_JobName, V275_DPI);
                    }).Start();

                    return;
                }
                else
                {
                    V275_JobName = "";
                }
            }

            StateChanged?.Invoke(V275_State, V275_JobName, V275_DPI);
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

        public async Task<Job> GetJob()
        {
            Job job;
            if ((job = await Commands.GetJob()) == null)
            {
                Status = Commands.Status;
                return null;
            }

            foreach (var sector in job.sectors)
            {
                if (sector.type == "blemish")
                {
                    Job.Mask mask;
                    if ((mask = await Commands.GetMask(sector.name)) == null)
                    {
                        Status = Commands.Status;
                        return null;
                    }
                    sector.blemishMask = mask;
                }
            }
            return job;
        }

        public async Task<bool> Inspect(int repeat)
        {
            if (repeat > 0)
                if (!await Commands.SetRepeat(repeat))
                {
                    Status = Commands.Status;
                    return false;
                }

            LabelEnd = false;
            if (!await Commands.Inspect())
            {
                Status = Commands.Status;
                return false;
            }


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
        public async Task<FullReport> GetReport(int repeat, bool getImage)
        {
            FullReport report = new FullReport();
            if (V275_State == "Editing")
            {
                if ((report.report = await Commands.GetReport()) == null)
                {
                    Status = Commands.Status;
                    return null;
                }
            }
            else
            {
                if ((report.report = await Commands.GetReport(repeat)) == null)
                {
                    Status = Commands.Status;
                    return null;
                }
            }

            if (getImage)
                if ((report.image = await Commands.GetRepeatsImage(repeat)) == null)
                {
                    if (!Commands.Status.StartsWith("Gone"))
                    {
                        Status = Commands.Status;
                        return null;
                    }
                }

            return report;
        }

        public async Task<bool> DeleteSectors()
        {
            Job job;
            if ((job = await Commands.GetJob()) == null)
            {
                Status = Commands.Status;
                return false;
            }

            foreach (var sec in job.sectors)
            {
                if (!await Commands.DeleteSector(sec.name))
                {
                    Status = Commands.Status;
                    return false;
                }
            }

            return true;
        }
        public async Task<bool> DetectSectors()
        {
            if (await Commands.GetDetect() == null)
            {
                Status = Commands.Status;
                return false;
            }

            if (!await Commands.Detect())
            {
                Status = Commands.Status;
                return false;
            }

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

        public async Task<bool> AddSector(string name, string json)
        {
            if (!await Commands.AddSector(name, json))
            {
                Status = Commands.Status;
                return false;
            }
            else
                return true;
        }

        public async Task<bool> AddMask(string name, string json)
        {
            if (!await Commands.AddMask(name, json))
            {
                Status = Commands.Status;
                return false;
            }
            else
                return true;
        }

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
            if (V275_State == "Idle")
                return false;

            if (V275_State == "Editing")
                return true;

            if (!await Commands.StopJob())
            {
                Status = Commands.Status;
                return false;
            }

            await Task.Run(() =>
            {
                DateTime start = DateTime.Now;
                while (V275_State != "Editing")
                    if ((DateTime.Now - start) > TimeSpan.FromMilliseconds(10000))
                        return;
            });

            return V275_State == "Editing";
        }
        public async Task<bool> SwitchToRun()
        {
            if (V275_State == "Idle")
                return false;

            if (V275_State == "Running")
                return true;

            if (string.IsNullOrEmpty(V275_JobName))
                return false;

            if (!await Commands.GetIsRunReady())
            {
                Status = Commands.Status;
                return false;
            }

            if (!await Commands.RunJob(V275_JobName))
            {
                Status = Commands.Status;
                return false;
            }

            if (!await Commands.StartJob())
            {
                Status = Commands.Status;
                return false;
            }

            await Task.Run(() =>
            {
                DateTime start = DateTime.Now;
                while (V275_State != "Running")
                    if ((DateTime.Now - start) > TimeSpan.FromMilliseconds(30000))
                        return;
            });

            return V275_State == "Running";
        }

        public async Task<bool> SimulatorTogglePrint()
        {
            if (!await Commands.SimulatorStart())
                return false;

            LabelBegin = false;
            await Task.Run(() =>
            {
                DateTime start = DateTime.Now;
                while (!LabelBegin)
                    if ((DateTime.Now - start) > TimeSpan.FromMilliseconds(10000))
                        return;
            });

            return await Commands.SimulatorStop() && LabelBegin;
        }

        public async Task<FullReport?> Read(int repeat, bool getImage)
        {
            Status = string.Empty;

            if (repeat == 0)
            {
                var res = V275_State == "Running" ? await Commands.GetRepeatsAvailableRun() : await Commands.GetRepeatsAvailable();
                repeat = res == null ? 0 : res.Count > 0 ? res.First() : 0;
            }

            if (V275_State == "Editing" && repeat != 0)
                if (!await Inspect(repeat))
                    return null;

            FullReport report;
            if ((report = await GetReport(repeat, getImage)) == null)
                return null;

            if ((report.job = await GetJob()) == null)
                return null;

            if (V275_State == "Paused")
            {
                if (!await Commands.RemoveRepeat(repeat))
                {
                    Status = Commands.Status;
                    return null;
                }

                if (!await Commands.ResumeJob())
                {
                    Status = Commands.Status;
                    return null;
                }
            }
            return report;
        }
        public async Task<int> GetLatestRepeat()
        {
            var res = V275_State == "Running" ? await Commands.GetRepeatsAvailableRun() : await Commands.GetRepeatsAvailable();
            return res == null ? 0 : res.Count > 0 ? res.First() : 0;
        }
    }
}
