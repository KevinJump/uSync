# uSync end-to-end tests

Playwright tests that drive the uSync backoffice in a real Umbraco site.

These run locally only. They are not part of the GitHub workflows, because the
test sites they run against are not in the repo.

## Setup

From the repo root:

    npm run install-e2e

## Running

    npm run test-e2e

With no configuration the tests start `uSyncSource.Site` on
`https://localhost:44398` (or reuse it when it is already running) and sign in
as the unattended install user, read from the
`UMBRACO__CMS__UNATTENDED__UNATTENDEDUSEREMAIL` and
`UMBRACO__CMS__UNATTENDED__UNATTENDEDUSERPASSWORD` environment variables.

To test another site or sign in as a different user, copy `.env.example` to
`.env` and set the values there.

From this folder, `npm run test:ui` opens the Playwright UI and
`npm run report` shows the last HTML report.
