using System.Reflection;
using System.Runtime.Loader;

namespace NoshNovel.Factories.AssemblyLoadContexts
{
    class CollectibleAssemblyLoadContext : AssemblyLoadContext
    {
        public CollectibleAssemblyLoadContext() : base(isCollectible: true)
        {

        }

        protected override Assembly? Load(AssemblyName name)
        {
            return null;
        }
    }
}
