import { css, html } from '@umbraco-cms/backoffice/external/lit';
import type { UmbLitElement } from '@umbraco-cms/backoffice/lit-element';
import type { uSyncPreviewLabels } from '../preview/types.js';

export type PreviewSide = 'current' | 'incoming';

/** the headline over one side of a preview. */
export function renderPreviewHeadline(
	host: UmbLitElement,
	side: PreviewSide,
	labels?: uSyncPreviewLabels,
) {
	const headline =
		side === 'current'
			? (labels?.current ??
				host.localize.termOrDefault('uSync_previewCurrent', 'Current (this site)'))
			: (labels?.incoming ??
				host.localize.termOrDefault('uSync_previewIncoming', 'Incoming (uSync files)'));

	return html`<h4 class="side-headline">${headline}</h4>`;
}

/**
 * styles shared by the sides of a preview (each side is `.side.current` or `.side.incoming`).
 *
 * the incoming side (the source) is always on the left, and the current side (the
 * target, which the import changes) on the right.
 *
 * what differs is red on the current side (what goes) and green on the incoming
 * side (what comes in), so you can see which way the change runs whichever way
 * round the sites are. elements colour their changes with `--usync-preview-change`.
 */
export const previewSideStyles = css`
	.side.current {
		--usync-preview-change: var(--uui-color-danger);
	}

	.side.incoming {
		--usync-preview-change: var(--uui-color-positive);
	}

	.side-headline {
		align-self: center;
		margin: 0;
		font-size: var(--uui-type-small-size);
		text-transform: uppercase;
		letter-spacing: 0.04em;
		color: var(--uui-color-text-alt);
	}
`;
