import { UmbLitElement } from '@umbraco-cms/backoffice/lit-element';
import {
	type PropertyValues,
	classMap,
	css,
	customElement,
	html,
	nothing,
	property,
	state,
	styleMap,
	when,
} from '@umbraco-cms/backoffice/external/lit';
import '@umbraco-cms/backoffice/components';
import { diffWords, type UmbDiffChange } from '@umbraco-cms/backoffice/utils';
import { ChangeDetailType, type USyncChange } from '../api';
import {
	USYNC_DIFF_LIMITS,
	classifyChange,
	collapseUnchanged,
	diffJsonValues,
	diffTextLines,
	formatDiffPath,
	formatLeafValue,
	parseChangeName,
	prettyJson,
	type USyncChangeLabel,
	type USyncClassifiedChange,
	type USyncJsonDiffResult,
	type USyncLeafChange,
	type USyncLineDiffBlock,
	type USyncLineDiffRow,
} from '../utils/diff/index.js';

/**
 * Renders a single `USyncChange` detail as an expandable row, using the
 * strategy chosen by `classifyChange` to pick a compact, readable diff
 * rather than a wall of pretty-printed JSON.
 */
@customElement('usync-change-detail')
export class uSyncChangeDetail extends UmbLitElement {
	@property({ type: Object })
	detail?: USyncChange;

	@property({ type: Boolean, reflect: true })
	open = false;

	@state()
	private _classified?: USyncClassifiedChange;

	@state()
	private _json?: USyncJsonDiffResult;

	@state()
	private _lines?: USyncLineDiffBlock[];

	@state()
	private _lineDegraded = false;

	@state()
	private _label?: USyncChangeLabel;

	@state()
	private _showRaw = false;

	private _expandedRuns = new Set<number>();
	private _expandedLeaves = new Set<number>();

	// Diffing happens here, and only here - `render()` runs on every property
	// change (including a plain expand/collapse click), so computing the
	// diff there would re-diff on every toggle instead of once per detail.
	protected willUpdate(changed: PropertyValues<this>) {
		if (!changed.has('detail') || !this.detail) return;

		const detail = this.detail;

		this._label = parseChangeName(detail.name);
		this._classified = classifyChange(detail);
		this._json =
			this._classified.strategy === 'json'
				? diffJsonValues(this._classified.oldJson, this._classified.newJson)
				: undefined;

		if (this._classified.strategy === 'lines') {
			const lineDiff = diffTextLines(this._classified.oldText, this._classified.newText);
			this._lines = collapseUnchanged(lineDiff.rows);
			this._lineDegraded = lineDiff.degraded;
		} else {
			this._lines = undefined;
			this._lineDegraded = false;
		}

		this._expandedRuns.clear();
		this._expandedLeaves.clear();
		this._showRaw = false;

		if (detail.success === false) {
			this.open = true;
		}
	}

	#toggleOpen() {
		this.open = !this.open;
	}

	#toggleRun(index: number) {
		if (this._expandedRuns.has(index)) {
			this._expandedRuns.delete(index);
		} else {
			this._expandedRuns.add(index);
		}
		this.requestUpdate();
	}

	#toggleLeaf(index: number) {
		if (this._expandedLeaves.has(index)) {
			this._expandedLeaves.delete(index);
		} else {
			this._expandedLeaves.add(index);
		}
		this.requestUpdate();
	}

	#toggleRaw() {
		this._showRaw = !this._showRaw;
	}

	#copyPath() {
		if (!this.detail) return;
		navigator.clipboard?.writeText(this.detail.path)?.catch(() => undefined);
	}

	#isExpandable(strategy: USyncClassifiedChange['strategy']): boolean {
		return strategy !== 'scalar' && strategy !== 'skip';
	}

	#lineCounts(): { added: number; removed: number } {
		let added = 0;
		let removed = 0;
		for (const block of this._lines ?? []) {
			if (block.type !== 'row') continue;
			if (block.row.type === 'add') added++;
			else if (block.row.type === 'remove') removed++;
		}
		return { added, removed };
	}

	render() {
		if (!this.detail || !this._classified) return nothing;

		const detail = this.detail;
		const classified = this._classified;
		const failed = detail.success === false;
		const expandable = this.#isExpandable(classified.strategy);
		const isOpen = this.open;

		return html`
			<div class=${classMap({ 'change-row': true, danger: failed })}>
				<div
					class=${classMap({ summary: true, expanded: isOpen, static: !expandable })}
					@click=${expandable ? this.#toggleOpen : undefined}>
					<div class="summary-left">
						${when(
							failed,
							() => html`<umb-icon name="icon-alert" class="color-red"></umb-icon>`,
						)}
						${when(
							this._label?.prefix,
							() =>
								html`<span class="prefix"
									>${this.localize.term('uSync_changeProperty')}</span
								>`,
						)}
						<span class="label" title=${detail.path}
							>${this._label?.label || detail.name}</span
						>
						${when(
							this._label?.culture,
							() =>
								html`<uui-tag size="s" look="secondary"
									>${this._label!.culture}</uui-tag
								>`,
						)}
					</div>
					<div class="summary-right">
						<span class=${classMap({ danger: classified.strategy === 'notice' })}
							>${this.#renderSummary(classified)}</span
						>
						${when(
							expandable,
							() =>
								html`<uui-icon
									name="icon-play"
									class=${classMap({ expanded: isOpen })}></uui-icon>`,
						)}
					</div>
				</div>
				${when(
					isOpen && expandable,
					() => html`
						<div class="body">
							${this.#renderBody(classified)}
							<div class="path-line">
								<span class="muted"
									>${this.localize.term('uSync_diffPath', detail.path)}</span
								>
								<div class="path-line-actions">
									${when(
										classified.strategy === 'json',
										() => html`
											<uui-button look="text" compact @click=${this.#toggleRaw}>
												${this._showRaw
													? this.localize.term('uSync_diffHideRaw')
													: this.localize.term('uSync_diffShowRaw')}
											</uui-button>
										`,
									)}
									<uui-button
										look="text"
										compact
										label=${this.localize.term('uSync_diffCopyPath')}
										@click=${this.#copyPath}>
										<uui-icon name="icon-documents"></uui-icon>
									</uui-button>
								</div>
							</div>
						</div>
					`,
				)}
			</div>
		`;
	}

	#renderSummary(c: USyncClassifiedChange) {
		switch (c.strategy) {
			case 'scalar':
				return html`<span class="inline-change"
					><del>${c.oldText}</del> → <ins>${c.newText}</ins></span
				>`;
			case 'json': {
				const diff = this._json;
				if (!diff) return nothing;
				if (diff.truncated) {
					return this.localize.term(
						'uSync_diffChangeCapped',
						diff.changes.length,
						diff.totalCount,
					);
				}
				if (diff.totalCount === 1) return this.localize.term('uSync_diffOneChange');
				return this.localize.term('uSync_diffChangeCount', diff.totalCount);
			}
			case 'lines': {
				const { added, removed } = this.#lineCounts();
				return this.localize.term('uSync_diffLineCount', added, removed);
			}
			case 'masked':
				return this.localize.term('uSync_diffMaskedShort');
			case 'single':
				return c.oldText === ''
					? this.localize.term('uSync_diffValueAdded')
					: this.localize.term('uSync_diffValueRemoved');
			case 'oversized':
				return this.localize.term('uSync_diffTooLargeShort');
			case 'notice':
				return c.oldText;
			case 'words':
				return this.localize.term('uSync_diffValueChanged');
			case 'skip':
			default:
				return this.localize.term('uSync_noChange');
		}
	}

	#renderBody(c: USyncClassifiedChange) {
		switch (c.strategy) {
			case 'json':
				return this.#renderJsonBody(c);
			case 'lines':
				return this.#renderLinesBody();
			case 'words':
				return this.#renderWordsBody(c);
			case 'single':
				return this.#renderSingleBody(c);
			case 'masked':
				return this.#renderMaskedBody();
			case 'oversized':
				return this.#renderOversizedBody(c);
			case 'notice':
				return this.#renderNoticeBody(c);
			default:
				return nothing;
		}
	}

	#renderJsonBody(c: USyncClassifiedChange) {
		const diff = this._json;
		if (!diff) return nothing;

		return html`
			<ul class="leaf-list">
				${diff.changes.map((leaf, index) => this.#renderLeaf(leaf, index))}
			</ul>
			${when(
				diff.truncated,
				() =>
					html`<p class="muted">
						${this.localize.term(
							diff.stopped ? 'uSync_diffMoreChangesAtLeast' : 'uSync_diffMoreChanges',
							diff.totalCount - diff.changes.length,
						)}
					</p>`,
			)}
			${when(
				diff.reordered.length > 0,
				() =>
					html`<p class="muted">
						${this.localize.term('uSync_diffReordered')}: ${diff.reordered.join(', ')}
					</p>`,
			)}
			${when(
				this._showRaw,
				() => html`
					<div class="raw-values">
						<div>
							<div class="muted">${this.localize.term('uSync_diffOldValue')}</div>
							<umb-code-block language="json" copy
								>${prettyJson(c.oldJson)}</umb-code-block
							>
						</div>
						<div>
							<div class="muted">${this.localize.term('uSync_diffNewValue')}</div>
							<umb-code-block language="json" copy
								>${prettyJson(c.newJson)}</umb-code-block
							>
						</div>
					</div>
				`,
			)}
		`;
	}

	#renderLeaf(leaf: USyncLeafChange, index: number) {
		const path = formatDiffPath(leaf.path);
		const symbol = leaf.kind === 'added' ? '+' : leaf.kind === 'removed' ? '−' : '~';

		return html`
			<li class=${classMap({ leaf: true, [leaf.kind]: true })}>
				<div class="leaf-path" title=${leaf.path}>
					<span class="symbol">${symbol}</span>${path}
					${when(
						leaf.expanded,
						() =>
							html`<uui-tag
								size="s"
								look="secondary"
								title=${this.localize.term('uSync_diffExpandedNote')}
								>»</uui-tag
							>`,
					)}
				</div>
				<div class="leaf-value">${this.#renderLeafValue(leaf, index)}</div>
			</li>
		`;
	}

	#renderLeafValue(leaf: USyncLeafChange, index: number) {
		if (leaf.kind === 'added') {
			return html`<span class="added-value">${formatLeafValue(leaf.newValue)}</span>`;
		}
		if (leaf.kind === 'removed') {
			return html`<span class="removed-value">${formatLeafValue(leaf.oldValue)}</span>`;
		}

		const oldText = formatLeafValue(leaf.oldValue);
		const newText = formatLeafValue(leaf.newValue);
		const bothSingleLine = !oldText.includes('\n') && !newText.includes('\n');
		const combinedLen = oldText.length + newText.length;

		if (bothSingleLine && combinedLen <= USYNC_DIFF_LIMITS.inlineScalarChars) {
			return html`<span class="inline-change"
				><del>${oldText}</del> → <ins>${newText}</ins></span
			>`;
		}

		const bothStrings =
			typeof leaf.oldValue === 'string' && typeof leaf.newValue === 'string';
		const hasWhitespace = /\s/.test(oldText) || /\s/.test(newText);

		if (
			bothStrings &&
			hasWhitespace &&
			combinedLen <= USYNC_DIFF_LIMITS.maxScalarWordDiffBytes
		) {
			const words = diffWords(oldText, newText);
			return html`<span class="word-diff"
				>${words.map((word: UmbDiffChange) =>
					word.added
						? html`<ins>${word.value}</ins>`
						: word.removed
							? html`<del>${word.value}</del>`
							: html`<span>${word.value}</span>`,
				)}</span
			>`;
		}

		const expanded = this._expandedLeaves.has(index);
		return html`
			<uui-button look="text" compact @click=${() => this.#toggleLeaf(index)}>
				${expanded
					? this.localize.term('uSync_diffHideRaw')
					: this.localize.term('uSync_diffShowRaw')}
			</uui-button>
			${when(
				expanded,
				() => html`
					<pre class="stacked"><del>- ${oldText}</del>
<ins>+ ${newText}</ins></pre>
				`,
			)}
		`;
	}

	#renderLinesBody() {
		const blocks = this._lines ?? [];

		return html`
			${when(
				this._lineDegraded,
				() => html`<p class="muted">${this.localize.term('uSync_diffDegraded')}</p>`,
			)}
			<uui-scroll-container class="line-diff">
				${blocks.map((block, index) =>
					block.type === 'row'
						? this.#renderLineRow(block.row)
						: this.#renderCollapsed(block, index),
				)}
			</uui-scroll-container>
		`;
	}

	#renderLineRow(row: USyncLineDiffRow) {
		const tint =
			row.type === 'add'
				? 'color-mix(in srgb, var(--uui-color-positive) 12%, transparent)'
				: row.type === 'remove'
					? 'color-mix(in srgb, var(--uui-color-danger) 12%, transparent)'
					: undefined;
		const marker = row.type === 'add' ? '+' : row.type === 'remove' ? '-' : ' ';

		return html`
			<div
				class="line-row ${row.type}"
				style=${styleMap({ background: tint ?? 'transparent' })}>
				<span class="gutter">${row.oldLine ?? ''}</span>
				<span class="gutter">${row.newLine ?? ''}</span>
				<span class="marker">${marker}</span>
				<span class="text">${row.text}</span>
			</div>
		`;
	}

	#renderCollapsed(
		block: Extract<USyncLineDiffBlock, { type: 'collapsed' }>,
		index: number,
	) {
		const expanded = this._expandedRuns.has(index);

		return html`
			<div class="collapsed-toggle" @click=${() => this.#toggleRun(index)}>
				<uui-icon name="icon-navigation"></uui-icon>
				${expanded
					? this.localize.term('uSync_diffHideUnchanged')
					: this.localize.term('uSync_diffShowUnchanged', block.count)}
			</div>
			${when(expanded, () => html`${block.rows.map((row) => this.#renderLineRow(row))}`)}
		`;
	}

	#renderWordsBody(c: USyncClassifiedChange) {
		const changes = diffWords(c.oldText, c.newText);
		return html`<pre class="word-diff">
${changes.map((word: UmbDiffChange) =>
				word.added
					? html`<ins>${word.value}</ins>`
					: word.removed
						? html`<del>${word.value}</del>`
						: html`<span>${word.value}</span>`,
			)}</pre
		>`;
	}

	#renderSingleBody(c: USyncClassifiedChange) {
		const isAdded = c.oldText === '';
		const value = isAdded ? c.newText : c.oldText;
		const tint = isAdded
			? 'color-mix(in srgb, var(--uui-color-positive) 12%, transparent)'
			: 'color-mix(in srgb, var(--uui-color-danger) 12%, transparent)';

		return html`
			<div class="muted">
				${isAdded
					? this.localize.term('uSync_diffValueAdded')
					: this.localize.term('uSync_diffValueRemoved')}
			</div>
			<div class="tinted" style=${styleMap({ background: tint })}>
				<umb-code-block language=${c.language} copy>${value}</umb-code-block>
			</div>
		`;
	}

	#renderMaskedBody() {
		return html`<p class="muted">${this.localize.term('uSync_diffMasked')}</p>`;
	}

	#renderOversizedBody(c: USyncClassifiedChange) {
		return html`
			<p class="muted">${this.localize.term('uSync_diffTooLarge')}</p>
			<div class="raw-values">
				<div>
					<div class="muted">${this.localize.term('uSync_diffOldValue')}</div>
					<umb-code-block language=${c.language} copy>${c.oldText}</umb-code-block>
				</div>
				<div>
					<div class="muted">${this.localize.term('uSync_diffNewValue')}</div>
					<umb-code-block language=${c.language} copy>${c.newText}</umb-code-block>
				</div>
			</div>
		`;
	}

	#renderNoticeBody(c: USyncClassifiedChange) {
		const label =
			this.detail?.change === ChangeDetailType.ERROR
				? this.localize.term('uSync_changeError')
				: this.localize.term('uSync_changeWarning');

		return html`
			<div class="notice-callout danger">
				<strong>${label}</strong>
				<p>${c.oldText}</p>
			</div>
		`;
	}

	static styles = css`
		:host {
			display: block;
			border-bottom: 1px solid var(--uui-color-border);
		}

		.change-row.danger {
			border-left: 3px solid var(--uui-color-danger);
		}

		.summary {
			display: flex;
			align-items: center;
			justify-content: space-between;
			gap: var(--uui-size-space-4);
			padding: var(--uui-size-space-3) var(--uui-size-space-5);
			cursor: pointer;
		}

		.summary.static {
			cursor: default;
		}

		.summary:hover:not(.static) {
			background-color: var(--uui-color-surface-alt);
		}

		.summary-left {
			display: flex;
			align-items: center;
			gap: var(--uui-size-space-3);
			min-width: 0;
			overflow: hidden;
		}

		.summary-left .label {
			font-weight: bold;
			overflow: hidden;
			text-overflow: ellipsis;
			white-space: nowrap;
		}

		.prefix {
			color: var(--uui-color-disabled-contrast);
		}

		.summary-right {
			display: flex;
			align-items: center;
			gap: var(--uui-size-space-4);
			flex-shrink: 0;
		}

		.summary-right uui-icon {
			transition: transform 90ms ease;
		}

		.summary-right uui-icon.expanded {
			transform: rotate(90deg);
		}

		.danger {
			color: var(--uui-color-danger);
		}

		.color-red {
			color: var(--uui-color-danger);
		}

		.muted {
			color: var(--uui-color-disabled-contrast);
			font-size: smaller;
		}

		.body {
			padding: 0 var(--uui-size-space-5) var(--uui-size-space-4);
		}

		.path-line {
			display: flex;
			align-items: center;
			justify-content: space-between;
			gap: var(--uui-size-space-3);
			margin-top: var(--uui-size-space-3);
		}

		.path-line > .muted {
			overflow: hidden;
			text-overflow: ellipsis;
			white-space: nowrap;
		}

		.path-line-actions {
			display: flex;
			align-items: center;
			gap: var(--uui-size-space-1, 3px);
			flex-shrink: 0;
		}

		.leaf-list {
			list-style: none;
			margin: 0;
			padding: 0;
			display: flex;
			flex-direction: column;
			gap: var(--uui-size-space-3);
		}

		.leaf {
			display: flex;
			flex-direction: column;
			gap: var(--uui-size-space-1, 3px);
			padding: var(--uui-size-space-2) var(--uui-size-space-3);
			border-radius: var(--uui-border-radius);
			border: 1px solid var(--uui-color-border);
			background: var(--uui-color-surface-alt);
		}

		.leaf.added {
			background: color-mix(in srgb, var(--uui-color-positive) 8%, transparent);
			border-color: color-mix(in srgb, var(--uui-color-positive) 30%, transparent);
		}

		.leaf.removed {
			background: color-mix(in srgb, var(--uui-color-danger) 8%, transparent);
			border-color: color-mix(in srgb, var(--uui-color-danger) 30%, transparent);
		}

		.leaf-path {
			font-family: monospace;
			font-size: smaller;
			font-weight: 600;
			color: var(--uui-color-text);
			overflow: hidden;
			text-overflow: ellipsis;
			white-space: nowrap;
		}

		.symbol {
			display: inline-block;
			width: 1em;
			font-weight: bold;
		}

		.added-value {
			color: var(--uui-color-positive);
		}

		.removed-value {
			color: var(--uui-color-danger);
		}

		.inline-change del {
			color: var(--uui-color-danger);
			text-decoration: line-through;
		}

		.inline-change ins {
			color: var(--uui-color-positive);
			text-decoration: none;
		}

		.word-diff {
			white-space: pre-wrap;
		}

		.word-diff ins {
			color: var(--uui-color-positive);
			text-decoration: none;
		}

		.word-diff del {
			color: var(--uui-color-danger);
			text-decoration: line-through;
		}

		.stacked {
			margin: 0;
			white-space: pre;
		}

		.stacked del {
			display: block;
			color: var(--uui-color-danger);
			text-decoration: none;
		}

		.stacked ins {
			display: block;
			color: var(--uui-color-positive);
			text-decoration: none;
		}

		.raw-values {
			display: flex;
			flex-direction: column;
			gap: var(--uui-size-space-4);
			margin-top: var(--uui-size-space-3);
		}

		.tinted {
			border-radius: var(--uui-border-radius);
		}

		uui-scroll-container.line-diff {
			max-height: 400px;
			display: block;
			border: 1px solid var(--uui-color-border);
			border-radius: var(--uui-border-radius);
		}

		.line-row {
			display: flex;
			gap: var(--uui-size-space-2);
			font-family: monospace;
			white-space: pre;
			padding: 0 var(--uui-size-space-2);
		}

		.line-row .gutter {
			display: inline-block;
			width: 3em;
			text-align: right;
			color: var(--uui-color-disabled-contrast);
			flex-shrink: 0;
		}

		.line-row .marker {
			flex-shrink: 0;
			width: 1em;
		}

		.line-row .text {
			white-space: pre;
		}

		.collapsed-toggle {
			display: flex;
			align-items: center;
			gap: var(--uui-size-space-2);
			padding: var(--uui-size-space-2);
			color: var(--uui-color-disabled-contrast);
			cursor: pointer;
			font-size: smaller;
		}

		.collapsed-toggle:hover {
			background-color: var(--uui-color-surface-alt);
		}

		.notice-callout {
			padding: var(--uui-size-space-4);
			border-radius: var(--uui-border-radius);
			background: color-mix(in srgb, var(--uui-color-danger) 10%, transparent);
		}

		.notice-callout p {
			margin: var(--uui-size-space-2) 0 0;
		}
	`;
}

export default uSyncChangeDetail;

declare global {
	interface HTMLElementTagNameMap {
		'usync-change-detail': uSyncChangeDetail;
	}
}
