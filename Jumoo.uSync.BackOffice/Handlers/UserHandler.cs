using System.Collections.Generic;
using Jumoo.uSync.BackOffice.Helpers;
using Jumoo.uSync.Core;
using Jumoo.uSync.Core.Extensions.Umbraco;
using Jumoo.uSync.Core.Serializers.Dtos;
using Umbraco.Core;
using Umbraco.Core.Events;
using Umbraco.Core.Logging;
using Umbraco.Core.Models.Membership;
using Umbraco.Core.Services;

namespace Jumoo.uSync.BackOffice.Handlers
{
    public class UserHandler : uSyncExtendedHandler<IUser>, ISyncHandler
    {
        public int Priority => uSyncConstants.Priority.User;
        public string Name => "uSync: UserHandler";
        public override string SyncFolder => "User";

        private readonly IUserService _userService;

        public delegate void UserSavedEventHandler(IUserService sender, SaveEventArgs<IUser> e);
        public static event UserSavedEventHandler BeforeSave;

        public UserHandler() : base(new UserSerializer())
        {
            _userService = ApplicationContext.Current.Services.UserService;
        }

        public IEnumerable<uSyncAction> ExportAll(string folder)
        {
            LogHelper.Info<UserHandler>("Exporting Users");

            var actions = new List<uSyncAction>();
      
            foreach (var user in _userService.GetAllUsers())
            {
                actions.Add(Export(user, GetItemPath(user), folder));
            }

            return actions;
        }

        public void RegisterEvents()
        {
            UserService.SavedUser += UserSaved;
            UserService.SavingUser += UserSaving;
            UserService.DeletedUser += UserDeleted;
        }

        private void UserSaving(IUserService sender, SaveEventArgs<IUser> e)
        {
            if (uSyncEvents.Paused)
                return;

            foreach (var eSavedEntity in e.SavedEntities)
            {
                if (!eSavedEntity.HasIdentity) continue;
                var persistedUser = _userService.GetUserById(eSavedEntity.Id);
                if (persistedUser.Name == eSavedEntity.Name) continue;
                DeleteFile(persistedUser, false);
            }
        }

        private void UserSaved(IUserService sender, SaveEventArgs<IUser> e)
        {
            BeforeSave?.Invoke(sender, e);

            if (uSyncEvents.Paused)
                return;

            SaveItems(sender, e.SavedEntities);   
        }

        private void UserDeleted(IUserService sender, DeleteEventArgs<IUser> e)
        {
            if (uSyncEvents.Paused)
                return;

            foreach (var deletedEntity in e.DeletedEntities)
            {
                DeleteFile(deletedEntity);
            }
        }

        private void SaveItems(IUserService sender, IEnumerable<IUser> savedUsers)
        {
            foreach (var savedUser in savedUsers)
            {
                LogHelper.Info<UserHandler>("Save: Saving uSync files for : {0}", () => savedUser.Name);
                var user = _userService.GetUserById(savedUser.Id);
                var attempt = Export(user, GetItemPath(user), uSyncBackOfficeContext.Instance.Configuration.Settings.Folder);
                if (attempt.Success)
                {
                    NameChecker.ManageOrphanFiles(SyncFolder, user.Key, attempt.FileName);
                }
            }
        }

        public override string GetItemPath(IUser user)
        {
            var path = user.Name.ToSafeFileName();
            return path;
        }

        public override string GetItemName(IUser item)
        {
            return item.Name;
        }
    }
}