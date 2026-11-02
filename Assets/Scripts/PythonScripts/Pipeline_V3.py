#!/usr/bin/env python3
"""VISUAL ACCESSIBILITY PIPELINE — ORDERED PHASE PRESENTATION

Adaptive Visual Accessibility Classification System

Computational Pipeline

Baseline CSV → Validation → Variable Identification → Quartile Statistics
→ Rule-Based Accessibility Labeling → Pearson Correlation (reporting only)
→ ReliefF Feature Selection → C4.5-Style Decision Tree
→ Cross-Validation and Model Exports → Selected Player Inference
→ AI_RESULT.json (12 Unity-facing visual settings)

The executable function bodies are retained verbatim from the uploaded V4.5.
The initial V3 is a PRESENTATION reference, not an algorithm source.
The example baseline is synthetic and must not be reported as participant data.
"""

# ============================================================
# PHASE 0 — IMPORT LIBRARIES AND LOAD DATA
# ============================================================
# Import the original V4.5 dependencies without changing their behavior.

from __future__ import annotations
import argparse, json, hashlib
from pathlib import Path
from collections import Counter
import numpy as np
import pandas as pd

# Required variable definitions are declared before functions so every phase
# can refer to the same 12 features and two performance indicators.

FEATURES = [
    'background_color','background_contrast','background_brightness',
    'obstacle_color','obstacle_contrast','obstacle_brightness',
    'platform_color','platform_contrast','platform_brightness',
    'player_color','player_contrast','player_brightness'
]
PERFORMANCE = ['completion_time','error_count']
CLASSES = ['LOW','MEDIUM','HIGH']
REQUIRED = FEATURES + PERFORMANCE

# ------------------------------------------------------------
# CSV validation and input integrity
# ------------------------------------------------------------


# ------------------------------------------------------------
# Validate and normalize the baseline dataset; player inference uses its own visual-feature checks.
# ------------------------------------------------------------
def validate_frame(frame, name, min_rows=1):
    missing = [c for c in REQUIRED if c not in frame.columns]
    if missing: raise ValueError(f'{name}: missing required columns: {missing}')
    if len(frame) < min_rows: raise ValueError(f'{name}: requires at least {min_rows} record(s); found {len(frame)}.')
    out = frame[REQUIRED].copy()
    for c in REQUIRED: out[c] = pd.to_numeric(out[c], errors='coerce')
    if out.isna().any().any():
        bad = out.columns[out.isna().any()].tolist()
        raise ValueError(f'{name}: missing or non-numeric values in {bad}.')
    if not np.isfinite(out.to_numpy(float)).all(): raise ValueError(f'{name}: contains infinite values.')
    for c in FEATURES:
        if ((out[c] < 0) | (out[c] > 1)).any():
            raise ValueError(f'{name}: {c} must be normalized to [0,1].')
    if (out.completion_time <= 0).any():
        raise ValueError(f'{name}: completion_time must be > 0 for a completed attempt.')
    if (out.error_count < 0).any() or not np.allclose(out.error_count, np.round(out.error_count)):
        raise ValueError(f'{name}: error_count must be a nonnegative integer.')
    return out.reset_index(drop=True)


# ============================================================
# PHASE 1 — VARIABLE IDENTIFICATION
# ============================================================
# The 12 visual-perception variables are listed in FEATURES above.
# Gameplay indicators: completion_time and error_count (PERFORMANCE).
# These constants are shared by all phases and remain unchanged.


# ============================================================
# PHASE 2 — DESCRIPTIVE STATISTICS
# ============================================================

# ------------------------------------------------------------
# Compute baseline-only Q1 and Q3 cutoffs for completion time and errors.
# ------------------------------------------------------------
def derive_thresholds(df):
    """Operational cutoffs are derived only from the current training baseline."""
    return {
        'completion_q1': float(df.completion_time.quantile(.25)),
        'completion_q3': float(df.completion_time.quantile(.75)),
        'errors_q1': float(df.error_count.quantile(.25)),
        'errors_q3': float(df.error_count.quantile(.75)),
    }


# ------------------------------------------------------------
# Restore the complete V3 descriptive-statistics table for the
# training baseline only. The original quartile labeling is unchanged.
# ------------------------------------------------------------
def descriptive_statistics(values):
    """V3-compatible sample statistics; input must contain >= 2 observations."""
    values = np.asarray(values, dtype=float)
    return {
        'N': len(values),
        'Mean': float(np.mean(values)),
        'Median': float(np.median(values)),
        'Sample SD': float(np.std(values, ddof=1)),
        'Minimum': float(np.min(values)),
        'Q1 (25%)': float(np.percentile(values, 25)),
        'Q3 (75%)': float(np.percentile(values, 75)),
        'Maximum': float(np.max(values)),
    }


def baseline_descriptive_statistics(baseline):
    """Generate the V3-style summary without including the current player."""
    return pd.DataFrame(
        [descriptive_statistics(baseline[c].to_numpy(dtype=float)) for c in PERFORMANCE],
        index=['Completion Time', 'Error Count'],
    ).rename_axis('Gameplay Indicator')


# ============================================================
# PHASE 3 — RULE-BASED ACCESSIBILITY LABELING
# ============================================================

# ------------------------------------------------------------
# Assign LOW, MEDIUM, or HIGH using the original combined performance rules.
# ------------------------------------------------------------
def apply_labels(df, th):
    time_class = np.where(df.completion_time < th['completion_q1'], 'LOW',
                 np.where(df.completion_time <= th['completion_q3'], 'MEDIUM', 'HIGH'))
    error_class = np.where(df.error_count <= th['errors_q1'], 'LOW',
                  np.where(df.error_count <= th['errors_q3'], 'MEDIUM', 'HIGH'))
    combined = ['LOW' if a == 'LOW' and b == 'LOW' else
                'HIGH' if a == 'HIGH' and b == 'HIGH' else 'MEDIUM'
                for a, b in zip(time_class, error_class)]
    return np.asarray(combined), time_class, error_class


# ============================================================
# PHASE 4 — PEARSON CORRELATION
# ============================================================

# ------------------------------------------------------------
# Report feature correlations without removing or adjusting any features.
# ------------------------------------------------------------
def pearson_analysis(X, names, cutoff=.80):
    frame = pd.DataFrame(X, columns=names)
    rows = []
    for i, a in enumerate(names):
        for b in names[i+1:]:
            r = frame[a].corr(frame[b]) if frame[a].nunique() > 1 and frame[b].nunique() > 1 else np.nan
            rows.append({'Feature 1': a, 'Feature 2': b, 'Pearson r': r,
                         'Flag |r| >= threshold': bool(np.isfinite(r) and abs(r) >= cutoff)})
    return pd.DataFrame(rows)


# ============================================================
# PHASE 5 — RELIEFF FEATURE SELECTION
# ============================================================

# ------------------------------------------------------------
# Calculate class-weighted nearest-hit and nearest-miss contributions.
# ------------------------------------------------------------
def relief_f_weights(X, y, names, k=3):
    """ReliefF-style estimator with adaptive per-class neighbor counts.

    For each reference row, use up to k nearest hits and up to k nearest misses
    in each other class. Each class's miss contribution is averaged over the
    neighbors actually available and weighted by its prior probability. A
    training class must contain >=2 examples so a hit can be found.
    """
    X = np.asarray(X, float); y = np.asarray(y)
    classes = sorted(set(y), key=lambda c: CLASSES.index(c))
    counts = Counter(y)
    if len(classes) < 2: raise ValueError('ReliefF requires at least two represented classes.')
    if min(counts.values()) < 2:
        raise ValueError(f'ReliefF requires >=2 baseline records per represented class; got {dict(counts)}.')
    ranges = np.ptp(X, axis=0); ranges[ranges == 0] = 1.0
    diff = np.abs(X[:, None, :] - X[None, :, :]) / ranges
    distances = np.sum(diff, axis=2)
    n = len(y); weights = np.zeros(X.shape[1])
    priors = {c: counts[c] / n for c in classes}
    for i in range(n):
        hit_idx = np.where((y == y[i]) & (np.arange(n) != i))[0]
        hit_idx = hit_idx[np.argsort(distances[i, hit_idx])[:min(k, len(hit_idx))]]
        hit_mean = diff[i, hit_idx, :].mean(axis=0)
        miss = np.zeros(X.shape[1]); prior_other = 1.0 - priors[y[i]]
        for c in classes:
            if c == y[i]: continue
            idx = np.where(y == c)[0]
            idx = idx[np.argsort(distances[i, idx])[:min(k, len(idx))]]
            miss += (priors[c] / prior_other) * diff[i, idx, :].mean(axis=0)
        weights += (miss - hit_mean) / n
    return pd.DataFrame({'Feature': names, 'ReliefF Weight': weights}).sort_values(
        'ReliefF Weight', ascending=False, kind='mergesort').reset_index(drop=True)


# ------------------------------------------------------------
# Select features at the largest consecutive ReliefF weight gap.
# ------------------------------------------------------------
def choose_largest_gap(ranking):
    if len(ranking) < 2: return ranking.Feature.tolist()
    values = ranking['ReliefF Weight'].to_numpy(float)
    gaps = values[:-1] - values[1:]
    cut = int(np.argmax(gaps)) + 1
    return ranking.Feature.iloc[:cut].tolist()


# ============================================================
# PHASE 6 — C4.5 DECISION TREE IMPLEMENTATION
# ============================================================

# ------------------------------------------------------------
# Calculate label entropy for candidate split evaluation.
# ------------------------------------------------------------
def entropy(labels):
    counts = np.asarray(list(Counter(labels).values()), dtype=float)
    if not len(counts): return 0.0
    p = counts / counts.sum()
    return float(-np.sum(p * np.log2(p)))


# ------------------------------------------------------------
# Calculate split information for gain-ratio normalization.
# ------------------------------------------------------------
def split_info(n_left, n_right, total):
    p = np.asarray([n_left / total, n_right / total], float)
    return float(-np.sum(p[p > 0] * np.log2(p[p > 0])))


# ------------------------------------------------------------
# Evaluate numeric thresholds and select the best eligible gain-ratio split.
# ------------------------------------------------------------
def best_c45_split(X, y, feature_names, min_leaf=2):
    parent = entropy(y); n = len(y); candidates = []
    for j, name in enumerate(feature_names):
        vals = np.unique(X[:, j])
        if len(vals) < 2: continue
        for threshold in (vals[:-1] + vals[1:]) / 2:
            left = X[:, j] <= threshold
            nl = int(left.sum()); nr = n - nl
            if nl < min_leaf or nr < min_leaf: continue
            gain = parent - (nl/n)*entropy(y[left]) - (nr/n)*entropy(y[~left])
            si = split_info(nl, nr, n)
            if si > 0: candidates.append((gain/si, gain, name, float(threshold), left))
    if not candidates: return None
    avg_gain = float(np.mean([x[1] for x in candidates if x[1] > 0])) if any(x[1] > 0 for x in candidates) else 0
    eligible = [x for x in candidates if x[1] >= avg_gain - 1e-12]
    return max(eligible or candidates, key=lambda x: (x[0], x[1]))


# ------------------------------------------------------------
# Choose the majority class with the original tie-breaking order.
# ------------------------------------------------------------
def majority(y):
    counts = Counter(y)
    return sorted(counts, key=lambda c: (-counts[c], CLASSES.index(c)))[0]


# ------------------------------------------------------------
# Grow the original unpruned C4.5-style numeric decision tree.
# ------------------------------------------------------------
def train_c45(X, y, feature_names, max_depth=None, min_leaf=1, min_gain=1e-9):
    def grow(x, labels, depth):
        prediction = majority(labels)
        if len(set(labels)) == 1 or (max_depth is not None and depth >= max_depth) or len(labels) < 2*min_leaf:
            return {'leaf': prediction, 'n': int(len(labels))}
        split = best_c45_split(x, labels, feature_names, min_leaf)
        if split is None or split[1] <= min_gain:
            return {'leaf': prediction, 'n': int(len(labels))}
        ratio, gain, feature, threshold, mask = split
        j = feature_names.index(feature)
        return {'feature': feature, 'threshold': threshold, 'gain': gain,
                'gain_ratio': ratio, 'n': int(len(labels)),
                'left': grow(x[mask], labels[mask], depth+1),
                'right': grow(x[~mask], labels[~mask], depth+1)}
    return grow(np.asarray(X, float), np.asarray(y), 0)


# ------------------------------------------------------------
# Traverse the trained tree to obtain one classification.
# ------------------------------------------------------------
def predict_tree(tree, row):
    node = tree
    while 'leaf' not in node:
        node = node['left'] if float(row[node['feature']]) <= node['threshold'] else node['right']
    return node['leaf']


# ------------------------------------------------------------
# Format the trained tree for inspection.
# ------------------------------------------------------------
def tree_text(tree, indent=''):
    if 'leaf' in tree: return indent + f"Predict {tree['leaf']} (n={tree['n']})"
    s = indent + f"IF {tree['feature']} <= {tree['threshold']:.6f} [gain={tree['gain']:.4f}, gain_ratio={tree['gain_ratio']:.4f}]\n"
    s += tree_text(tree['left'], indent+'  ') + '\n'
    s += indent + f"ELSE ({tree['feature']} > {tree['threshold']:.6f})\n"
    return s + tree_text(tree['right'], indent+'  ')


# ------------------------------------------------------------
# Record the exact branches taken during validation.
# ------------------------------------------------------------
def prediction_trace(tree, row):
    """Record the exact numeric path taken by an unseen participant."""
    node = tree; path = []
    while 'leaf' not in node:
        feature = node['feature']; value = float(row[feature]); threshold = float(node['threshold'])
        branch = 'left' if value <= threshold else 'right'
        path.append({'feature': feature, 'value': value, 'threshold': threshold,
                     'branch': branch, 'gain_ratio': float(node['gain_ratio']),
                     'node_training_count': int(node['n'])})
        node = node[branch]
    return path, node['leaf']


# ============================================================
# PHASE 7 — COMPLETE TRAINING PIPELINE
# ============================================================

# ------------------------------------------------------------
# Fit labeling, Pearson diagnostics, ReliefF selection, tree and baseline class presets.
# ------------------------------------------------------------
def fit_pipeline(baseline, max_depth=None, min_leaf=1, pearson_enabled=True):
    thresholds = derive_thresholds(baseline)
    labels, time_labels, error_labels = apply_labels(baseline, thresholds)
    counts = Counter(labels)
    missing_classes = [c for c in CLASSES if counts[c] == 0]
    if missing_classes: raise ValueError(f'Cannot train three-class model; no baseline records labeled {missing_classes}.')
    if min(counts.values()) < 2: raise ValueError(f'Cannot train ReliefF; need >=2 records per class. Counts: {dict(counts)}')
    pearson = pearson_analysis(baseline[FEATURES].to_numpy(), FEATURES) if pearson_enabled else None
    relief = relief_f_weights(baseline[FEATURES].to_numpy(), labels, FEATURES, k=3)
    selected = choose_largest_gap(relief)
    if not selected: raise ValueError('ReliefF selection returned no features.')
    tree = train_c45(baseline[selected].to_numpy(), labels, selected, max_depth=max_depth, min_leaf=min_leaf)
    labelled = baseline.copy()
    labelled['completion_time_class'] = time_labels
    labelled['error_count_class'] = error_labels
    labelled['accessibility_level'] = labels
    presets = {}
    for cls in CLASSES:
        subset = labelled[labelled.accessibility_level == cls]
        presets[cls] = {f: float(subset[f].mean()) for f in FEATURES}
    return {'thresholds': thresholds, 'labels': labels, 'labelled': labelled,
            'pearson': pearson, 'relief': relief, 'selected': selected,
            'tree': tree, 'presets': presets, 'class_counts': dict(counts)}


# ============================================================
# PHASE 8 — CROSS-VALIDATION AND MODEL EVALUATION
# ============================================================

# ------------------------------------------------------------
# Choose a feasible fold count while retaining enough training examples per class.
# ------------------------------------------------------------
def cv_fold_count(class_counts, requested):
    # Keep at least two training records per class in every fold for ReliefF hits.
    for folds in range(min(requested, min(class_counts.values())), 1, -1):
        if all(n - int(np.ceil(n/folds)) >= 2 for n in class_counts.values()): return folds
    return 0


# ------------------------------------------------------------
# Compute the confusion matrix and per-class evaluation metrics.
# ------------------------------------------------------------
def score_predictions(actual, predicted):
    matrix = pd.DataFrame(0, index=CLASSES, columns=CLASSES)
    for a, p in zip(actual, predicted): matrix.loc[a, p] += 1
    metrics = {}
    for cls in CLASSES:
        tp = int(matrix.loc[cls, cls]); fp = int(matrix[cls].sum()-tp); fn = int(matrix.loc[cls].sum()-tp)
        precision = tp/(tp+fp) if tp+fp else 0.0
        recall = tp/(tp+fn) if tp+fn else 0.0
        f1 = 2*precision*recall/(precision+recall) if precision+recall else 0.0
        metrics[cls] = {'precision': precision, 'recall': recall, 'f1': f1, 'support': int(matrix.loc[cls].sum())}
    return matrix, metrics, float(np.mean([metrics[c]['f1'] for c in CLASSES]))


# ------------------------------------------------------------
# Build reproducible class-stratified validation partitions.
# ------------------------------------------------------------
def stratified_bins(baseline, requested_folds, seed):
    strata, _, _ = apply_labels(baseline, derive_thresholds(baseline))
    counts = Counter(strata)
    folds = cv_fold_count(counts, requested_folds)
    if folds < 2: return [], counts
    rng = np.random.default_rng(seed); bins = [[] for _ in range(folds)]
    for cls in CLASSES:
        indices = np.where(strata == cls)[0].tolist(); rng.shuffle(indices)
        for j, idx in enumerate(indices): bins[j % folds].append(idx)
    return bins, counts


# ------------------------------------------------------------
# Retain the fixed unpruned training method; no hyperparameter search.
# ------------------------------------------------------------
def tune_training_only(train, seed=42, requested_folds=3):
    """No tuning: unpruned tree with mathematical termination rules only."""
    return {'max_depth': None, 'min_leaf': 1, 'status': 'unpruned_fixed_method',
            'candidates': [], 'note': 'No pre-pruning depth or minimum-leaf tuning.'}


# ------------------------------------------------------------
# Refit the full pipeline inside each fold and evaluate held-out records.
# ------------------------------------------------------------
def cross_validate(baseline, requested_folds=5, seed=42, tune=True, pearson_enabled=True):
    # Global provisional labels are used ONLY to allocate stratified folds, not to train or score.
    # Fold labels, Pearson, ReliefF, feature selection and tree are refit on training data.
    # No hyperparameter tuning. Training-fold thresholds define labels for both train and held-out validation.
    bins, counts = stratified_bins(baseline, requested_folds, seed)
    if not bins:
        return {'note': f'Cross-validation unavailable: too few records per class. Counts={dict(counts)}.'}
    actual = []; predicted = []; records = []; fold_summaries = []
    for fold_number, test_indices in enumerate(bins, 1):
        test_set = set(test_indices)
        train = baseline.iloc[[i for i in range(len(baseline)) if i not in test_set]].reset_index(drop=True)
        valid_indices = sorted(test_indices)
        valid = baseline.iloc[valid_indices].reset_index(drop=True)
        tuning = tune_training_only(train, seed=seed+fold_number) if tune else {
            'max_depth': None, 'min_leaf': 1, 'status': 'unpruned_fixed_method', 'candidates': []}
        fitted = fit_pipeline(train, max_depth=tuning['max_depth'], min_leaf=tuning['min_leaf'], pearson_enabled=pearson_enabled)
        valid_labels, _, _ = apply_labels(valid, fitted['thresholds'])
        fold_summaries.append({'fold': fold_number, 'train_records': len(train),
            'validation_records': len(valid), 'selected_features': fitted['selected'],
            'class_counts': fitted['class_counts'], 'thresholds': fitted['thresholds'], 'label_definition': 'training_fold_quartiles',
            'tuning': tuning, 'tree': fitted['tree']})
        for j, (_, row) in enumerate(valid.iterrows()):
            trace, pred = prediction_trace(fitted['tree'], row.to_dict())
            actual.append(str(valid_labels[j])); predicted.append(pred)
            records.append({'record_number': int(valid_indices[j])+1, 'csv_row': int(valid_indices[j])+2,
                'fold': fold_number, 'actual': str(valid_labels[j]), 'predicted': pred,
                'correct': bool(str(valid_labels[j]) == pred),
                'completion_time': float(row['completion_time']), 'error_count': int(row['error_count']),
                'selected_features': '|'.join(fitted['selected']),
                'max_depth': tuning['max_depth'], 'min_leaf': tuning['min_leaf'],
                'decision_path_json': json.dumps(trace), 'label_thresholds_json': json.dumps(fitted['thresholds'])})
    matrix, metrics, macro_f1 = score_predictions(actual, predicted)
    accuracy = float(np.mean(np.asarray(actual) == np.asarray(predicted)))
    balanced_accuracy = float(np.mean([metrics[c]['recall'] for c in CLASSES]))
    stability = {f: int(sum(f in fold['selected_features'] for fold in fold_summaries)) for f in FEATURES}
    return {'folds': len(bins), 'accuracy': accuracy, 'confusion_matrix': matrix,
            'per_class': metrics, 'macro_f1': macro_f1, 'balanced_accuracy': balanced_accuracy,
            'record_predictions': records, 'fold_diagnostics': fold_summaries,
            'selection_frequency': stability}


# ============================================================
# PHASE 9 — SAVE MODEL AND TRAINING OUTPUTS
# ============================================================

# ------------------------------------------------------------
# Save the trained model, presets, selected features and provenance metadata.
# ------------------------------------------------------------
def save_model(model, baseline_path, output_dir):
    artifact = {
        'model_version': '4.5', 'algorithm': 'C4.5-style gain-ratio decision tree',
        'baseline_source': str(baseline_path), 'baseline_records': len(model['labelled']),
        'baseline_sha256': hashlib.sha256(Path(baseline_path).read_bytes()).hexdigest(),
        'features': FEATURES, 'selected_features': model['selected'],
        'thresholds': model['thresholds'], 'class_counts': model['class_counts'],
        'tree': model['tree'], 'presets': model['presets'],
        'pearson_cutoff': 0.80, 'pearson_mode': 'reporting_only_no_feature_removal_or_weight_adjustment',
        'tree_stopping': {'max_depth': None, 'min_leaf': 1, 'min_gain': 1e-9, 'post_pruning': False},
        'notes': ['Synthetic sample data must not be reported as actual participant observations.',
                  'Presets are baseline class means, not experimentally optimized settings.']
    }
    (output_dir/'trained_model.json').write_text(json.dumps(artifact, indent=2), encoding='utf-8')
    return artifact


# ------------------------------------------------------------
# Run the training workflow and export all diagnostics and evaluation files.
# ------------------------------------------------------------
def run_training(baseline_path, output_dir, cv_folds):
    raw = pd.read_csv(baseline_path)
    baseline = validate_frame(raw, 'Baseline', min_rows=6)
    print(f'Baseline records: {len(baseline)} (study target 30–45; no fixed 30-row cap)')
    if len(baseline) < 30 or len(baseline) > 45:
        print('NOTE: Baseline count is outside the planned 30–45 participant range; processing is still permitted.')
    duplicate_count = int(baseline.duplicated().sum())
    print(f'Exact duplicate records detected: {duplicate_count} (reported, not automatically removed)')
    tuning = tune_training_only(baseline, seed=42)
    model = fit_pipeline(baseline, max_depth=tuning['max_depth'], min_leaf=tuning['min_leaf'])
    output_dir.mkdir(parents=True, exist_ok=True)
    statistics_table = baseline_descriptive_statistics(baseline)
    print('\n' + '=' * 70)
    print('PHASE 2 — BASELINE DESCRIPTIVE STATISTICS (V3 RESTORED)')
    print('=' * 70)
    print(statistics_table.round(4).to_string())
    statistics_table.to_csv(output_dir/'Baseline_Descriptive_Statistics.csv')
    model['labelled'].to_csv(output_dir/'Baseline_With_Labels.csv', index=False)
    model['pearson'].to_csv(output_dir/'Pearson_Correlation_Results.csv', index=False)
    model['relief'].to_csv(output_dir/'ReliefF_Feature_Ranking.csv', index=False)
    pd.DataFrame({'Selected Feature': model['selected']}).to_csv(output_dir/'ReliefF_Selected_Features.csv', index=False)
    (output_dir/'C45_Tree.txt').write_text(tree_text(model['tree'])+'\n', encoding='utf-8')
    artifact = save_model(model, baseline_path, output_dir)
    artifact['training_only_tuning'] = tuning
    (output_dir/'trained_model.json').write_text(json.dumps(artifact, indent=2), encoding='utf-8')
    (output_dir/'Training_Only_Tuning.json').write_text(json.dumps(tuning, indent=2), encoding='utf-8')
    print('\nCLASS DISTRIBUTION:', model['class_counts'])
    print(f'Pearson pairs: {len(model["pearson"])}; flagged |r| >= 0.80: {int(model["pearson"]["Flag |r| >= threshold"].sum())}')
    print('ReliefF selected features:', model['selected'])
    print('\nTRAINED TREE\n'+tree_text(model['tree']))
    cv = cross_validate(baseline, cv_folds, tune=True)
    # Identical CV partitions and tuning, with Pearson switched off in V2.
    v2 = cross_validate(baseline, cv_folds, tune=True, pearson_enabled=False)
    if 'accuracy' in cv and 'accuracy' in v2:
        a = [(r['record_number'], r['actual'], r['predicted'], r['selected_features']) for r in cv['record_predictions']]
        b = [(r['record_number'], r['actual'], r['predicted'], r['selected_features']) for r in v2['record_predictions']]
        assert a == b, 'Reporting-only Pearson unexpectedly changed ReliefF/C4.5 outputs.'
        comparison = {'V2_ReliefF_C45': {'accuracy': v2['accuracy'], 'macro_f1': v2['macro_f1'], 'balanced_accuracy': v2['balanced_accuracy']}, 'V3_Pearson_Report_ReliefF_C45': {'accuracy': cv['accuracy'], 'macro_f1': cv['macro_f1'], 'balanced_accuracy': cv['balanced_accuracy']}, 'predictions_and_selected_features_identical': True, 'interpretation': 'Pearson provides correlation diagnostics only; no classification improvement is expected.'}
    else:
        comparison = {'status': 'comparison_unavailable', 'V2_note': v2.get('note'), 'V3_note': cv.get('note')}
    (output_dir/'V2_V3_Comparison.json').write_text(json.dumps(comparison, indent=2), encoding='utf-8')
    if 'accuracy' in cv:
        print(f'\n{cv["folds"]}-fold full-pipeline cross-validation accuracy: {cv["accuracy"]:.4f}')
        print('Confusion matrix (actual rows x predicted columns):\n', cv['confusion_matrix'])
        print('Per-class metrics:', json.dumps(cv['per_class'], indent=2))
        cv['confusion_matrix'].to_csv(output_dir/'Cross_Validation_Confusion_Matrix.csv')
        pd.DataFrame(cv['record_predictions']).to_csv(output_dir/'Cross_Validation_Record_Predictions.csv', index=False)
        pd.DataFrame([{'feature': f, 'folds_selected': n, 'total_folds': cv['folds'],
                       'selection_rate': n/cv['folds']} for f, n in cv['selection_frequency'].items()]
                     ).sort_values(['folds_selected','feature'], ascending=[False,True]).to_csv(
                         output_dir/'ReliefF_Selection_Stability.csv', index=False)
        (output_dir/'Cross_Validation_Fold_Trees.json').write_text(
            json.dumps(cv['fold_diagnostics'], indent=2), encoding='utf-8')
        print(f'Macro F1: {cv["macro_f1"]:.4f}; balanced accuracy: {cv["balanced_accuracy"]:.4f}')
        print('Incorrect validation predictions:')
        for record in cv['record_predictions']:
            if not record['correct']:
                print(f'  CSV row {record["csv_row"]}, fold {record["fold"]}: '
                      f'{record["actual"]} -> {record["predicted"]}')
    else:
        print('\nCROSS-VALIDATION NOTE:', cv['note'])
    summary = {'baseline_records': len(baseline), 'exact_duplicate_rows': duplicate_count,
               'thresholds': model['thresholds'], 'class_distribution': model['class_counts'],
               'selected_features': model['selected'], 'tree': model['tree'], 'training_only_tuning': tuning,
               'cross_validation': {'folds': cv.get('folds'), 'accuracy': cv.get('accuracy'),
                                    'confusion_matrix': cv.get('confusion_matrix', pd.DataFrame()).to_dict() if 'accuracy' in cv else None,
                                    'per_class': cv.get('per_class'), 'macro_f1': cv.get('macro_f1'),
                                    'balanced_accuracy': cv.get('balanced_accuracy'),
                                    'selection_frequency': cv.get('selection_frequency'), 'note': cv.get('note'), 'evaluation_type': 'outer_stratified_cross_validation_with_training_fold_thresholds'},
               'labeling_note': 'Each outer fold derives quartiles from its training partition only and applies them to its validation partition. Thus target categories can vary by fold; pooled CV metrics measure fold-relative performance labels. Global labels are used for fold stratification only.',
               'warning': 'Verify provenance of input; supplied example files are synthetic demonstration data, not actual participant results.'}
    (output_dir/'Model_Summary.json').write_text(json.dumps(summary, indent=2), encoding='utf-8')
    return model


# ============================================================
# PHASE 10 — PLAYER INFERENCE AND UNITY PRESET
# ============================================================

# ------------------------------------------------------------
# Select one CSV record, predict its class and export all 12 Unity-facing settings.
# ------------------------------------------------------------
def run_inference(player_path, model_path, output_dir):
    artifact = json.loads(Path(model_path).read_text(encoding='utf-8'))
    # One player per device; each CSV row is a gameplay attempt.
    # Preserve V3 behavior: always select only the first data row.
    raw = pd.read_csv(player_path, nrows=1)
    if raw.empty:
        raise ValueError("Player CSV contains no gameplay attempt records.")
    # Only visual settings are needed for inference; gameplay outcomes may be
    # absent or unfinished at prediction time. Do not modify the player file.
    missing = [c for c in FEATURES if c not in raw.columns]
    if missing: raise ValueError(f'Player: missing visual features: {missing}')
    player = raw[FEATURES].copy()
    for c in FEATURES: player[c] = pd.to_numeric(player[c], errors='coerce')
    if not np.isfinite(player.to_numpy(float)).all() or ((player < 0) | (player > 1)).any().any():
        raise ValueError('Player: visual settings must be finite normalized numbers in [0,1].')
    row = player.iloc[0]
    selected = artifact['selected_features']
    prediction = predict_tree(artifact['tree'], row[selected].to_dict())
    decision_path, traced_prediction = prediction_trace(artifact['tree'], row[selected].to_dict())
    if traced_prediction != prediction:
        raise RuntimeError('Prediction and decision trace disagree.')
    preset = artifact['presets'].get(prediction)
    if not preset: raise ValueError(f'No trained baseline preset exists for predicted class {prediction}.')
    missing_preset = [f for f in FEATURES if f not in preset]
    if missing_preset: raise ValueError(f'Model is missing preset values: {missing_preset}')
    # All baseline preset values are normalized to [0,1]. Convert to the
    # historical V4.5 Unity-facing scales (color 0..360; brightness/contrast 0..2).
    # These conversion assumptions MUST be verified against the Unity slider ranges.
    settings = {}
    for feature in FEATURES:
        factor = 360.0 if feature.endswith('_color') else 2.0
        settings[feature] = round(float(preset[feature]) * factor, 6)
    payload = {'predicted_accessibility_level': prediction, 'recommended_settings': settings,
               'selected_features': selected, 'player_attempt_index': 0,
               'model_version': artifact.get('model_version'),
               'baseline_records_in_model': artifact['baseline_records']}
    output_dir.mkdir(parents=True, exist_ok=True)
    # Save a human-readable, auditable explanation separately from the
    # existing Unity payload, keeping AI_RESULT.json backwards compatible.
    explanation = {
        'player_attempt_index': 0,
        'predicted_accessibility_level': prediction,
        'selected_features': selected,
        'decision_path': [
            {**step, 'comparison': '<=' if step['branch'] == 'left' else '>',
             'condition_satisfied': f"{step['feature']} ({step['value']:.6f}) "
                                    f"{'<=' if step['branch'] == 'left' else '>'} "
                                    f"{step['threshold']:.6f}"}
            for step in decision_path
        ],
        'terminal_leaf_class': traced_prediction,
        'preset_source': 'Baseline training records assigned to the predicted class',
        'preset_selection_reason': (
            f'The trained decision tree classified the selected player as {prediction}; '
            f'the corresponding {prediction} baseline class-mean preset was selected.'
        ),
        'normalized_baseline_class_mean_preset': preset,
        'unity_scaled_recommended_settings': settings,
        'caution': 'The trace explains model behavior; it does not establish that the preset is optimal.'
    }
    (output_dir/'Player_Prediction_Explanation.json').write_text(
        json.dumps(explanation, indent=2), encoding='utf-8')
    print('\nPLAYER DECISION PATH:')
    for step in explanation['decision_path']:
        print('  ' + step['condition_satisfied'])
    print('PRESET REASON:', explanation['preset_selection_reason'])
    (output_dir/'AI_RESULT.json').write_text(json.dumps(payload, indent=2), encoding='utf-8')
    print('PLAYER PREDICTION:', prediction)
    print('AI_RESULT:', json.dumps(payload, indent=2))
    return payload


# ============================================================
# PHASE 11 — COMMAND-LINE PIPELINE EXECUTION
# ============================================================

# ------------------------------------------------------------
# Parse the existing CLI arguments and run training and/or inference.
# ------------------------------------------------------------
def main():
    parser = argparse.ArgumentParser(description='Pearson + ReliefF + C4.5-style accessibility pipeline')
    parser.add_argument('--mode', choices=['all','train','infer'], default='all',
                        help='all=train then infer; train=build model only; infer=load saved model only')
    parser.add_argument('--baseline', default=None, help='Path to any baseline CSV file (required for all/train)')
    parser.add_argument('--player', default=None, help='Path to any player CSV file (required for all/infer)')
    parser.add_argument('--model', default='pipeline_outputs/trained_model.json')
    parser.add_argument('--output-dir', default='pipeline_outputs')
    parser.add_argument('--cv-folds', type=int, default=5)
    args = parser.parse_args()
    # Accept any CSV filename or directory; require only the paths needed by the selected mode.
    if args.mode in ('all', 'train') and not args.baseline:
        parser.error('--baseline is required for --mode all or train')
    if args.mode in ('all', 'infer') and not args.player:
        parser.error('--player is required for --mode all or infer')
    output_dir = Path(args.output_dir)
    model_path = Path(args.model)
    if args.mode in ('all','train'):
        run_training(args.baseline, output_dir, args.cv_folds)
        model_path = output_dir/'trained_model.json'
    if args.mode in ('all','infer'):
        run_inference(args.player, model_path, output_dir)
    print(f'\nOutputs saved to: {output_dir.resolve()}')


if __name__ == '__main__':
    main()
