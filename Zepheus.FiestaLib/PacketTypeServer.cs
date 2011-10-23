
namespace Zepheus.FiestaLib
{
    // Named as SHXType , where X = header ID

    public enum SH2Type : byte
    {
        Ping = 4,
        SetXorKeyPosition = 7,
        Chatblock = 72,
    }

    public enum SH3Type : byte
    {
        IncorrectVersion = 2, //please update client
        VersionAllowed = 3,
        FilecheckAllow = 5,
        Error = 9,
        WorldlistNew = 10,
        WorldServerIP = 12,
        WorldistResend = 28,

        //Actually used in world
        CharacterList = 20,
    }

    public enum SH4Type : byte
    {
        ConnectError = 2,
        ServerIP = 3,
        CharacterInfo = 56,
        CharacterLook = 57,
        CharacterQuestsBusy = 58,
        CharacterQuestsDone = 59,
        CharacterActiveSkillList = 61,
        CharacterPassiveSkillList = 62,
        CharacterItemList = 71,
        CharacterInfoEnd = 72,
        CharacterTitles = 73,
        CharacterTimedItemList = 74,

        ReviveWindow = 77,
        Revive = 79,

        CharacterPoints = 91,
        SetPointOnStat = 95,
    }

    public enum SH5Type : byte
    {
        CharCreationError = 4,
        CharCreationOK = 6,
        CharDeleteOK = 12,
    }

    public enum SH6Type : byte
    {
        DetailedCharacterInfo = 2,
        Error = 3,
        RemoveDrop = 5,
        ChangeMap = 9,
        ChangeZone = 10,
    }

    public enum SH7Type : byte
    {
        ShowUnequip = 4,
        ShowEquip = 5,
        SpawnSinglePlayer = 6,
        SpawnMultiPlayer = 7,
        SpawnSingleObject = 8,
        SpawnMultiObject = 9,
        ShowDrop = 10,
        ShowDrops = 11,
        RemoveObject = 14,
    }

    public enum SH8Type : byte
    {
        ChatNormal = 2,
        GmNotice = 17,
        StopTele = 19, // Stops char but can teleport
        Walk = 24,
        Move = 26,
        Teleport = 27,
        Interaction = 28,
        Shout = 31,
        Emote = 33,
        Jump = 37,
        BeginRest = 40,
        BeginDisplayRest = 41,
        EndRest = 43,
        EndDisplayRest = 44,
        BlockWalk = 74,
    }

    public enum SH9Type : byte
    {
        StatUpdate = 2,
        GainEXP = 11,
        LevelUP = 12,
        LevelUPAnimation = 13,
        HealHP = 14,
        HealSP = 15,
        SkillAck = 53,
        ResetStance = 61,
        AttackAnimation = 71,
        AttackDamage = 72,
        DieAnimation = 74,

        SkillUsePrepareSelf = 78,
        SkillUsePrepareOthers = 79, 

        SkillAnimationPosition = 81,
        SkillAnimationTarget = 82,
        SkillAnimation = 87,
    }

    public enum SH12Type : byte
    {
        ModifyItemSlot = 1,
        ModifyEquipSlot = 2,
        InventoryFull = 4,
        ObtainedItem = 10,
        FailedEquip = 17,
        FailedUnequip = 19,
        ItemUseEffect = 22,
        ItemUpgrade = 24,
        ItemUsedOk = 26,
    }

    public enum SH15Type : byte
    {
        Question = 1,
    }

    //skills & crap?
    public enum SH18Type : byte
    {
        LearnSkill = 4,
    }

    public enum SH20Type : byte
    {
        ChangeHPStones = 3,
        ChangeSPStones = 4,
        ErrorBuyStone = 5,
        ErrorUseStone = 6,
        StartHPStoneCooldown = 8,
        StartSPStoneCooldown = 10,
    }

    public enum SH25Type : byte
    {
        WorldMessage = 2,
    }

    public enum SH28Type : byte
    {
        LoadQuickBar = 3,
        LoadQuickBarState = 5,
        LoadGameSettings = 11,
        LoadClientSettings = 13,
        LoadShortCuts = 15,
    }

    public enum SH29Type : byte
    {
        GuildNameResult = 119,
    }

    public enum SH31Type : byte
    {
        LoadUnkown = 7,
    }
}
