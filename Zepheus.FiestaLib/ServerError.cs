
namespace Zepheus.FiestaLib
{
    public enum ServerError : ushort
    {
        INVALID_CREDENTIALS = 68,
        DATABASE_ERROR = 67,
        EXCEPTION = 65,
        BLOCKED = 71,
        SERVER_MAINTENANCE = 72,
        TIMEOUT = 73,
        AGREEMENT_MISSING = 75,
        WRONG_REGION = 81,
    }
}
