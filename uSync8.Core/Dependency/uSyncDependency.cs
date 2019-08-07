using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Umbraco.Core;

namespace uSync8.Core.Dependency
{
    public class uSyncDependency
    {
        /// <summary>
        ///  name to display to user (not critical for deployment of a dependency)
        /// </summary>
        public string Name { get; set; }

        public Udi Udi { get; set; }
        public int Order { get; set; }

        public int Level { get; set; }

        public DependencyMode Mode { get; set; }

        public DependencyFlags Flags { get; set; }
    }

    public enum DependencyMode
    {
        MustMatch,
        MustExist
    }
}
