export type USyncLeafChangeKind = 'added' | 'removed' | 'changed';

export interface USyncLeafChange {
	path: string; // "contentData[0: Hero].values[headline (en-US)]"
	kind: USyncLeafChangeKind;
	oldValue?: unknown;
	newValue?: unknown;
	expanded?: boolean; // path crossed into an encoded JSON string
}

export interface USyncJsonDiffResult {
	changes: USyncLeafChange[];
	totalCount: number; // before the cap
	truncated: boolean;
	/**
	 * True when the walk aborted early on a runaway subtree, which makes
	 * `totalCount` a floor rather than an exact count - report it as
	 * "N or more changes".
	 */
	stopped: boolean;
	reordered: string[]; // arrays whose contents matched but order changed
}

export type USyncDiffStrategy =
	| 'skip'
	| 'notice'
	| 'masked'
	| 'single'
	| 'json'
	| 'lines'
	| 'words'
	| 'scalar'
	| 'oversized';

export interface USyncClassifiedChange {
	strategy: USyncDiffStrategy;
	oldText: string;
	newText: string;
	oldJson?: unknown; // strategy === 'json' only
	newJson?: unknown;
	language: 'json' | 'xml' | 'text';
	bytes: number;
}

export type USyncLineRowType = 'context' | 'add' | 'remove';

export interface USyncLineDiffRow {
	type: USyncLineRowType;
	oldLine?: number;
	newLine?: number;
	text: string;
}

export interface USyncLineDiffResult {
	oldLines: string[];
	newLines: string[];
	degraded: boolean;
	rows: USyncLineDiffRow[];
}

export type USyncLineDiffBlock =
	| { type: 'row'; row: USyncLineDiffRow }
	| { type: 'collapsed'; count: number; rows: USyncLineDiffRow[] };

// All thresholds in one exported const so they are tunable in one line.
export const USYNC_DIFF_LIMITS = {
	maxDiffBytes: 512_000, // above: raw only, no walk/diff at all
	maxWordDiffBytes: 8_000, // diffWords is ~quadratic; also ~where a word diff stops being readable
	maxScalarWordDiffBytes: 2_000,
	inlineScalarChars: 80, // at or below: render inline as "old → new"
	maxLeafChanges: 40,
	maxJsonDepth: 12,
	maxStringExpansions: 3,
	maxLineDiffArea: 4_000_000, // |old| * |new| lines before degrading
	lineContext: 3,
} as const;
