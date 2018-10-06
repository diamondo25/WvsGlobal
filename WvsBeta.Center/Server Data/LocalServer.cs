using System.Net;
using WvsBeta.Common.Sessions;

namespace WvsBeta.Center
{
    public enum LocalServerType
    {
        Login,
        World,
        Game,
        Shop,
        MapGen,
        Claim,
        ITC,
        Unk
    }


    public class LocalServer
    {
        public ushort Port { get; set; }
        public IPAddress PublicIP { get; set; }
        public IPAddress PrivateIP { get; set; }
        public string Name { get; set; }
        public LocalServerType Type { get; set; }
        public int Connections { get; set; }
        public bool Connected { get; set; }
        public LocalConnection Connection { get; set; }
        public LocalConnection TransferConnection { get; set; }
        public byte ChannelID { get; set; }

        public bool IsReallyUsed { get; set; }

        public bool InMaintenance { get; set; }
        public double RateMobEXP { get; set; }
        public double RateMesoAmount { get; set; }
        public double RateDropChance { get; set; }

        private double RateMobEXP_Default;
        private double RateMesoAmount_Default;
        private double RateDropChance_Default;

        public LocalConnection ActiveServerConnection => InMaintenance ? TransferConnection : Connection;
        public bool IsGameServer => Type == LocalServerType.Game;

        public LocalServer()
        {
            Connected = false;
            Connections = 0;
            InMaintenance = false;
        }

        public void SetConnection(LocalConnection lc)
        {
            if (InMaintenance)
            {
                if (lc == null)
                {
                    // Other party disconnected; swap
                    Connection = TransferConnection;
                    TransferConnection = null;
                    InMaintenance = false;
                    Program.MainForm.LogAppend("Migrated server!");
                }
                else
                {
                    TransferConnection = lc;
                }
            }
            else
            {
                Connection = lc;
                Connected = lc != null;
                Connections = 0;
            }
        }

        public void EncodeForTransfer(Packet pw)
        {
            pw.WriteDouble(RateMobEXP);
            pw.WriteDouble(RateMesoAmount);
            pw.WriteDouble(RateDropChance);
        }

        public void DecodeForTransfer(Packet pr)
        {
            RateMobEXP = pr.ReadDouble();
            RateMesoAmount = pr.ReadDouble();
            RateDropChance = pr.ReadDouble();
        }

        public void SetRates(double exp, double drop, double meso, bool broadcastChange, bool isDefault = false)
        {
            RateMobEXP = exp;
            RateDropChance = drop;
            RateMesoAmount = meso;
            if (isDefault)
            {
                RateMobEXP_Default = exp;
                RateDropChance_Default = drop;
                RateMesoAmount_Default = meso;
            }

            if (broadcastChange)
            {
                var p = new Packet(ISServerMessages.ChangeRates);
                p.WriteDouble(RateMobEXP);
                p.WriteDouble(RateMesoAmount);
                p.WriteDouble(RateDropChance);

                ActiveServerConnection?.SendPacket(p);
            }
        }

        public void ResetRates()
        {
            SetRates(RateMobEXP_Default, RateDropChance_Default, RateMesoAmount_Default, true);
        }
    }
}
