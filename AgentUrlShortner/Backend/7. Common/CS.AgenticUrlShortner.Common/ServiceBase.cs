using CS.AgenticUrlShortner.Common.Contracts;

namespace CS.AgenticUrlShortner.Common
{
    public abstract class ServiceBase : IService
    {
        public AmbientContext Context { get; set; }

        public FactoryBase Factory { get; set; }

        public virtual string TestMe(string input)
        {
            return $"{input} : {GetType().Name}";
        }
    }
}
