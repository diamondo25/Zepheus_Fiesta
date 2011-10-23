using System;

namespace Zepheus.Util
{
    public enum InitializationStage
    {
        Metadata = 0,
        Settings = 1,
        DataStore = 2,
        Services = 3,
        SpecialDataProvider = 4,
        Worker = 5,
        Clients = 6,
        Networking = 7
    }

    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
    public class ServerModuleAttribute : Attribute
    {
        private readonly InitializationStage stageInternal;
        public InitializationStage InitializationStage { get { return this.stageInternal; } }

        public ServerModuleAttribute(InitializationStage initializationStage)
        {
            this.stageInternal = initializationStage;
        }
    }

    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
    public class InitializerMethodAttribute : Attribute {} 
}