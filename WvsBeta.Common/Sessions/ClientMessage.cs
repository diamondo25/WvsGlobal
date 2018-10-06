namespace WvsBeta.Common.Sessions
{
    public enum ClientMessages : byte
    {
        __PADDING_0 = 0,
        // Login Headers
        LOGIN_CHECK_PASSWORD = 1,
        LOGIN_SELECT_CHANNEL = 2,
        LOGIN_WORLD_SELECT = 3,
        LOGIN_EULA = 4,
        LOGIN_SET_GENDER = 5,
        LOGIN_CHECK_PIN = 6,
        LOGIN_UPDATE_PIN = 7,
        LOGIN_WORLD_INFO_REQUEST = 8,
        LOGIN_SELECT_CHARACTER = 9,
        MIGRATE_IN = 10,
        LOGIN_CHECK_CHARACTER_NAME = 11,
        LOGIN_CREATE_CHARACTER = 12,
        LOGIN_DELETE_CHARACTER = 13,

        // Client Communication Headers
        PONG = 14,
        CLIENT_CRASH_REPORT = 15,
        CLIENT_HASH = 16,
        // Game Headers
        
        __PADDING_17 = 17,
        __PADDING_18 = 18,
        ENTER_PORTAL = 19,
        CHANGE_CHANNEL = 20,
        ENTER_CASH_SHOP = 21,
        MOVE_PLAYER = 22,
        SIT_REQUEST = 23,

        //Damage Headers
        CLOSE_RANGE_ATTACK = 24,
        RANGED_ATTACK = 25,
        MAGIC_ATTACK = 26,
        __PADDING_27,
        TAKE_DAMAGE = 28,

        CHAT = 29,
        EMOTE = 30,

        __PADDING_31,
        __PADDING_32,

        //NPC Interaction Headers
        NPC_TALK = 33,
        NPC_TALK_MORE = 34,
        SHOP_ACTION = 35,
        STORAGE_ACTION = 36,

        //Inventory Headers
        ITEM_MOVE = 37,
        ITEM_USE = 38,
        SUMMON_BAG_USE = 39,
        PET_EAT_FOOD = 40,
        CASH_ITEM_USE = 41, // Assumed Value
        RETURN_SCROLL_USE = 42, // Assumed Value
        SCROLL_USE = 43, // Assumed Value
        
        //Player Stat Headers
        DISTRIBUTE_AP = 44,
        HEAL_OVER_TIME = 45,
        DISTRIBUTE_SP = 46,
        GIVE_BUFF = 47,
        CANCEL_BUFF = 48, // Assumed Value
        PREPARE_SKILL = 49,

        DROP_MESOS = 50,
        GIVE_FAME = 51,
        __PADDING_52,
        CHAR_INFO_REQUEST = 53,
        SPAWN_PET = 54, // Assumed Value
        CHARACTER_IS_DEBUFFED = 55, // spammed.
        ENTER_SCRIPTED_PORTAL = 56,
        MAP_TRANSFER_REQUEST = 57, // Teleport rock stuff

        REPORT_USER = 58,
        __PADDING_59,
        __PADDING_60,
        GROUP_MESSAGE = 61,
        WHISPER = 62,
        MESSENGER = 63,
        MINI_ROOM_OPERATION = 64,
        PARTY_OPERATION = 65,
        DENY_PARTY_REQUEST = 66,
        // -- nonexistant headers, such as admin commands and guild! --
        __PADDING_67,
        __PADDING_68,
        FRIEND_OPERATION = 69,
        NOTE_OPERATION = 70,
        ENTER_TOWN_PORTAL = 71,
        __PADDING_72,
        __PADDING_73,
        PET_MOVE = 74,
        PET_ACTION = 75,
        PET_INTERACTION = 76,
        __PADDING_77,
        __PADDING_78,
        __PADDING_79,
        SUMMON_MOVE = 80,
        SUMMON_ATTACK = 81,
        SUMMON_DAMAGED = 82,
        __PADDING_83,
        __PADDING_84,
        __PADDING_85,
        __PADDING_86,
        __PADDING_87,
        MOB_MOVE = 88,
        MOB_APPLY_CONTROL = 89, // Only when PickUpDrop or FirstAttack
        MOB_PICKUP_DROP = 90,
        __PADDING_91,
        __PADDING_92,
        NPC_ANIMATE = 93,
        __PADDING_94,
        __PADDING_95,
        __PADDING_96,
        DROP_PICK_UP = 97,
        __PADDING_98,
        __PADDING_99,
        REACTOR_HIT = 100,
        __PADDING_101,
        __PADDING_102,
        __PADDING_103,
        __PADDING_104,
        FIELD_SNOWBALL_ATTACK = 105,
        FIELD_COCONUT_ATTACK = 106,
        __PADDING_107,
        __PADDING_108,
        FIELD_TOURNAMENT_MATCHTABLE = 109, // '/matchtable 1' sends this
        __PADDING_110,
        __PADDING_111,
        __PADDING_112,
        __PADDING_113,
        FIELD_CONTIMOVE_STATE = 114, // CONTISTATE?
        __PADDING_115,
        __PADDING_116,
        __PADDING_117,
        CASHSHOP_QUERY_CASH = 118,
        CASHSHOP_ACTION = 119,
        CASHSHOP_ENTER_COUPON = 120,
        
        // THERE SHOULD BE 120 (+ 1) FIELDS

        __CUSTOM_DC_ME__ = 0xC0,

        JUNK = 250
    }
}