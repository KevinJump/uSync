using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Umbraco.Core;
using Umbraco.Core.Models;

namespace uSync.ContentEdition.Extensions
{
    public static class ContentScheduleExtensions
    {
        /// <summary>
        ///  Works out what state a specific culture should be in for a node (e.g published, unpublished, saved)
        /// </summary>
        public static uSyncContentState CalculateCultureState(this IList<ContentSchedule> schedules, string culture, uSyncContentState defaultState)
        {
            foreach (var schedule in schedules.Where(x => x.Culture.InvariantEquals(culture))
                .OrderBy(x => x.Date))
            {
                switch (schedule.Action)
                {
                    case ContentScheduleAction.Release:
                        if (schedule.Date < DateTime.Now)
                        {
                            defaultState = uSyncContentState.Published;
                        }
                        else
                        {
                            // if a schedule publish hasn't happend yet,
                            // if the whole culture is already 'published' we save it.
                            // but if its unpublished, then we keep that, so it will get 
                            // unpublished if it isn't 
                            if (defaultState == uSyncContentState.Published)
                            {
                                defaultState = uSyncContentState.Saved;
                            }
                        }
                        break;
                    case ContentScheduleAction.Expire:
                        if (schedule.Date < DateTime.Now)
                        {
                            defaultState = uSyncContentState.Unpublished;
                        }
                        break;
                }

            }
            return defaultState;
        }

    }
}
