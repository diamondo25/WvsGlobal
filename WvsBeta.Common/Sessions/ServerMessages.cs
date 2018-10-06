namespace WvsBeta.Common.Sessions
{
	public enum ServerMessages : byte
    {
        ___START_SOCKET = 0,
        CHECK_PASSWORD_RESULT = 1,
        //Confirm that the server can handle the connection
        CHECK_USER_LIMIT_RESULT = 2,

        //Set Gender result
        SET_ACCOUNT_RESULT = 3,

        //EULA - Not implemented
        CONFIRM_EULA_RESULT = 4,

        // 5 does not exist

        // Pin Codes
        PIN_OPERATION = 6, // Not Implemented
        PIN_ASSIGNED = 7, // Not Implemented

        WORLD_INFORMATION = 8,

        //Confusing naming by Nexon, is select channel in selected world
        SELECT_WORLD_RESULT = 9,
        //Confusing naming, basically connect to server header :)
        SELECT_CHARACTER_RESULT = 10,
        CHECK_CHARACTER_NAME_AVAILABLE = 11,
        CREATE_NEW_CHARACTER_RESULT = 12,
        DELETE_CHARACTER_RESULT = 13,

        CHANGE_CHANNEL = 14,
        PING = 15,
        AUTHEN_CODE_CHANGED = 16,
        SECURITY_SOMETHING = 17, // Either read a buffer (passed to GG?) or a set of ints. Looks like CRC info.
        ___END_SOCKET = 18,

        ___START_CHARACTERDATA = 19,
        INVENTORY_OPERATION = 20,
        INVENTORY_GROW = 21,
        STAT_CHANGED = 22,
        FORCED_STAT_SET = 23,
        FORCED_STAT_RESET = 24,
        CHANGE_SKILL_RECORD_RESULT = 25,
        SKILL_USE_RESULT = 26,
        GIVE_POPULARITY_RESULT = 27,
        SHOW_STATUS_INFO = 28, // Called 'Message'
        MEMO_RESULT = 29,
        MAP_TRANSFER_RESULT = 30,
        SUE_CHARACTER_RESULT = 31,
        // 32 does not exist, possibly ClaimServer stuff
        // 33 does not exist
        CHARACTER_INFO = 34,
        PARTY_RESULT = 35,
        FRIEND_RESULT = 36,
        TOWN_PORTAL = 37,
        BROADCAST_MSG = 38,
        ___END_CHARACTEDATA = 39,

        ___START_STAGE = 40,
        SET_FIELD = 41,
        SET_CASH_SHOP = 42,
        ___END_STAGE = 43,

        ___START_FIELD = 44,
        TRANSFER_FIELD_REQ_IGNORED = 45,
        TRANSFER_CHANNEL_REQ_IGNORED = 46,
        FIELD_SPECIFIC_DATA = 47,
        GROUP_MESSAGE = 48,
        WHISPER = 49,
        SUMMON_ITEM_INAVAILABLE = 50,
        FIELD_EFFECT = 51,
        BLOW_WEATHER = 52,
        PLAY_JUKE_BOX = 53,
        ADMIN_RESULT = 54,
        QUIZ = 55,
        DESC = 56,
        CLOCK = 57,
        
        CONTIMOVE = 58,
        CONTISTATE = 59,

        WARN_MESSAGE = 61,
        
        ___START_USERPOOL = 62,
        USER_ENTER_FIELD = 63,
        USER_LEAVE_FIELD = 64,
        
        ___START_USERCOMMON = 65,
        CHAT = 66,
        MINI_ROOM_BALLOON = 67,
        SET_CONSUME_ITEM_EFFECT = 68, // int itemid; See Effect\ItemEff.img\(itemid)

        ___START_PET = 69,
        SPAWN_PET = 70,
        PET_MOVE = 71,
        PET_ACTION = 72,
        PET_NAME_CHANGED = 73,
        PET_INTERACTION = 74,
        ___END_PET = 75,
        
        ___START_SPAWN = 76,
        SPAWN_ENTER_FIELD = 77,
        SPAWN_LEAVE_FIELD = 78,
        SPAWN_MOVE = 79,
        SPAWN_ATTACK = 80,
        SPAWN_HIT = 81,
        ___END_SPAWN = 82,

        ___START_USERREMOTE = 84,
        MOVE_PLAYER = 85,
        CLOSE_RANGE_ATTACK = 86,
        RANGED_ATTACK = 87,
        MAGIC_ATTACK = 88,
        PREPARE_SKILL = 89, // Skills related: 1111008 (Shout), 1311006 (Dragon Roar), 5001006 (Super Dragon Roar)
        SKILL_END = 90,
        DAMAGE_PLAYER = 91, // Called 'Hit'
        FACIAL_EXPRESSION = 92,
        AVATAR_MODIFIED = 93, //Called UPDATE_CHAR_LOOK in odin, new name = gms-like
        SHOW_FOREIGN_EFFECT = 94, // Called 'Effect'
        GIVE_FOREIGN_BUFF = 95,
        RESET_FOREIGN_BUFF = 96,
        UPDATE_PARTYMEMBER_HP = 97,
        ___END_USERREMOTE = 98,

        ___START_USERLOCAL = 99,
        SHOW_CHAIR = 100,
        PLAYER_EFFECT = 101, // CUser::OnEffect, 
        // 102 reads a byte?
        // 103 is missing
        MESOBAG_SUCCEED = 104,
        MESOBAG_FAILED = 105,
        ___END_USERLOCAL = 106,

        ___START_MOBPOOL = 108,
        MOB_ENTER_FIELD = 109,
        MOB_LEAVE_FIELD = 110,
        MOB_CHANGE_CONTROLLER = 111,

        ___START_MOB = 112,
        MOB_MOVE = 113,
        MOB_MOVE_RESPONSE = 114,
        // 115 doesnt exist
        MOB_STAT_SET = 116,
        MOB_STAT_RESET = 117,
        MOB_SUSPEND_RESET = 118,
        MOB_AFFECTED = 119,
        MOB_DAMAGED = 120,
        MOB_EFFECT_BY_SKILL = 121, // int mapmobid, int skillid. Seems to activate a special animation caused by certain skills (e.g. 3210001 mortal blow), mainly those with the 'special' node
        // 122 doesnt exist
        ___END_MOB = 123,

        ___END_MOBPOOL = 124,

        ___START_NPCPOOL = 125,
        NPC_ENTER_FIELD = 126,
        NPC_LEAVE_FIELD = 127,
        NPC_CHANGE_CONTROLLER = 128,
        NPC_SET_SPECIAL_ACTION = 129,
        NPC_ANIMATE = 130,
        ___END_NPCPOOL = 131,
        // 132 ???
        ___START_DROPPOOL = 133,
        DROP_ENTER_FIELD = 134,
        DROP_LEAVE_FIELD = 135,
        ___END_DROPPOOL = 136,

        ___START_MESSAGEBOXPOOL = 137,
        MESSAGE_BOX_CREATE_FAILED = 138,
        MESSAGE_BOX_ENTER_FIELD = 139,
        MESSAGE_BOX_LEAVE_FIELD = 140,
        ___END_MESSAGEBOXPOOL = 141,

        ___START_AFFECTED_AREA = 142,
        AFFECTED_AREA_CREATED = 143,
        AFFECTED_AREA_REMOVED = 144,
        ___END_AFFECTED_AREA = 145,

        ___START_TOWN_PORTAL = 146,
        TOWN_PORTAL_CREATED = 147,
        TOWN_PORTAL_REMOVED = 148,
        ___END_TOWN_PORTAL = 149,

        ___START_REACTORPOOL = 150,
        REACTOR_CHANGE_STATE = 151,
        // 152 is missing
        REACTOR_ENTER_FIELD = 153,
        REACTOR_LEAVE_FIELD = 154,
        ___END_REACTORPOOL = 155,

        ___START_ETCFIELDOBJ = 156,
        SNOWBALL_STATE = 157,
        SNOWBALL_HIT = 158,
        
        COCONUT_HIT = 159, // was 156 in v40b, assumed to be 159 in v12
        COCONUT_SCORE = 160, // was 157 in v40b, assumed to be 160 in v12

        // MC stuff comes here

        // Zakum timer here
        ___END_ETCFIELDOBJ = 161,

        ___START_SCRIPT = 162,
        SCRIPT_MESSAGE = 163, // IE used for SendSnowballRules
        ___END_SCRIPT = 164,

        ___START_SHOP = 165,
        SHOP = 166,
        SHOP_TRANSACTION = 167,
        ___END_SHOP = 168,

        ___START_STORAGE = 169,
        STORAGE = 170,
        STORAGE_RESULT = 171,
        ___END_STORAGE = 172,

        ___START_MESSENGER = 173,
        MESSENGER = 174,
        ___END_MESSENGER = 175,

        ___START_MINIROOM = 176,
        MINI_ROOM_BASE = 177,
        ___END_MINIROOM = 178,

        ___START_TOURNAMENT = 179,
        TOURNAMENT_INFO = 180,
        TOURNAMENT_MATCH_TABLE = 181,
        TOURNAMENT_SET_PRIZE = 182,
        TOURNAMENT_NOTICE_UEW = 183,
        TOURNAMENT_AVATAR_INFO = 184,
        ___END_TOURNAMENT = 185,

        // ???

        ___START_CASHSHOP = 187,
        CASHSHOP_RECHARGE = 188,
        CASHSHOP_UPDATE_AMOUNTS = 189,
        CASHSHOP_ACTION = 190,
        ___END_CASHSHOP = 191,

        /*
        CLIENT_CONNECT_TO_SERVER_LOGIN = 0x05,
		LOGIN_CHARACTER_REMOVE_RESULT = 0x08,
		CLIENT_CONNECT_TO_SERVER = 0x09,

		INVENTORY_CHANGE_SLOT = 0x12,
		INVENTORY_CHANGE_INVENTORY_SLOTS = 0x13,
		
		STATS_CHANGE = 0x14,

		SKILLS_GIVE_BUFF = 0x15,
		SKILLS_GIVE_DEBUFF = 0x16,
		SKILLS_ADD_POINT = 0x17,

		Fame = 0x19,

		Notice = 0x1A,
		TeleportRock = 0x1C,

		PlayerInformation = 0x1F,
		Message = 0x23,
		EnterMap = 0x26,

		IncorrectChannelNumber = 0x2B, //0x2B
        CashshopUnavailable = 0x64,
		SlashCmdAnswer = 0x2E,
        Party_Operation = 0x20,
        Buddy_Operation = 0x21,

		RemotePlayerSpawn = 0x3C,
		RemotePlayerDespawn = 0x3D,
		RemotePlayerChat = 0x3F,

		SummonDespawn = 0x4B,
		SummonMove = 0x4B,
		SummonAttack = 0x4D,
		SummonDamage = 0x4E,

		RemotePlayerMove = 0x52,
		RemotePlayerMeleeAttack = 0x53,
		RemotePlayerRangedAttack = 0x54,
		RemotePlayerMagicAttack = 0x55,
		RemotePlayerGetDamage = 0x58,
		RemotePlayerEmote = 0x59,
		RemotePlayerChangeEquips = 0x5A,
		RemotePlayerAnimation = 0x5B,
		RemotePlayerSkillBuff = 0x5C,
		RemotePlayerSkillDebuff = 0x5D,

		RemotePlayerSitOnChair = 0x61,
		RemotePlayerThirdPartyAnimation = 0x62,

		MesoSackResult = 0x65,

		MobSpawn = 0x6A,
		MobRespawn = 0x6B,
		MobControlRequest = 0x6C,
		MobMovement = 0x6E,
		MobControlResponse = 0x6F,
		MobChangeHealth = 0x75,

		NpcSpawn = 0x7B,
		NpcControlRequest = 0x7D,

		NpcAnimate = 0x7F,

		DropSpawn = 0x83,
		DropModify = 0x84,
        Reactor_Hit = 0x94,
        Reactor_Spawn = 0x96,
        Reactor_Destroy = 0x97,

		SnowBall_State = 0x9A,
		SnowBall_Hit = 0x9B,

		Coconut_Hit = 0x9C,
		Coconut_Score = 0x9D,

		NpcScriptChat = 0xA0,

		NpcShopShow = 0xA3,
		NpcShopResult = 0xA4,

		StorageShow = 0xA7,
		StorageResult = 0xA8
        */
    }
}