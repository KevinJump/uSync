import {
	USYNC_DIFF_LIMITS,
	type USyncLineDiffBlock,
	type USyncLineDiffResult,
	type USyncLineDiffRow,
} from './types.js';

function normalize(text: string): string {
	return text.replace(/\r\n/g, '\n');
}

function contextRow(oldLine: number, newLine: number, text: string): USyncLineDiffRow {
	return { type: 'context', oldLine, newLine, text };
}

function removeRow(oldLine: number, text: string): USyncLineDiffRow {
	return { type: 'remove', oldLine, text };
}

function addRow(newLine: number, text: string): USyncLineDiffRow {
	return { type: 'add', newLine, text };
}

/**
 * Emits the whole [oldStart, oldEnd) / [newStart, newEnd) middle range as a
 * wholesale remove-then-add - used both when the pathological-size guard
 * trips and as the degrade path if Myers' `d` bound is hit.
 */
function wholesaleRows(
	oldLines: string[],
	newLines: string[],
	oldStart: number,
	oldEnd: number,
	newStart: number,
	newEnd: number,
): USyncLineDiffRow[] {
	const rows: USyncLineDiffRow[] = [];
	for (let i = oldStart; i < oldEnd; i++) rows.push(removeRow(i + 1, oldLines[i]));
	for (let i = newStart; i < newEnd; i++) rows.push(addRow(i + 1, newLines[i]));
	return rows;
}

const MAX_D = 2000;

/**
 * Myers greedy forward diff (the classic O(ND) algorithm) over the given
 * line ranges. Returns `null` if the edit distance exceeds `MAX_D`, in which
 * case the caller should degrade to a wholesale remove/add instead of
 * continuing to search.
 */
function myersDiff(
	oldLines: string[],
	newLines: string[],
	oldStart: number,
	oldEnd: number,
	newStart: number,
	newEnd: number,
): USyncLineDiffRow[] | null {
	const n = oldEnd - oldStart;
	const m = newEnd - newStart;
	const max = Math.min(n + m, MAX_D);

	if (n === 0 && m === 0) return [];

	const offset = max;
	const v = new Int32Array(2 * max + 1);
	const trace: Int32Array[] = [];

	let found = false;
	let foundD = -1;

	for (let d = 0; d <= max; d++) {
		const snapshot = v.slice();
		trace.push(snapshot);

		for (let k = -d; k <= d; k += 2) {
			let x: number;
			if (k === -d || (k !== d && v[k - 1 + offset] < v[k + 1 + offset])) {
				x = v[k + 1 + offset];
			} else {
				x = v[k - 1 + offset] + 1;
			}
			let y = x - k;

			while (x < n && y < m && oldLines[oldStart + x] === newLines[newStart + y]) {
				x++;
				y++;
			}

			v[k + offset] = x;

			if (x >= n && y >= m) {
				found = true;
				foundD = d;
				break;
			}
		}
		if (found) break;
	}

	if (!found) return null;

	// Backtrack through the trace to recover the edit script.
	const rows: USyncLineDiffRow[] = [];
	let x = n;
	let y = m;

	for (let d = foundD; d > 0; d--) {
		const vPrev = trace[d];
		const k = x - y;

		let prevK: number;
		if (k === -d || (k !== d && vPrev[k - 1 + offset] < vPrev[k + 1 + offset])) {
			prevK = k + 1;
		} else {
			prevK = k - 1;
		}
		const prevX = vPrev[prevK + offset];
		const prevY = prevX - prevK;

		while (x > prevX && y > prevY) {
			rows.push(contextRow(oldStart + x, newStart + y, oldLines[oldStart + x - 1]));
			x--;
			y--;
		}

		if (x === prevX) {
			rows.push(addRow(newStart + y, newLines[newStart + y - 1]));
		} else {
			rows.push(removeRow(oldStart + x, oldLines[oldStart + x - 1]));
		}
		x = prevX;
		y = prevY;
	}

	while (x > 0 && y > 0) {
		rows.push(contextRow(oldStart + x, newStart + y, oldLines[oldStart + x - 1]));
		x--;
		y--;
	}

	rows.reverse();
	return rows;
}

/**
 * Diffs two texts line by line. Normalises `\r\n` to `\n` before splitting
 * (uSync files get written on one OS and imported on another - without this
 * a line-ending-only change would diff the whole file), trims the identical
 * common prefix/suffix so large files with one changed line stay cheap, and
 * degrades to a wholesale remove/add - bounded, honest output - rather than
 * letting the O(ND) middle blow up.
 */
export function diffTextLines(oldText: string, newText: string): USyncLineDiffResult {
	const oldLines = normalize(oldText).split('\n');
	const newLines = normalize(newText).split('\n');

	const minLen = Math.min(oldLines.length, newLines.length);

	let prefix = 0;
	while (prefix < minLen && oldLines[prefix] === newLines[prefix]) prefix++;

	let suffix = 0;
	while (
		suffix < minLen - prefix &&
		oldLines[oldLines.length - 1 - suffix] === newLines[newLines.length - 1 - suffix]
	) {
		suffix++;
	}

	const oldMidStart = prefix;
	const oldMidEnd = oldLines.length - suffix;
	const newMidStart = prefix;
	const newMidEnd = newLines.length - suffix;

	const oldMidLen = oldMidEnd - oldMidStart;
	const newMidLen = newMidEnd - newMidStart;

	let degraded = false;
	let middleRows: USyncLineDiffRow[];

	if (oldMidLen * newMidLen > USYNC_DIFF_LIMITS.maxLineDiffArea) {
		degraded = true;
		middleRows = wholesaleRows(
			oldLines,
			newLines,
			oldMidStart,
			oldMidEnd,
			newMidStart,
			newMidEnd,
		);
	} else {
		const myers = myersDiff(
			oldLines,
			newLines,
			oldMidStart,
			oldMidEnd,
			newMidStart,
			newMidEnd,
		);
		if (myers === null) {
			degraded = true;
			middleRows = wholesaleRows(
				oldLines,
				newLines,
				oldMidStart,
				oldMidEnd,
				newMidStart,
				newMidEnd,
			);
		} else {
			middleRows = myers;
		}
	}

	const rows: USyncLineDiffRow[] = [];
	for (let i = 0; i < prefix; i++) rows.push(contextRow(i + 1, i + 1, oldLines[i]));
	rows.push(...middleRows);
	for (let i = 0; i < suffix; i++) {
		const oldLine = oldLines.length - suffix + i;
		const newLine = newLines.length - suffix + i;
		rows.push(contextRow(oldLine + 1, newLine + 1, oldLines[oldLine]));
	}

	return { oldLines, newLines, degraded, rows };
}

/**
 * Collapses long runs of unchanged context rows down to a small lead-in/
 * lead-out plus a single collapsed block, keeping the hidden rows so a
 * consumer can splice them back in on click without re-diffing.
 */
export function collapseUnchanged(
	rows: USyncLineDiffRow[],
	context: number = USYNC_DIFF_LIMITS.lineContext,
): USyncLineDiffBlock[] {
	const blocks: USyncLineDiffBlock[] = [];
	let i = 0;

	while (i < rows.length) {
		if (rows[i].type !== 'context') {
			blocks.push({ type: 'row', row: rows[i] });
			i++;
			continue;
		}

		let end = i;
		while (end < rows.length && rows[end].type === 'context') end++;
		const run = rows.slice(i, end);

		if (run.length <= 2 * context + 1) {
			for (const row of run) blocks.push({ type: 'row', row });
		} else {
			const lead = run.slice(0, context);
			const hidden = run.slice(context, run.length - context);
			const trail = run.slice(run.length - context);
			for (const row of lead) blocks.push({ type: 'row', row });
			blocks.push({ type: 'collapsed', count: hidden.length, rows: hidden });
			for (const row of trail) blocks.push({ type: 'row', row });
		}

		i = end;
	}

	return blocks;
}
