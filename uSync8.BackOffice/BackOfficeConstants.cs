﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace uSync8.BackOffice
{
    public static partial class uSyncBackOfficeConstants
    {
        public static class Priorites
        {
            public const int USYNC_RESERVED_LOWER = 1000;
            public const int USYNC_RESERVED_UPPER = 2000;

            public const int DataTypes = USYNC_RESERVED_LOWER + 10;
            public const int Templates = USYNC_RESERVED_LOWER + 20;

            public const int ContentTypes = USYNC_RESERVED_LOWER + 30;
            public const int MediaTypes = USYNC_RESERVED_LOWER + 40;
            public const int MemberTypes = USYNC_RESERVED_LOWER + 45;

            public const int Languages = USYNC_RESERVED_LOWER + 5;
            public const int DictionaryItems = USYNC_RESERVED_LOWER + 6;
            public const int Macros = USYNC_RESERVED_LOWER + 70;

            public const int Media = USYNC_RESERVED_LOWER + 200;
            public const int Content = USYNC_RESERVED_LOWER + 210;
            public const int ContentTemplate = USYNC_RESERVED_LOWER + 215;

            public const int DomainSettings = USYNC_RESERVED_LOWER + 219;

            public const int DataTypeMappings = USYNC_RESERVED_LOWER + 220;
        }
    }
}
