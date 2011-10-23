
namespace Zepheus.FiestaLib
{
    public enum CreateCharError : ushort
    {
        FailedToCreate = 130,       // Failed to create the char
        WrongClass = 131,           // You have chosen wrong class for your avatar
        NameTaken = 132,            // The name is already taken by another user
        ErrorInMaxSlot = 133,       // There is and error in the Max slot Number
        NameInUse = 385,            // The name is already in use
    }
}
