// Files that use the json helpers import Jumoo.Json instead of uSync.Core.Extensions - the two
// declare the same extension method signatures, so having both in scope is ambiguous (CS0121).
// uSyncTaskHelper lives in uSync.Core.Extensions and is public, so it can't move; aliasing it
// here keeps it available to those files without pulling the whole namespace back in.
global using uSyncTaskHelper = uSync.Core.Extensions.uSyncTaskHelper;
