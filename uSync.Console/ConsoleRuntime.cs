using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using Umbraco.Core;
using Umbraco.Core.Composing;
using Umbraco.Core.Runtime;
using Umbraco.Web;

namespace uSync.ConsoleApp
{
    /// <summary>
    ///  wrapper for the Umbraco Core runtime, via a console
    /// </summary>
    /// <remarks>
    /// </remarks>
    public class ConsoleRuntime : CoreRuntime
    {
        private readonly TextReader reader;
        private readonly TextWriter writer;

        public ConsoleRuntime(TextReader reader, TextWriter writer)
            : base()
        {
            this.reader = reader;
            this.writer = writer;
        }

        public override IFactory Boot(IRegister register)
        {
            register.Register(this.reader);
            register.Register(this.writer);
            register.Register<IHttpContextAccessor, NullHttpContextAccessor>();
            return base.Boot(register);
        }
    }

    /// <summary>
    ///  provide a HttpContext accesstor, that just returns null.
    /// </summary>
    public class NullHttpContextAccessor : IHttpContextAccessor
    {
        public HttpContext HttpContext
        {
            get { return HttpContext = null; }
            set { throw new NotImplementedException(); }
        }
    }

}
