using System.Collections.Generic;
using RustExtended;
using UnityEngine;
using RageMods;
using System;

namespace Oxide.Plugins
{
    [Info("SXCZGangZone", "systemXcrackedZ", "1.0.0")]
    internal class SXCZGangZone : RustLegacyPlugin
    {
        public class Zone
        {
            public string GangZoneName;
            public string Name;
            public string ZoneCenter;
        }
        public class Statistic
        {
            public string GangZoneName;
            public int TotalZones;
            public int CaptureKills;
            public ulong FundBalance;
            public ulong LeaderID;
            public bool tpm;
            public string tppos;
            public bool captured;
            public bool ffire;
            public List<ulong> MembersID;
        }
        public class GangZone
        {
            public List<Zone> Zones = new List<Zone>();
            public List<Statistic> Statistics = new List<Statistic>();
        }
        GangZone Gang;
        void ss()
        {
            Gang.Statistics.Add(new Statistic
            {
                
            });
        }
        public List<GangZone> gangs = new List<GangZone>();

        //private readonly Dictionary<ulong, gangs> Invited = new Dictionary<ulong, gangs>();
        //private readonly Dictionary<gangs.Statistics, DateTime> Captured = new Dictionary<GangZone.Statistic, DateTime>();
    }
}