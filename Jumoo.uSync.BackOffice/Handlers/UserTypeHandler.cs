using System.Collections.Generic;
using Jumoo.uSync.BackOffice.Helpers;
using Jumoo.uSync.Core;
using Jumoo.uSync.Core.Serializers;
using Umbraco.Core;
using Umbraco.Core.Events;
using Umbraco.Core.Logging;
using Umbraco.Core.Models.Membership;
using Umbraco.Core.Services;

namespace Jumoo.uSync.BackOffice.Handlers
{
    public class UserTypeHandler : uSyncExtendedHandler<IUserType>, ISyncHandler
    {
        public int Priority => uSyncConstants.Priority.UserType;
        public string Name => "uSync: UserTypeHandler";
        public override string SyncFolder => "UserType";

        private readonly IUserService _userService;

        public UserTypeHandler() : base(new UserTypeSerializer())
        {
            _userService = ApplicationContext.Current.Services.UserService;
        }

        public IEnumerable<uSyncAction> ExportAll(string folder)
        {
            LogHelper.Info<UserTypeHandler>("Exporting UserTypes");

            var actions = new List<uSyncAction>();
            foreach (var user in _userService.GetAllUserTypes())
            {
                actions.Add(Export(user, GetItemPath(user), folder));
            }

            return actions;
        }

        public void RegisterEvents()
        {
            UserService.SavedUserType += UserTypeSaved;
            UserService.SavingUserType += UserTypeSaving;
            UserService.DeletedUserType += UserTypeDeleted;
        }

        private void UserTypeSaving(IUserService sender, SaveEventArgs<IUserType> e)
        {
            if (uSyncEvents.Paused)
                return;

            foreach (var eSavedEntity in e.SavedEntities)
            {
                if (!eSavedEntity.HasIdentity) continue;
                var persistedUserType = _userService.GetUserTypeById(eSavedEntity.Id);
                if (persistedUserType.Name == eSavedEntity.Name) continue;
                DeleteFile(persistedUserType, false);
            }
        }


        private void UserTypeSaved(IUserService sender, SaveEventArgs<IUserType> e)
        {
            if (uSyncEvents.Paused)
                return;

            SaveItems(sender, e.SavedEntities);   
        }

        private void UserTypeDeleted(IUserService sender, DeleteEventArgs<IUserType> e)
        {
            if (uSyncEvents.Paused)
                return;

            foreach (var deletedEntity in e.DeletedEntities)
            {
                DeleteFile(deletedEntity);
            }
        }

        private void SaveItems(IUserService sender, IEnumerable<IUserType> savedUserTypes)
        {
            foreach (var savedUserType in savedUserTypes)
            {
                LogHelper.Info<UserTypeHandler>("Save: Saving uSync files for : {0}", () => savedUserType.Name);
                var userType = _userService.GetUserTypeById(savedUserType.Id);
                var attempt = Export(userType, GetItemPath(userType), uSyncBackOfficeContext.Instance.Configuration.Settings.Folder);
                if (attempt.Success)
                {
                    NameChecker.ManageOrphanFiles(SyncFolder, userType.Key, attempt.FileName);
                }
            }
        }

        public override string GetItemPath(IUserType userType)
        {
            var path = userType.Name.ToSafeFileName();
            return path;
        }

        public override string GetItemName(IUserType item)
        {
            return item.Name;
        }
    }
}