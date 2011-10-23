
namespace Zepheus.FiestaLib
{
    public enum CH2Type : byte
    {
        Pong = 5,
    }

    public enum CH3Type : byte
    {
        Version = 1,
        Login = 32,
        WorldReRequest = 27,
        FileHash = 4,
        WorldSelect = 11,

        //Actually used in World
        WorldClientKey = 15,
        BackToCharSelect = 24,
    }

    public enum CH4Type : byte
    {
        CharSelect = 1,

        ReviveToTown = 78,
        SetPointOnStat = 92,
    }

    public enum CH5Type : byte
    {
        CreateCharacter = 1,
        DeleteCharacter = 7,
    }

    public enum CH6Type : byte
    {
        TransferKey = 1,
        ClientReady = 3,
    }

    public enum CH7Type : byte
    {
        UnknownSomethingWithMobs = 1,
    }

    public enum CH8Type : byte
    {
        ChatNormal = 1,
        BeginInteraction = 10,
        Stop = 18,
        Walk = 23,
        Run = 25,
        Shout = 30,
        Emote = 32,
        Jump = 36,
        BeginRest = 39,
        EndRest = 42,
    }

    public enum CH9Type : byte
    {
        SelectObject = 1,
        DeselectObject = 8,

        AttackEntityMelee = 43,

        StopAttackingMelee = 50,

        AttackEntitySkill = 61,
        UseSkillWithTarget = 64,
        UseSkillWithPosition = 65,
    }

    //items
    public enum CH12Type : byte
    {
        DropItem = 7,
        LootItem = 9,
        MoveItem = 11,
        Equip = 15,
        Unequip = 18,
        UseItem = 21,
        ItemEnhance = 23,
    }

    public enum CH15Type : byte
    {
        AnswerQuestion = 2,
    }

    public enum CH20Type : byte
    {
        UseHPStone = 7,
        UseSPStone = 9,
    }

    public enum CH28Type : byte
    {
        GetQuickBar = 2,
        GetQuickBarState = 4,
        GetGameSettings = 10,
        GetClientSettings = 12,
        GetShortCuts = 14,

        SaveQuickBar = 16,
        SaveQuickBarState = 17,
        SaveGameSettings = 20,
        SaveClientSettings = 21,
        SaveShortCuts = 22,
    }

    public enum CH29Type : byte
    {
        GuildNameRequest = 118,
    }

    public enum CH31Type : byte
    {
        GetUnknown = 6,
    }
}
