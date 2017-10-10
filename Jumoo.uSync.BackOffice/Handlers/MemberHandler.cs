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
    public class MemberHandler : uSyncExtendedHandler<IMember>, ISyncHandler
    {
        public int Priority => uSyncConstants.Priority.Member;
        public string Name => "uSync: MemberHandler";
        public override string SyncFolder => "Member";

        private readonly IMemberService _memberService;

        public MemberHandler() : 
            base(new MemberSerializer())
        {
            _memberService = ApplicationContext.Current.Services.MemberService;
        }

        public IEnumerable<uSyncAction> ExportAll(string folder)
        {
            LogHelper.Info<MemberHandler>("Exporting Members");

            var actions = new List<uSyncAction>();

            foreach (var member in _memberService.GetAllMembers())
            {
                actions.Add(Export(member, GetItemPath(member), folder));
            }

            return actions;
        }

        public void RegisterEvents()
        {
            MemberService.Saved += MemberSaved;
            MemberService.Saving += MemberSaving;
            MemberService.Deleted += MemberDeleted;
        }

        private void MemberSaving(IMemberService sender, SaveEventArgs<IMember> e)
        {
            if (uSyncEvents.Paused)
                return;

            foreach (var eSavedEntity in e.SavedEntities)
            {
                if (!eSavedEntity.HasIdentity) continue;
                var persistedMember = _memberService.GetById(eSavedEntity.Id);
                if (persistedMember.Name == eSavedEntity.Name) continue;
                DeleteFile(persistedMember, false);
            }
        }

        private void MemberSaved(IMemberService sender, SaveEventArgs<IMember> e)
        {
            if (uSyncEvents.Paused)
                return;

            SaveItems(sender, e.SavedEntities);   
        }

        private void MemberDeleted(IMemberService sender, DeleteEventArgs<IMember> e)
        {
            if (uSyncEvents.Paused)
                return;

            foreach (var deletedEntity in e.DeletedEntities)
            {
                DeleteFile(deletedEntity);
            }
        }

        private void SaveItems(IMemberService sender, IEnumerable<IMember> savedMembers)
        {
            foreach (var savedMember in savedMembers)
            {
                LogHelper.Info<MemberHandler>("Save: Saving uSync files for : {0}", () => savedMember.Name);
                var member = _memberService.GetById(savedMember.Id);
                var attempt = Export(member, GetItemPath(member), uSyncBackOfficeContext.Instance.Configuration.Settings.Folder);
                if (attempt.Success)
                {
                    NameChecker.ManageOrphanFiles(SyncFolder, member.Key, attempt.FileName);
                }
            }
        }

        public override string GetItemPath(IMember member)
        {
            var path = member.Name.ToSafeFileName();
            if (member.ParentId != -1)
            {
                path = $"{GetItemPath(_memberService.GetById(member.ParentId))}\\{path}";
            }

            return path;
        }

        public override string GetItemName(IMember item)
        {
            return item.Name;
        }
    }
}