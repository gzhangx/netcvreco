﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WpfRoadApp
{
    public interface IRecordStarter
    {
        void StartRecord();
        void EndRecord();
    }
    public class CommandInfo
    {
        public double timePositionMs { get; set; }
        public double timeMs { get; set; }
        public string Command { get; set; }
        public int CommandParam { get; set; }

        public override string ToString()
        {
            return $"CMD {Command} {CommandParam} {timePositionMs} {timeMs}";
        }
        public void Load(string s)
        {
            var parts = s.Split(' ');
            Load(parts, 0);
        }
        public void Load(string[] parts, int off)
        {
            if (parts[off] != "CMD") throw new ArgumentException("bad format");
            Command = parts[off+1];
            CommandParam = Convert.ToInt32(parts[off+2]);
            timePositionMs = Convert.ToDouble(parts[off+3]);
            timeMs = Convert.ToDouble(parts[off+4]);
        }
    }
    public class CommandRecorder
    {
        IRecordStarter recorder = null;
        public CommandRecorder(IRecordStarter r, string saveDir = "orig")
        {
            DefaultSaveDir = saveDir;
            recorder = r;
        }
        public string DefaultSaveDir
        {
            get;
            private set;
        }
        public DateTime startTime;
        public List<CommandInfo> Commands = new List<CommandInfo>();
        public bool Inited
        {
            get; private set;
        }
        public void Init()
        {
            startTime = DateTime.Now;
            recorder.StartRecord();
            Commands = new List<CommandInfo>();
            Inited = true;
        }
        public void Stop()
        {
            Inited = false;
            recorder.EndRecord();
            Save();
        }
        private Dictionary<string, bool> isDriveCmd = new Dictionary<string, bool>
        {
            {"D",true },
            {"R",true },
            {"V",false },
        };
        public void AddCommandInfo(CommandInfo info)
        {
            info.timePositionMs = DateTime.Now.Subtract(startTime).TotalMilliseconds;
            var prior = Commands.LastOrDefault(c=>isDriveCmd[c.Command]);
            if (prior != null)
            {
                prior.timeMs = info.timePositionMs - prior.timePositionMs;
            }
            Commands.Add(info);
        }
        public void Save()
        {
            List<string> s = new List<string>();
            Commands.ForEach(c => s.Add(c.ToString()));
            File.WriteAllLines(SaveFileName, s.ToArray());
        }

        protected string SaveFileName
        {
            get
            {
                return $"{DefaultSaveDir}\\cmds.txt";
            }
        }
        public void Load()
        {
            Commands.Clear();
            foreach(var line in File.ReadAllLines(SaveFileName))
            {
                if (line.Trim() != "")
                {
                    var ci = new CommandInfo();
                    ci.Load(line);
                    Commands.Add(ci);
                }
            }
        }
    }
}
