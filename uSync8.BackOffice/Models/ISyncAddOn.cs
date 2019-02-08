using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace uSync8.BackOffice.Models
{
    public interface ISyncAddOn
    {
        string Name { get; }
        string Version { get; }
    }
}
