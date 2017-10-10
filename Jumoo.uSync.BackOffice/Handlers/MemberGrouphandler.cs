using System.Collections.Generic;
using Jumoo.uSync.BackOffice.Helpers;
using Jumoo.uSync.Core;
using Jumoo.uSync.Core.Serializers;
using Umbraco.Core;
using Umbraco.Core.Events;
using Umbraco.Core.Logging;
using Umbraco.Core.Models;
using Umbraco.Core.Services;

namespace Jumoo.uSync.BackOffice.Handlers
{
    public class MemberGroupHandler : uSyncExtendedHandler<IMemberGroup>, ISyncHandler
    {
        private readonly IMemberGroupService _memberGroupService;

        public MemberGroupHandler() : base(new MemberGroupSerializer())
        {
            _memberGroupService = ApplicationContext.Current.Services.MemberGroupService;
        }

        public int Priority => uSyncConstants.Priority.MemberGroup;
        public string Name => "uSync: MemberGroupHandler";
        public override string SyncFolder => "MemberGroup";

        public IEnumerable<uSyncAction> ExportAll(string folder)
        {
            LogHelper.Info<MemberGroupHandler>("Exporting MemberGroups");

            var actions = new List<uSyncAction>();

            foreach (var memberGroup in _memberGroupService.GetAll())
            {
                actions.Add(Export(memberGroup, GetItemPath(memberGroup), folder));
            }

            return actions;
        }

        public void RegisterEvents()
        {
            MemberGroupService.Saved += MemberGroupSaved;
            MemberGroupService.Saving += MemberGroupSaving;
            MemberGroupService.Deleted += MemberGroupDeleted;
        }

        private void MemberGroupSaving(IMemberGroupService sender, SaveEventArgs<IMemberGroup> e)
        {
            if (uSyncEvents.Paused)
                return;

            foreach (var eSavedEntity in e.SavedEntities)
            {
                if (!eSavedEntity.HasIdentity) continue;
                var persistedMemberGroup = _memberGroupService.GetById(eSavedEntity.Id);
                if (persistedMemberGroup.Name == eSavedEntity.Name) continue;
                DeleteFile(persistedMemberGroup, false);
            }
        }

        private void MemberGroupSaved(IMemberGroupService sender, SaveEventArgs<IMemberGroup> e)
        {
            if (uSyncEvents.Paused)
                return;

            SaveItems(sender, e.SavedEntities);
        }

        private void MemberGroupDeleted(IMemberGroupService sender, DeleteEventArgs<IMemberGroup> e)
        {
            if (uSyncEvents.Paused)
                return;

            foreach (var deletedEntity in e.DeletedEntities)
            {
                DeleteFile(deletedEntity);
            }
        }

        private void SaveItems(IMemberGroupService sender, IEnumerable<IMemberGroup> savedMemberGroups)
        {
            foreach (var memberGroup in savedMemberGroups)
            {
                LogHelper.Info<MemberGroupHandler>("Save: Saving uSync files for : {0}", () => memberGroup.Name);
                var attempt = Export(memberGroup, GetItemPath(memberGroup),
                    uSyncBackOfficeContext.Instance.Configuration.Settings.Folder);
                if (attempt.Success)
                {
                    NameChecker.ManageOrphanFiles(SyncFolder, memberGroup.Key, attempt.FileName);
                }
            }
        }

        public override string GetItemPath(IMemberGroup memberGroup)
        {
            var path = memberGroup.Name.ToSafeFileName();
            return path;
        }

        public override string GetItemName(IMemberGroup item)
        {
            return item.Name;
        }
    }
}