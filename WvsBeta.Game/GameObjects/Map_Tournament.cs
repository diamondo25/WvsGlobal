using System;
using System.Linq;
using WvsBeta.Common;
using WvsBeta.Common.Sessions;

namespace WvsBeta.Game.GameObjects
{
    public class Map_Tournament : Map
    {
        public enum GameKindEnum
        {
            Omok,
            MemoryGame,
        }

        private const int SLOTS = 32;
        public string[] CharacterNames = new string[SLOTS];

        // [Amount of people, Round]
        // So [1, 1] === second person in second round!!!
        public int[,] MatchTable = new int[SLOTS, 6];
        public int[,] Match = new int[SLOTS, 6];
        // Max 5 rounds
        public int CurrentRound { get; private set; } = -1;

        public int[] ItemID { get; private set; } = new int[2];

        // Amount of matches going on
        public int MatchGoingOn { get; private set; } = 0;
        public int RoundState { get; private set; } = 0;
        public GameKindEnum GameKind { get; private set; }
        public bool WaitingNextOperation { get; set; } = false;
        public bool PrizeSet { get; set; } = false;
        public bool GameOn { get; set; } = false;
        public long NextOperation { get; private set; }

        public Map_Tournament(int id) : base(id)
        {
            for (var i = 0; i < SLOTS; i++)
                CharacterNames[i] = null;
        }

        public int GetPeopleLeft(int round)
        {
            // 2 << (4 - 4) == 2
            // 2 << (4 - 3) == 4
            // 2 << (4 - 2) == 8
            // 2 << (4 - 1) == 16
            // 2 << (4 - 0) == 32
            return 2 << (4 - round);
        }

        public static int GetNextRoundIndex(int currentIndex)
        {
            return currentIndex / 2;
        }

        public void OnStart(Character character, GameKindEnum gameKind)
        {
            if (CurrentRound != -1) return;
            if (PrizeSet /*&& Characters.Count >= 25*/)
            {
                GameKind = gameKind;
                PrepareTournament();
                SendAvatarInfo(null);
                CurrentRound = 0;
                MatchGoingOn = 16; // Amount of matches running

                Match_MakeRoom();
            }
            else
            {
                // Sending wrong packet in BMS: Instead of TOURNAMENT_SET_PRIZE, its sending TOURNAMENT_INFO
                var packet = new Packet(ServerMessages.TOURNAMENT_SET_PRIZE);
                packet.WriteByte(0);
                packet.WriteBool(PrizeSet);
                character.SendPacket(packet);
            }
        }

        public void PrepareTournament()
        {
            Match = new int[SLOTS, 6];

            var players = this.Characters
                //.Where(x => x.Admin == false)
                .Take(SLOTS)
                .Select(x => x.ID)
                .ToArray();

            for (var i = 0; i < SLOTS && i < players.Length; i++)
            {
                var player = players[i];
                // fill uneven slots first
                if (i < (SLOTS / 2))
                    Match[i * 2, 0] = player;
                else
                    Match[(i - (SLOTS / 2)) + 1, 0] = player;
            }

            // Build matchtable
            for (var i = 0; i < SLOTS; i++)
            {
                for (var j = 0; j < 6; j++)
                {
                    MatchTable[i, j] = Match[i, j];
                }
            }
        }

        public void SendAvatarInfo(Character toCharacter)
        {
            var packet = new Packet(ServerMessages.TOURNAMENT_AVATAR_INFO);
            for (var i = 0; i < SLOTS; i++)
            {
                var id = Match[i, 0]; // Initial player list
                var player = GetPlayer(id);
                if (player == null)
                {
                    packet.WriteBool(false);
                }
                else
                {
                    packet.WriteBool(true);
                    PacketHelper.AddAvatar(packet, player);
                    packet.WriteString(player.Name);
                }
            }

            if (toCharacter != null)
                toCharacter.SendPacket(packet);
            else
                SendPacket(packet);
        }

        public void Match_MakeRoom()
        {
            var packet = new Packet(ServerMessages.TOURNAMENT_NOTICE_UEW);
            byte nextRound = (byte)GetPeopleLeft(CurrentRound + 1);
            packet.WriteByte(nextRound);

            var peopleLeft = GetPeopleLeft(CurrentRound);
            // TODO: Fix
            if (peopleLeft > 0 && false)
            {
                for (var i = 0; i < peopleLeft; i++)
                {
                    if (i % 2 == 1) continue;

                    var player1Id = Match[i, CurrentRound];
                    var player1 = GetPlayer(player1Id);
                    var player2Id = Match[i + 1, CurrentRound];
                    var player2 = GetPlayer(player2Id);

                    if (player1Id != 0 && player1 != null)
                    {
                        if (player2Id != 0 && player2 != null)
                        {
                            // Create miniroom, where Round = 'peopleLeft'. Owner is player1
                            // If an error occurs, kick player to return field id (and make player 2 winner)
                            // Otherwise, add player2 to the omok. If that fails, make player 1 winner.

                            // Could not create room
                            if (true)
                            {
                                // Kick player
                                player1.ChangeMap(this.ForcedReturn);
                                SetWinner(player2Id, false);

                                if (CurrentRound != 4)
                                    player2.SendPacket(packet);
                            }
                            else
                            {
                                // Other player could not enter
                                if (true)
                                {
                                    SetWinner(player1Id, false);
                                    if (CurrentRound != 4)
                                        player1.SendPacket(packet);
                                }
                            }
                        }
                        else
                        {
                            // Make player1 winner
                            SetWinner(player1Id, false);
                            if (CurrentRound != 4)
                                player1.SendPacket(packet);

                            Match[i + 1, CurrentRound] = 0;
                        }
                    }
                    else
                    {
                        Match[i, CurrentRound] = 0;
                        if (player2Id != 0 && player2 != null)
                        {
                            // Make player 2 winner (bug in BMS: no packet sent)

                            if (CurrentRound != 4 && false)
                                player2.SendPacket(packet);

                            SetWinner(player2Id, false);
                        }
                        else
                        {
                            // No player 2??? Continue to next round
                            Match[i + 1, CurrentRound] = 0;
                            // Make sure next round is fixed
                            Match[GetNextRoundIndex(i) + 1, CurrentRound + 1] = 0;

                            MatchGoingOn--;
                        }
                    }
                }
            }

            if (MatchGoingOn <= 0)
            {
                MatchGoingOn = 0;
                RoundState = 1;
            }
            else
            {
                RoundState = 0;
                NextOperation = MasterThread.CurrentTime + 12000;
                WaitingNextOperation = true;
            }
        }

        public int FindMatchIndex(int characterId)
        {
            var peopleLeft = GetPeopleLeft(CurrentRound);
            if (peopleLeft <= 0) return -1;
            
            for (var i = 0; i < peopleLeft; i++)
            {
                if (Match[i, CurrentRound] == characterId) return i;
            }

            return -1;
        }

        public void SetWinner(int characterId, bool draw)
        {
            var idx = FindMatchIndex(characterId);
            var currentRound = CurrentRound;
            if (currentRound >= 100) currentRound -= 100;

            var nextRoundIndex = GetNextRoundIndex(idx);

            if (currentRound < 0 ||
                // Opponent already set ¡_¡
                Match[nextRoundIndex, currentRound + 1] != 0)
            {
                throw new Exception();
            }

            var x = draw ? 0 : characterId;
            Match[nextRoundIndex, currentRound + 1] = x;
            MatchTable[nextRoundIndex, currentRound + 1] = x;
            if (!draw)
            {
                Match[idx + 1, CurrentRound] = 0;
            }

            MatchGoingOn--;
        }

        public override bool FilterAdminCommand(Character character, CommandHandling.CommandArgs command)
        {
            switch (command.Command)
            {
                case "omok":

                    OnStart(character, GameKindEnum.Omok);
                    return true;

                case "senduser":
                    // Arg: <charname> <portalname>
                    if (command.Count == 2)
                    {
                        var charName = command[0].Value;
                        var portalName = command[1].Value;

                        if (Portals.TryGetValue(portalName, out Portal portal))
                        {
                            var c = FindUser(charName);
                            if (c != null)
                            {
                                c.ChangeMap(ID, portal);
                            }
                        }
                    }
                    return true;

                case "reset":
                    // Do map reset....
                    return true;

                case "matchtable":
                    {

                        SendAvatarInfo(character);

                        var p = new Packet(ServerMessages.TOURNAMENT_MATCH_TABLE);
                        // This one is nasty. We cannot write lots of data at once.
                        for (var i = 0; i < SLOTS; i++)
                        {
                            for (var j = 0; j < 6; j++)
                            {
                                p.WriteInt(character.ID);
                            }
                        }
                        var round = CurrentRound;
                        if (round >= 100) round -= 100;
                        p.WriteByte((byte)round);
                        character.SendPacket(p);
                        return true;
                    }



                case "prize":
                    // Arg: <itemid1> <itemid2>
                    // TODO add validation
                    if (command.Count == 2)
                    {
                        PrizeSet = true;
                        var p = new Packet(ServerMessages.TOURNAMENT_SET_PRIZE);
                        ItemID[0] = command[0].GetInt32();
                        ItemID[1] = command[1].GetInt32();

                        p.WriteBool(true);
                        p.WriteByte(1);
                        p.WriteInt(ItemID[0]);
                        p.WriteInt(ItemID[1]);
                        SendPacket(p);

                        // For the user himself
                        p = new Packet(ServerMessages.TOURNAMENT_SET_PRIZE);
                        p.WriteBool(true);
                        p.WriteByte(0);
                        character.SendPacket(p);
                    }
                    return true;

                case "giveprize":
                    // Arg: <itemid> <portalname> 
                    if (command.Count == 2)
                    {
                        var itemid = command[0].GetInt32();
                        var portalName = command[1].Value;

                        if (Portals.TryGetValue(portalName, out Portal portal))
                        {
                            var item = BaseItem.CreateFromItemID(itemid);
                            item.GiveStats(ItemVariation.None);
                            var reward = Reward.Create(item);

                            // Drops through floor with X + 50 on pt 2
                            DropPool.Create(
                                reward,
                                0,
                                0,
                                DropType.Normal,
                                0,
                                new Pos((short)(portal.X + 40), portal.Y),
                                portal.X + 40,
                                0,
                                false,
                                0,
                                true, // Yes, by pet??!? 
                                false
                            );
                        }
                    }
                    return true;
            }
            return false;
        }

        public override bool HandlePacket(Character character, Packet packet, ClientMessages opcode)
        {
            switch (opcode)
            {
                // /matchtable command, expects bool arg
                case ClientMessages.FIELD_TOURNAMENT_MATCHTABLE:
                    {
                        if (packet.ReadBool())
                        {
                            SendAvatarInfo(character);
                        }


                        // BIG FAT NOTE: THE UI ONLY DISPLAYS 1, 2, 4 AND 8 USERS
                        // DO NOT TRY TO FIX IT; THE UI DOESNT SUPPORT MORE

                        var p = new Packet(ServerMessages.TOURNAMENT_MATCH_TABLE);
                        // This one is nasty. We cannot write lots of data at once.
                        for (var i = 0; i < SLOTS; i++)
                        {
                            for (var j = 0; j < 6; j++)
                            {
                                p.WriteInt(MatchTable[i, j]);
                            }
                        }
                        var round = CurrentRound;
                        if (round >= 100) round -= 100;
                        p.WriteByte((byte)round);
                        character.SendPacket(p);
                        return true;
                    }
                default: return base.HandlePacket(character, packet, opcode);
            }
        }
    }
}
