using System.Collections.Generic;
using Zepheus.FiestaLib;
using Zepheus.FiestaLib.Networking;
using Zepheus.Util;
using Zepheus.Zone.Game;
using Zepheus.Zone.Networking;

namespace Zepheus.Zone.Handlers
{
    public sealed class Handler9
    {
        [PacketHandler(CH9Type.SelectObject)]
        public static void SelectObjectHandler(ZoneClient client, Packet packet)
        {
            ushort id;
            if (!packet.TryReadUShort(out id))
            {
                Log.WriteLine(LogLevel.Warn, "Could not read entity select request.");
                return;
            }

            MapObject mo;
            // Try to see if there is a map object with this ID
            if (!client.Character.Map.Objects.TryGetValue(id, out mo))
            {
                return; // Nothing found. Just return lawl
            }

            mo.SelectedBy.Add(client.Character);

            if (mo is ZoneCharacter || mo is Mob)
            {
                client.Character.SelectedObject = mo;
                SendStatsUpdate(mo, client, false);
            }
        }

        [PacketHandler(CH9Type.DeselectObject)]
        public static void DeselectObjectHandler(ZoneClient client, Packet packet)
        {
            if (client.Character.SelectedObject != null)
            {
                client.Character.SelectedObject.SelectedBy.Remove(client.Character);
                client.Character.SelectedObject = null;
            }
        }

        [PacketHandler(CH9Type.AttackEntityMelee)]
        public static void AttackMeleeHandler(ZoneClient client, Packet packet)
        {
            client.Character.Attack(null);
        }

        [PacketHandler(CH9Type.AttackEntitySkill)]
        public static void AttackSkillHandler(ZoneClient client, Packet packet)
        {
            ushort skill;
            if (!packet.TryReadUShort(out skill))
            {
                Log.WriteLine(LogLevel.Warn, "Could not read skillID from attack entity skill function. {0}", client);
                return;
            }

            if (!client.Character.SkillsActive.ContainsKey(skill))
            {
                Log.WriteLine(LogLevel.Warn, "User tried to attack with a wrong skill. {0} {1} ", skill, client);
                return;
            }

            client.Character.AttackSkill(skill, null);
        }

        [PacketHandler(CH9Type.StopAttackingMelee)]
        public static void StopAttackMeleeHandler(ZoneClient client, Packet packet)
        {
            client.Character.AttackStop();
        }

        [PacketHandler(CH9Type.UseSkillWithTarget)]
        public static void UseSkillWithTargetHandler(ZoneClient client, Packet packet)
        {
            ushort skillid, victimid;
            if (!packet.TryReadUShort(out skillid) || !packet.TryReadUShort(out victimid))
            {
                Log.WriteLine(LogLevel.Warn, "Couldn't read useskill packet {0}", client);
                return;
            }
            Skill skill;
            if (!client.Character.SkillsActive.TryGetValue(skillid, out skill))
            {
                Log.WriteLine(LogLevel.Warn, "User tried to use a wrong skill. {0} {1} ", skillid, client);
                return;
            }


            MapObject victim;
            if (!client.Character.MapSector.Objects.TryGetValue(victimid, out victim))
            {
                Log.WriteLine(LogLevel.Warn, "User tried to do something with an unknown victim. {0} {1} {2}", skillid, victimid, client);
            }
            var self = client.Character;

            if (skill.Info.DemandType == 6)
            {
                if (!(victim is ZoneCharacter)) return;
                var zc = victim as ZoneCharacter;

                // Only Heal has this
                // Some heal calculation here
                uint amount = 12 * (uint)Program.Randomizer.Next(1, 300); //lulz

                if (amount > victim.MaxHP - victim.HP)
                {
                    amount = victim.MaxHP - victim.HP;
                }

                zc.HP += amount;

                ushort id = self.UpdateCounter;
                SendSkillStartSelf(self, skillid, victimid, id);
                SendSkillStartOthers(self, skillid, victimid, id);
                SendSkillOK(self);
                SendSkillAnimationForPlayer(self, skillid, id);
                // Damage as heal val :D
                SendSkill(self, id, victimid, amount, zc.HP, zc.UpdateCounter);
            }
            else
            {
                if (!(victim is Mob)) return;
                uint dmgmin = (uint)self.GetWeaponDamage(true);
                uint dmgmax = (uint)(self.GetWeaponDamage(true) + (self.GetWeaponDamage(true) % 3));
                uint amount = (uint)Program.Randomizer.Next((int)dmgmin, (int)dmgmax);
                if (amount > victim.HP)
                {
                    victim.HP = 0;
                }
                else {
                    victim.HP -= amount;
                }

                ushort id = self.UpdateCounter;
                SendSkillStartSelf(self, skillid, victimid, id);
                SendSkillStartOthers(self, skillid, victimid, id);
                SendSkillOK(self);
                SendSkillAnimationForPlayer(self, skillid, id);
                SendSkill(self, id, victimid, amount, victim.HP, victim.UpdateCounter, 0x01, 0x01);

                if (!victim.IsDead)
                {
                    victim.Attack(self);
                }
            }
        }

        [PacketHandler(CH9Type.UseSkillWithPosition)]
        public static void UseSkillWithPositionHandler(ZoneClient client, Packet packet)
        {
            ushort skillid;
            uint x, y;
            if (!packet.TryReadUShort(out skillid) || !packet.TryReadUInt(out x) || !packet.TryReadUInt(out y))
            {
                Log.WriteLine(LogLevel.Warn, "Couldn't read UseSkillWithPosition packet. {0} ", client);
                return;
            }

            Skill skill;
            if (!client.Character.SkillsActive.TryGetValue(skillid, out skill))
            {
                Log.WriteLine(LogLevel.Warn, "User tried to use a wrong skill. {0} {1} ", skillid, client);
                return;
            }

            var self = client.Character;
            var block = self.Map.Block;

            if (x == 0 || y == 0 || x > block.Width || y > block.Height)
            {
                Log.WriteLine(LogLevel.Warn, "User tried to use skill out of map boundaries. {0} {1} {2} {3}", x, y, skillid, client);
                return;
            }

            var pos = new Vector2((int)x, (int)y);

            if (skill.Info.MaxTargets <= 1)
            {
                // No AoE skill :s
                Log.WriteLine(LogLevel.Warn, "User tried to use skill with no MaxTargets or less than 1. {0} {1}", skillid, client);
                return;
            }

            self.AttackSkillAoE(skillid, x, y);
        }


        public static void SendAttackAnimation(MapObject from, ushort objectID, ushort attackspeed, byte stance)
        {
            using (var packet = new Packet(SH9Type.AttackAnimation))
            {
                packet.WriteUShort(from.MapObjectID);
                packet.WriteUShort(objectID);
                packet.WriteByte(stance);

                packet.WriteUShort(attackspeed);

                packet.WriteByte(4);
                packet.WriteByte(100);
                from.MapSector.Broadcast(packet);
            }
        }

        public static void SendAttackDamage(MapObject from, ushort objectID, ushort damage, bool crit, uint hpleft, ushort counter)
        {
            using (var packet = new Packet(SH9Type.AttackDamage))
            {
                packet.WriteUShort(from.MapObjectID);
                packet.WriteUShort(objectID);
                packet.WriteBool(crit);
                packet.WriteUShort(damage);
                packet.WriteUInt(hpleft);
                packet.WriteUShort(counter);
                packet.WriteByte(4);
                packet.WriteByte(100);

                from.MapSector.Broadcast(packet);
            }
        }

        public static void SendDieAnimation(MapObject from, ushort objectID)
        {
            // DO NOT SEND THIS TO THE DYING PLAYER, LOL
            using (var packet = new Packet(SH9Type.DieAnimation))
            {
                packet.WriteUShort(objectID);
                packet.WriteUShort(from.MapObjectID);
                from.MapSector.Broadcast(packet, objectID);
            }
        }

        public static void SendGainEXP(ZoneCharacter who, uint amount, ushort mobid = 0xFFFF)
        {
            using (var packet = new Packet(SH9Type.GainEXP))
            {
                packet.WriteUInt(amount);
                packet.WriteUShort(mobid);
                who.Client.SendPacket(packet);
            }
        }

        public static void SendLevelUPAnim(ZoneCharacter who, ushort mobid = 0xFFFF)
        {
            using (var packet = new Packet(SH9Type.LevelUPAnimation))
            {
                packet.WriteUShort(who.MapObjectID);
                packet.WriteUShort(mobid);
                who.MapSector.Broadcast(packet);
            }
        }

        public static void SendLevelUPData(ZoneCharacter who, ushort mobid = 0xFFFF)
        {
            using (var packet = new Packet(SH9Type.LevelUP))
            {
                packet.WriteByte(who.Level);
                packet.WriteUShort(mobid);
                who.WriteDetailedInfoExtra(packet, true);

                who.Client.SendPacket(packet);
            }
        }

        /*
        public static void SendResetStance(MapObject obj)
        {
            using (var packet = new Packet(SH9Type.ResetStance))
            {
                packet.WriteUShort(obj.MapObjectID);
                obj.MapSector.Broadcast(packet);
            }
        }*/
        public static void SendUpdateHP(ZoneCharacter character)
        {
            using (var p = new Packet(SH9Type.HealHP))
            {
                p.WriteUInt(character.HP);
                p.WriteUShort(character.UpdateCounter);
                character.Client.SendPacket(p);
            }
        }

        public static void SendUpdateSP(ZoneCharacter character)
        {
            using (var p = new Packet(SH9Type.HealSP))
            {
                p.WriteUInt(character.SP);
                p.WriteUShort(character.UpdateCounter);
                character.Client.SendPacket(p);
            }
        }

        public static void SendStatsUpdate(MapObject pObject, ZoneClient to, bool selectedby)
        {
            using (var packet = new Packet(SH9Type.StatUpdate))
            {
                packet.WriteBool(selectedby);
                packet.WriteUShort(pObject.MapObjectID);
                if (pObject is ZoneCharacter)
                {
                    ((ZoneCharacter)pObject).WriteUpdateStats(packet);
                }
                else
                {
                    ((Mob)pObject).WriteUpdateStats(packet);
                }
                to.SendPacket(packet);
            }
        }

        public static void SendSkillStartSelf(ZoneCharacter user, ushort skillid, ushort victim, ushort animid)
        {
            // 9 78 | [04 06] [8A 27] [E5 3F]
            using (var packet = new Packet(SH9Type.SkillUsePrepareSelf))
            {
                packet.WriteUShort(skillid);
                packet.WriteUShort(victim);
                packet.WriteUShort(animid);
                user.Client.SendPacket(packet);
            }
        }

        public static void SendSkillStartOthers(MapObject user, ushort skillid, ushort victim, ushort animid)
        {
            // 9 79 | [9A 26] [06 06] [8A 27] [97 2D] 
            using (var packet = new Packet(SH9Type.SkillUsePrepareOthers))
            {
                packet.WriteUShort(user.MapObjectID);
                packet.WriteUShort(skillid);
                packet.WriteUShort(victim);
                packet.WriteUShort(animid);
                user.MapSector.Broadcast(packet, user.MapObjectID);
            }
        }

        public static void SendSkillOK(ZoneCharacter user)
        {
            // 9 53 |
            using (var packet = new Packet(SH9Type.SkillAck))
            {
                user.Client.SendPacket(packet);
            }
        }

        public static void SendSkillAnimationForPlayer(MapObject user, ushort skillid, ushort animid)
        {
            // 9 87 | [E5 3F] [8A 27] [04 06]
            // 9 87 | [97 2D] [9A 26] [06 06]
            using (var packet = new Packet(SH9Type.SkillAnimation))
            {
                packet.WriteUShort(animid);
                packet.WriteUShort(user.MapObjectID);
                packet.WriteUShort(skillid);
                user.MapSector.Broadcast(packet);
            }
        }

        public static void SendSkill(MapObject user, ushort animid, ushort victimid, uint damage, uint newhp, ushort counter, byte special1 = 0x10, byte special2 = 0x00)
        {
            // 9 82 | [E5 3F] [8A 27] [01] [8A 27] [10 00] [09 00 00 00] [5E 00 00 00] [A7 4C]
            // 9 82 | [9A 35] [8A 27] [01] [C2 05] [10 00] [0A 00 00 00] [1D 01 00 00] [73 37]
            // 9 82 | [43 3C] [42 15] [01] [AC 4C] [01 01] [7A 02 00 00] [00 00 00 00] [35 09]
            // 9 82 | [0E 39] [42 15] [01] [00 4A] [21 01] [1C 03 00 00] [00 00 00 00] [8C 0E]
            using (var packet = new Packet(SH9Type.SkillAnimationTarget))
            {
                packet.WriteUShort(animid);
                packet.WriteUShort(user.MapObjectID);
                packet.WriteBool(true);
                packet.WriteUShort(victimid);
                packet.WriteByte(special1);
                packet.WriteByte(special2);
                packet.WriteUInt(damage);
                packet.WriteUInt(newhp);
                packet.WriteUShort(counter);
                user.MapSector.Broadcast(packet);
            }
        }

        public static void SendSkillNoVictim(MapObject user, ushort animid)
        {
            // 9 82 | [75 70] [32 29] [00]
            using (var packet = new Packet(SH9Type.SkillAnimationTarget))
            {
                packet.WriteUShort(animid);
                packet.WriteUShort(user.MapObjectID);
                packet.WriteBool(false);
                user.MapSector.Broadcast(packet);
            }
        }

        public static void SendSkill(MapObject user, ushort animid, List<SkillVictim> victims)
        {
            using (var packet = new Packet(SH9Type.SkillAnimationTarget))
            {
                packet.WriteUShort(animid);
                packet.WriteUShort(user.MapObjectID);
                packet.WriteByte((byte)(victims.Count > 255 ? 255 : victims.Count));
                for (byte i = 0; i < victims.Count && i != 255; i++)
                {
                    var victim = victims[i];
                    packet.WriteUShort(victim.MapObjectID);
                    packet.WriteByte(victim.Stance1);
                    packet.WriteByte(victim.Stance2);
                    packet.WriteUInt(victim.Damage);
                    packet.WriteUInt(victim.HPLeft);
                    packet.WriteUShort(victim.HPCounter);
                }
                user.MapSector.Broadcast(packet);
            }
        }

        public static void SendSkillPosition(MapObject user, ushort animid, ushort skillid, uint x, uint y)
        {
            // 9 81 | [32 29] [B8 10] [56 0A 00 00] [3A 27 00 00] [75 70]
            using (var packet = new Packet(SH9Type.SkillAnimationPosition))
            {
                packet.WriteUShort(user.MapObjectID);
                packet.WriteUShort(skillid);
                packet.WriteUInt(x);
                packet.WriteUInt(y);
                packet.WriteUShort(animid);
                user.MapSector.Broadcast(packet);
            }
        }
    }
}
