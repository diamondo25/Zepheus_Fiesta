
namespace Zepheus.FiestaLib
{
    public enum WorldStatus : byte
    {
        // With messages:
        OFFLINE = 0,        // Server is closed. Please try login later.
        MAINTENANCE = 1,    // Server is under maintenance. Please try login later.
        EMPTYSERVER = 2,    // You cannot connect to an empty server.
        RESERVED = 3,       // The server has been reserved for a special use.
        OFFLINEUNKERROR = 4, // Login failed due to an unknown error.
        FULL = 5,           // Server is full. Please try again later.
        LOW = 6,
        // LOW = 7,
        // LOW = 8,
        MEDIUM = 9,
        HIGH = 10,
    }
}
