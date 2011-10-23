using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Reflection;
using Zepheus.Database;
using Zepheus.FiestaLib;
using Zepheus.FiestaLib.Data;
using Zepheus.FiestaLib.Networking;
using Zepheus.Services.DataContracts;
using Zepheus.Util;
using Zepheus.World.Data;
using Zepheus.World.Handlers;

namespace Zepheus.World.Networking
{
    public sealed class WorldClient : Client
    {
        public bool Authenticated { get; set; }
        public string Username { get; set; }
        public int AccountID { get; set; }
        public byte Admin { get; set; }

        public ushort RandomID { get; set; } //this ID is used to authenticate later on.
        public Dictionary<byte, WorldCharacter> Characters { get; private set; }

        public WorldCharacter Character { get; set; }
        public string CharacterName { get { return GetCharacterName(); } }

        public DateTime lastPing { get; set; }
        public bool Pong { get; set; }

        public WorldClient(Socket socket)
            : base(socket)
        {
            base.OnPacket += new EventHandler<PacketReceivedEventArgs>(WorldClient_OnPacket);
            base.OnDisconnect += new EventHandler<SessionCloseEventArgs>(WorldClient_OnDisconnect);
        }

        public string GetCharacterName() {
            if(Character == null) 
                return string.Empty;
            else return Character.Character.Name;
        }

        void WorldClient_OnDisconnect(object sender, SessionCloseEventArgs e)
        {
            Log.WriteLine(LogLevel.Debug, "{0} Disconnected.", this.Host);
            ClientManager.Instance.RemoveClient(this);
        }

        void WorldClient_OnPacket(object sender, PacketReceivedEventArgs e)
        {
            if (!Authenticated && !(e.Packet.Header == 3 && e.Packet.Type == 15)) return; //do not handle packets if not authenticated!
            MethodInfo method = HandlerStore.GetHandler(e.Packet.Header, e.Packet.Type);
            if (method != null)
            {
                Action action = HandlerStore.GetCallback(method, this, e.Packet);
                Worker.Instance.AddCallback(action);
            }
            else
            {
                Log.WriteLine(LogLevel.Debug, "Unhandled packet: {0}", e.Packet);
            }
        }

        public bool LoadCharacters() 
        {
            if (!Authenticated) return false;
            Characters = new Dictionary<byte, WorldCharacter>();
            try
            {
                foreach (var ch in Program.Entity.Characters.Where(chr => chr.AccountID == this.AccountID))
                {
                    Characters.Add(ch.Slot, new WorldCharacter(ch));
                }
            }
            catch (Exception ex)
            {
                Log.WriteLine(LogLevel.Exception, "Error loading characters from {0}: {1}", Username, ex.InnerException.ToString());
                return false;
            }
            return true;
        }

        public ClientTransfer GenerateTransfer(byte slot)
        {
            if (!Characters.ContainsKey(slot))
            {
                Log.WriteLine(LogLevel.Warn, "Generating transfer for slot {0} which {1} doesn't own.", slot, Username);
                return null;
            }
            WorldCharacter character;
            if(Characters.TryGetValue(slot, out character)) {
                return new ClientTransfer(AccountID, Username, character.Character.Name, RandomID, Admin, this.Host);
            } else return null;
        }

        public WorldCharacter CreateCharacter(string name, byte slot, byte hair, byte color, byte face, Job job, bool ismale)
        {
            if (Characters.ContainsKey(slot) || slot > 5)
                return null;
            //TODO: check if hair etc are actual beginner ones! (premium hack)

            BaseStatsEntry stats = DataProvider.Instance.JobBasestats[job];
            if (stats == null)
            {
                Log.WriteLine(LogLevel.Warn, "Houston, we have a problem! Jobstats not found for job {0}", job.ToString());
                return null;
            }

            Character newchar = new Character();
            newchar.AccountID = this.AccountID;
            newchar.CharLevel = 1;
            newchar.Name = name;
            newchar.Face = face;
            newchar.Hair = hair;
            newchar.HairColor = color;
            newchar.Job = (byte)job;
            newchar.Male = ismale;
            newchar.Slot = slot;
            newchar.XPos = 7636;
            newchar.YPos = 4610;
            newchar.HP = (short)stats.MaxHP;
            newchar.SP = (short)stats.MaxSP;
            newchar.HPStones = (short)stats.MaxHPStones;
            newchar.SPStones = (short)stats.MaxSPStones;
            Program.Entity.AddToCharacters(newchar);
            int charID = newchar.ID;
            ushort begineqp = GetBeginnerEquip(job);
            if (begineqp > 0)
            {
                DatabaseEquip eqp = new DatabaseEquip();
                eqp.EquipID = begineqp;
                eqp.Slot = (short)((job == Job.Archer) ? -10 : -12);
                newchar.Equips.Add(eqp);
            }
            Program.Entity.SaveChanges();
            WorldCharacter tadaa = new WorldCharacter(newchar, (job == Job.Archer) ? (byte)12 : (byte)10, begineqp);
            Characters.Add(slot, tadaa);
            return tadaa;
        }

        //TODO: move to helper class?
        private ushort GetBeginnerEquip(Job job)
        {
            ushort equipID;
            switch (job)
            {
                case Job.Archer:
                    equipID = 1250;
                    break;
                case Job.Fighter:
                    equipID = 250;
                    break;
                case Job.Cleric:
                    equipID = 750;
                    break;
                case Job.Mage:
                    equipID = 1750;
                    break;
                case Job.Trickster:
                    equipID = 57363;
                    break;
                default:
                    Log.WriteLine(LogLevel.Exception, "{0} is creating a wrong job (somehow)", this.Username);
                    return 0;
            }
            return equipID;
        }
    }
}
