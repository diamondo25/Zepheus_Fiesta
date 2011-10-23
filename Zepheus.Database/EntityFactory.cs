using Zepheus.Util;

namespace Zepheus.Database
{
    public static class EntityFactory
    {
        public static AccountEntity GetAccountEntity(EntitySetting setting)
        {
            string connectionstring = ConnectionStringbuilder.CreateEntityString(setting);
            return new AccountEntity(connectionstring);
        }

        public static WorldEntity GetWorldEntity(EntitySetting setting)
        {
            string connectionstring = ConnectionStringbuilder.CreateEntityString(setting);
            return new WorldEntity(connectionstring);
        }

        public static WorldEntity GetWorldEntity(string connection)
        {
            return new WorldEntity(connection);
        }
    }
}
