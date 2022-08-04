﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using V725_REST_lib.Models;

namespace V725_REST_lib
{
    public class Controller
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

        public string V275_State { get; set; } = "";
        public string V275_JobName { get; set; } = "";

        public Events_System SetupDetectEvent { get; set; }
        private bool SetupDetectEnd { get; set; } = false;

        bool LabelBegin { get; set; } = false;
        bool LabelEnd { get; set; } = false;

        public string Host { get => Commands.Host; set => Commands.Host = value; }
        public uint SystemPort { get => Commands.SystemPort; set => Commands.SystemPort = value; }
        public uint NodeNumber { get => Commands.NodeNumber; set => Commands.NodeNumber = value; }

        public string Status
        {
            get { return _Status; }
            set { _Status = value; }
        }
        private string _Status;

        public Controller()
        {
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

        private void WebSocket_Heartbeat(Events_System ev)
        {
            string state = char.ToUpper(ev.data.state[0]) + ev.data.state.Substring(1);

            if (V275_State != state)
            {
                V275_State = state;

                if (V275_State != "Idle")
                {
                    new Task(async () =>
                    {
                        if (await Commands.GetJob())
                        {
                            V275_JobName = Commands.Job.name;
                        }
                        else
                        {
                            V275_JobName = "";
                        }
                    }).Start();
                }
                else
                {
                    V275_JobName = "";
                }
            }
        }
        private void WebSocket_StateChange(Events_System ev)
        {
            ev.data.state = ev.data.toState;
            WebSocket_Heartbeat(ev);
            //V275_State = char.ToUpper(ev.data.toState[0]) + ev.data.toState.Substring(1);
        }

        public async Task<bool> GetJob()
        {
            if (!await Commands.GetJob())
            {
                Status = Commands.Status;
                return false;
            }

            foreach(var sector in Commands.Job.sectors)
            {
                if(sector.type == "blemish")
                {
                    if (!await Commands.GetMask(sector.name))
                    {
                        Status = Commands.Status;
                        return false;
                    }
                    sector.blemishMask = Commands.Mask;
                }
            }
            return true;
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

        public async Task<bool> GetReport(int repeat, bool getImage)
        {
            if (V275_State == "Editing")
            {
                if (!await Commands.GetReport())
                {
                    Status = Commands.Status;
                    return false;
                }
            }
            else
            {
                if (!await Commands.GetReport(repeat))
                {
                    Status = Commands.Status;
                    return false;
                }
            }

            if (getImage)
                if (!await Commands.GetRepeatsImage(repeat))
                {
                    if (!Commands.Status.StartsWith("Gone"))
                    {
                        Status = Commands.Status;
                        return false;
                    }
                }

            return true;
        }

        public async Task<bool> DeleteSectors()
        {
            if (!await Commands.GetJob())
            {
                Status = Commands.Status;
                return false;
            }

            foreach (var sec in Commands.Job.sectors)
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
            if (!await Commands.GetDetect())
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

        public List<Sector_New_Verify> CreateSectors(Events_System ev, string tableID)
        {
            int d1 = 1;
            int d2 = 1;

            List<Sector_New_Verify> lst = new List<Sector_New_Verify>();


            foreach (var val in ev.data.detections)
            {
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

                Symbologies.Symbol sym = Commands.Symbologies.Find((e) => e.symbology == val.symbology);

                if (sym == null)
                    continue;

                if (sym.regionType == "verify1D")
                    verify.id = d1++;
                else
                    verify.id = d2++;

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

            if (!await Commands.SimulatorStop())
                return false;

            return LabelBegin;
        }

        private void read()
        {


        }

        public async Task<bool> Read(int repeat,bool getImage)
        {
            Status = string.Empty;

            if (repeat == 0)
            {
                bool ok = false;
                if (V275_State == "Running")
                    ok = await Commands.GetRepeatsAvailableRun();
                else
                    ok = await Commands.GetRepeatsAvailable();

                if (!ok)
                {
                    if (Commands.Available == null)
                        repeat = 0;
                    else
                    {
                        Status = Commands.Status;
                        return false;
                    }
                }
                else
                {
                    if (Commands.Available.Count > 0)
                        repeat = Commands.Available.First();
                }

            }

            if (V275_State == "Editing" && repeat != 0)
                if (!await Inspect(repeat))
                     return false;

            if (!await GetReport(repeat, getImage))
                return false;

            if (!await GetJob())
                return false;

            if (V275_State == "Paused")
            {
                if (!await Commands.RemoveRepeat(repeat))
                {
                    Status = Commands.Status;
                    return false;
                }

                if (!await Commands.ResumeJob())
                {
                    Status = Commands.Status;
                    return false;
                }
            }
            return true;
        }
        public async Task<int> GetLatestRepeat()
        {
            bool ok;
            if (V275_State == "Running")
                ok = await Commands.GetRepeatsAvailableRun();
            else
                ok = await Commands.GetRepeatsAvailable();

            if (!ok)
            {
                if (Commands.Available == null)
                    return 0;
                else
                {
                    Status = Commands.Status;
                }
            }
            else
            {
                if (Commands.Available.Count > 0)
                    return Commands.Available.First();
            }

            return -9999;
        }
    }
}
