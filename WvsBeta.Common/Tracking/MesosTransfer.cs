// ReSharper disable InconsistentNaming

using log4net;

namespace WvsBeta.Common.Tracking
{
    public class MesosTransfer
    {
        private static ILog log = LogManager.GetLogger("TranferLog");

        public int mesosTranferAmount { get; set; }
        public int mesosTranferFrom { get; set; }
        public int mesosTranferTo { get; set; }
        public string mesosTransferType { get; set; }

        // Identification for multi-transfer events, such as selling or buying an item
        public string transferId { get; set; }

        public static void PlayerDropMesos(int droppee, int amount, string transferId)
        {
            log.Info(new MesosTransfer
            {
                mesosTranferAmount = -amount,
                mesosTranferFrom = droppee,
                mesosTranferTo = -1,
                mesosTransferType = "PlayerDropMesos",
                transferId = transferId,
            });
        }

        public static void PlayerLootMesos(int droppee, int looter, int amount, string transferId)
        {
            
            log.Info(new MesosTransfer
            {
                mesosTranferAmount = amount,
                mesosTranferFrom = droppee,
                mesosTranferTo = looter,
                mesosTransferType = "PlayerLootMesos",
                transferId = transferId,
            });
        }

        public static void PlayerBuysFromShop(int playerId, int npcId, int amount, string transferId)
        {
            log.Info(new MesosTransfer
            {
                mesosTranferAmount = -amount,
                mesosTranferFrom = npcId,
                mesosTranferTo = playerId,
                mesosTransferType = "PlayerBuysFromShop",
                transferId = transferId,
            });
        }

        public static void PlayerBuysFromPersonalShop(int srcPlayerId, int destPlayerId, int amount, string transferId)
        {
            log.Info(new MesosTransfer
            {
                mesosTranferAmount = -amount,
                mesosTranferFrom = srcPlayerId,
                mesosTranferTo = destPlayerId,
                mesosTransferType = "PlayerBuysFromPersonalShop",
                transferId = transferId,
            });
            log.Info(new MesosTransfer
            {
                mesosTranferAmount = amount,
                mesosTranferFrom = destPlayerId,
                mesosTranferTo = srcPlayerId,
                mesosTransferType = "PlayerBuysFromPersonalShop",
                transferId = transferId,
            });
        }

        public static void PlayerSellsToShop(int playerId, int npcId, int amount, string transferId)
        {
            log.Info(new MesosTransfer
            {
                mesosTranferAmount = amount,
                mesosTranferFrom = playerId,
                mesosTranferTo = npcId,
                mesosTransferType = "PlayerSellsToShop",
                transferId = transferId,
            });
        }

        // When a player does a simple sell action to an (event) NPC, this should be used
        public static void PlayerReceivedFromNPC(int playerId, int npcId, int amount, string transferId)
        {
            log.Info(new MesosTransfer
            {
                mesosTranferAmount = amount,
                mesosTranferFrom = npcId,
                mesosTranferTo = playerId,
                mesosTransferType = "PlayerReceivedFromNPC",
                transferId = transferId,
            });
        }

        // When a player does a simple buy action to an (event) NPC, this should be used
        public static void PlayerGaveToNPC(int playerId, int npcId, int amount, string transferId)
        {
            log.Info(new MesosTransfer
            {
                mesosTranferAmount = -amount,
                mesosTranferFrom = playerId,
                mesosTranferTo = npcId,
                mesosTransferType = "PlayerGaveToNPC",
                transferId = transferId,
            });
        }

        public static void PlayerStoreMesos(int playerId, int amount)
        {
            log.Info(new MesosTransfer
            {
                mesosTranferAmount = -amount,
                mesosTranferFrom = playerId,
                mesosTranferTo = 0,
                mesosTransferType = "PlayerStoreMesos",
                transferId = null,
            });
        }

        public static void PlayerRetrieveMesos(int playerId, int amount)
        {
            log.Info(new MesosTransfer
            {
                mesosTranferAmount = amount,
                mesosTranferFrom = playerId,
                mesosTranferTo = 0,
                mesosTransferType = "PlayerRetrieveMesos",
                transferId = null,
            });
        }

        public static void PlayerUsedSkill(int playerId, int amount, int skillId)
        {
            log.Info(new MesosTransfer
            {
                mesosTranferAmount = -amount,
                mesosTranferFrom = playerId,
                mesosTranferTo = 0,
                mesosTransferType = "PlayerUsedSkill",
                transferId = "skill-" + skillId,
            });
        }

        public static void PlayerTradePutUp(int srcPlayerId, int amount, string transferId)
        {
            log.Info(new MesosTransfer
            {
                mesosTranferAmount = -amount,
                mesosTranferFrom = srcPlayerId,
                mesosTranferTo = 0,
                mesosTransferType = "PlayerTradePutUp",
                transferId = transferId,
            });
        }

        public static void PlayerTradeReverted(int srcPlayerId, int amount, string transferId)
        {
            log.Info(new MesosTransfer
            {
                mesosTranferAmount = amount,
                mesosTranferFrom = srcPlayerId,
                mesosTranferTo = 0,
                mesosTransferType = "PlayerTradeReverted",
                transferId = transferId,
            });
        }

        public static void PlayerTradeExchange(int srcPlayerId, int destPlayerId, int amount, string transferId)
        {
            log.Info(new MesosTransfer
            {
                mesosTranferAmount = amount,
                mesosTranferFrom = srcPlayerId,
                mesosTranferTo = destPlayerId,
                mesosTransferType = "PlayerTradeExchange",
                transferId = transferId,
            });
        }
    }
}
