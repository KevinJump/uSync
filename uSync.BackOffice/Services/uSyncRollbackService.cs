using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

using Umbraco.Cms.Core.Hosting;

using uSync.BackOffice.Configuration;
using uSync.BackOffice.SyncHandlers;

namespace uSync.BackOffice.Services
{
    /// <summary>
    ///  service for putting files into a folder - so we can rollback from a change. 
    /// </summary>
    public class uSyncRollbackService
    {
        private readonly string _rollbackFolder;

        public uSyncRollbackService(IHostingEnvironment hostingEnvironment)
        {
            _rollbackFolder = Path.GetFullPath(Path.Combine(hostingEnvironment.LocalTempPath, "uSync", "Rollback"));
        }

        private string GetRollbackFolder(Guid id)
            => Path.Combine(_rollbackFolder, id.ToString());

        /// <summary>
        ///  create a rollback file. 
        /// </summary>
        /// <param name="rollbackId"></param>
        /// <param name="handlerConfigPair"></param>
        /// <param name="node"></param>
        public void Rollback(Guid rollbackId, HandlerConfigPair handlerConfigPair, XElement node)
        {
            var udi = handlerConfigPair.Handler.FindFromNode(node);
            if (udi != null)
            {
                var folder = Path.Combine(GetRollbackFolder(rollbackId), handlerConfigPair.Handler.DefaultFolder);
                handlerConfigPair.Handler.Export(udi, folder, handlerConfigPair.Settings);
            }
        }

        /// <summary>
        ///  stamps the rollback with the date and time, so you know when it was made. (TODO: add advanced info?)
        /// </summary>
        /// <param name="rollbackId"></param>
        public void StampRollback(Guid rollbackId)
        {
            var rollbackInfo = new RollbackInfo
            {
                RollbackTime = DateTime.Now
            };

            var rollbackfile = Path.Combine(GetRollbackFolder(rollbackId), "_info.json");
            File.WriteAllText(rollbackfile, JsonConvert.SerializeObject(rollbackInfo));
        }

        [JsonObject(NamingStrategyType = typeof(CamelCaseNamingStrategy))]
        public class RollbackInfo
        {
            public DateTime RollbackTime { get; set; }
        }
    }
}
