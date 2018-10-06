using System.Diagnostics;
using WvsBeta.Common.Sessions;

namespace WvsBeta.Game
{
    public enum Conti : byte
    {
        Dormant = 0x01, // Moved to 0x00 in later versions
        Wait,
        Start,
        Move,
        Mobgen,
        Mobdestroy,
        End,
        TargetStartfield,
        TargetStartShipmoveField,
        TargetWaitfield,
        TargetMovefield,
        TargetEndfield,
        TargetEndShipmoveField,
    }

    public partial class ContinentMan
    {

        public CONTIMOVE[] ContiMoveArray { get; private set; }
        public long LastUpdateTime { get; private set; }

        public static ContinentMan Instance { get; private set; }


        public CONTIMOVE FindContiMove(int fieldId)
        {
            foreach (var contimove in ContiMoveArray)
            {
                if (contimove.FieldIdStartShipMove == fieldId ||
                    contimove.FieldIdMove == fieldId)
                {
                    return contimove;
                }
            }

            return null;
        }

        public int GetInfo(int fieldId, int flag)
        {
            var cm = FindContiMove(fieldId);
            if (cm == null)
            {
                return 0;
            }

            // Center::ms_bServerCheckup thing here... meh

            if (flag == 1) return cm.EventDoing ? 1 : 0;
            if (flag == 0) return (byte)cm.State;

            return -1;
        }

        public void MoveField(int fieldFrom, int fieldTo)
        {
            if (!DataProvider.Maps.TryGetValue(fieldFrom, out var field)) return;

            foreach (var character in field.Characters.ToArray())
            {
                // Little hack
                if (character.PrimaryStats.HP == 0)
                {
                    // Dead
                    character.ChangeMap(field.ReturnMap);
                }
                else
                {
                    character.ChangeMap(fieldTo);
                }
            }
        }

        public void OnAllSummonedMobRemoved(int fieldId)
        {
            // Actual bug in GMS here:
            // This does not reset the 'eventdoing' value, thus re-entering the map
            // will show the ballrog ship again
            // Kassy said they fixed it in EMS by not sending this packet at all

            foreach (var contimove in ContiMoveArray)
            {
                if (contimove.FieldIdMove == fieldId)
                {
                    SendContiPacket(fieldId, Conti.TargetMovefield, Conti.Mobdestroy);
                }
            }

        }

        public void SendContiPacket(int fieldId, Conti target, Conti flag)
        {
            if (!DataProvider.Maps.TryGetValue(fieldId, out var field)) return;

            var packet = new Packet(ServerMessages.CONTIMOVE);
            packet.WriteByte((byte)target);
            packet.WriteByte((byte)flag);

            field.SendPacket(packet);
        }

        public void SetReactorState(int fieldId, string sName, int state)
        {
            // TODO: Implement
        }

        public void Update(long tCur)
        {
            LastUpdateTime = tCur;

            foreach (var contimove in ContiMoveArray)
            {
                var curState = contimove.GetState();
                // Trace.WriteLine("State: " + curState + " Next in seconds: " + ((contimove.NextBoardingTime - tCur) / 1000));
                switch (curState)
                {
                    case Conti.Mobgen:
                        // Summon mob
                        contimove.SummonMob();

                        SendContiPacket(
                            contimove.FieldIdMove, 
                            Conti.TargetMovefield,
                            Conti.Mobgen
                        );
                        break;
                    case Conti.Mobdestroy:
                        // Remove mob
                        contimove.DestroyMob();
                        
                        SendContiPacket(
                            contimove.FieldIdMove,
                            Conti.TargetMovefield,
                            Conti.Mobdestroy
                        );
                        break;

                    case Conti.End:
                        // Finish moving
                        SendContiPacket(
                            contimove.FieldIdEndShipMove,
                            Conti.TargetEndShipmoveField,
                            Conti.End
                        );
                        MoveField(contimove.FieldIdMove, contimove.FieldIdEnd);
                        MoveField(contimove.FieldIdCabin, contimove.FieldIdEnd);

                        if (contimove.ReactorName?.Length > 0)
                        {
                            SetReactorState(contimove.FieldIdEndShipMove, contimove.ReactorName, contimove.StateOnEnd);
                        }
                        break;
                        
                    case Conti.Start:
                        SendContiPacket(
                            contimove.FieldIdStartShipMove,
                            Conti.TargetStartShipmoveField,
                            Conti.Start
                        );
                        MoveField(contimove.FieldIdWait, contimove.FieldIdMove);

                        if (contimove.ReactorName?.Length > 0)
                        {
                            SetReactorState(contimove.FieldIdStartShipMove, contimove.ReactorName, contimove.StateOnStart);
                        }
                        break;

                }
            }

        }
    }
}
