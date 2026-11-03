#!/usr/bin/env python3

"""
ADAPTIVE VISUAL ACCESSIBILITY CLASSIFICATION SYSTEM

PIPELINE V2
ReliefF + C4.5 Decision Tree

THESIS:
Enhancing ReliefF and C4.5 Decision Tree-Based
Adaptive Visual Accessibility in Precision Platformer
Games (Pipeline V2: no Pearson preprocessing)

WORKFLOW:
1. Baseline CSV validation
2. Variable identification
3. Descriptive statistics
4. Performance-based accessibility labeling
5. ReliefF feature selection
6. C4.5-style decision tree classification
7. Cross-validation and evaluation
8. Model training and export
9. Player inference
10. AI_RESULT.json generation

IMPORTANT:
All 12 visual features go directly into ReliefF.
No correlation-based filtering is applied.

The example dataset is synthetic and must not be
reported as actual participant data.
"""

# ============================================================
# PHASE 0 — IMPORT LIBRARIES
# ============================================================

from __future__ import annotations

import argparse
import json
import hashlib

from pathlib import Path
from collections import Counter

import numpy as np
import pandas as pd


# ============================================================
# PHASE 1 — VARIABLE IDENTIFICATION
# ============================================================

# Twelve normalized visual accessibility features.

FEATURES = [
    "background_color",
    "background_contrast",
    "background_brightness",

    "obstacle_color",
    "obstacle_contrast",
    "obstacle_brightness",

    "platform_color",
    "platform_contrast",
    "platform_brightness",

    "player_color",
    "player_contrast",
    "player_brightness"
]

# Gameplay performance indicators.

PERFORMANCE = [
    "completion_time",
    "error_count"
]

# Performance-based classification categories.

CLASSES = [
    "LOW",
    "MEDIUM",
    "HIGH"
]

# Required baseline CSV columns.

REQUIRED = FEATURES + PERFORMANCE


# ============================================================
# PHASE 2 — BASELINE CSV VALIDATION
# ============================================================

def validate_frame(frame, name, min_rows=1):
    """
    Validate the baseline dataset.

    Requirements:
    - All 12 visual features must exist.
    - Completion time and error count must exist.
    - Visual features must be normalized to [0,1].
    - Completion time must be positive.
    - Error count must be a nonnegative integer.
    - Missing and infinite values are not permitted.
    """

    missing = [
        column
        for column in REQUIRED
        if column not in frame.columns
    ]

    if missing:
        raise ValueError(
            f"{name}: missing required columns: {missing}"
        )

    if len(frame) < min_rows:
        raise ValueError(
            f"{name}: requires at least {min_rows} "
            f"records; found {len(frame)}."
        )

    out = frame[REQUIRED].copy()

    for column in REQUIRED:
        out[column] = pd.to_numeric(
            out[column],
            errors="coerce"
        )

    if out.isna().any().any():

        bad_columns = out.columns[
            out.isna().any()
        ].tolist()

        raise ValueError(
            f"{name}: missing or non-numeric values "
            f"in {bad_columns}."
        )

    if not np.isfinite(
        out.to_numpy(dtype=float)
    ).all():

        raise ValueError(
            f"{name}: contains infinite values."
        )

    for column in FEATURES:

        if (
            (out[column] < 0) |
            (out[column] > 1)
        ).any():

            raise ValueError(
                f"{name}: {column} must be "
                "normalized to [0,1]."
            )

    if (out["completion_time"] <= 0).any():

        raise ValueError(
            f"{name}: completion_time must be > 0."
        )

    errors = out["error_count"]

    if (
        (errors < 0).any()
        or not np.allclose(
            errors,
            np.round(errors)
        )
    ):

        raise ValueError(
            f"{name}: error_count must be "
            "a nonnegative integer."
        )

    return out.reset_index(drop=True)


# ============================================================
# PHASE 3 — DESCRIPTIVE STATISTICS
# ============================================================

def derive_thresholds(df):
    """
    Calculate Q1 and Q3 for completion time
    and error count.

    These thresholds must be calculated using
    training data only.
    """

    return {
        "completion_q1": float(
            df["completion_time"].quantile(0.25)
        ),

        "completion_q3": float(
            df["completion_time"].quantile(0.75)
        ),

        "errors_q1": float(
            df["error_count"].quantile(0.25)
        ),

        "errors_q3": float(
            df["error_count"].quantile(0.75)
        )
    }


def descriptive_statistics(values):
    """
    Calculate descriptive statistics for
    one gameplay performance indicator.
    """

    values = np.asarray(
        values,
        dtype=float
    )

    return {
        "N": len(values),

        "Mean": float(
            np.mean(values)
        ),

        "Median": float(
            np.median(values)
        ),

        "Sample SD": float(
            np.std(values, ddof=1)
        ),

        "Minimum": float(
            np.min(values)
        ),

        "Q1 (25%)": float(
            np.percentile(values, 25)
        ),

        "Q3 (75%)": float(
            np.percentile(values, 75)
        ),

        "Maximum": float(
            np.max(values)
        )
    }


def baseline_descriptive_statistics(baseline):
    """
    Generate descriptive statistics for
    completion time and error count.

    Player inference records are excluded.
    """

    statistics = [
        descriptive_statistics(
            baseline[column].to_numpy(dtype=float)
        )
        for column in PERFORMANCE
    ]

    return pd.DataFrame(
        statistics,
        index=[
            "Completion Time",
            "Error Count"
        ]
    ).rename_axis("Gameplay Indicator")


# ============================================================
# PHASE 4 — RULE-BASED ACCESSIBILITY LABELING
# ============================================================

def apply_labels(df, th):
    """
    Assign LOW, MEDIUM, or HIGH based on
    completion time and error count.

    LOW:
        Both completion time and errors
        satisfy their LOW conditions.

    HIGH:
        Both completion time and errors
        satisfy their HIGH conditions.

    MEDIUM:
        All other combinations.

    These are operational performance classes,
    not clinical visual impairment diagnoses.
    """

    time_class = np.where(
        df["completion_time"] < th["completion_q1"],
        "LOW",

        np.where(
            df["completion_time"] <= th["completion_q3"],
            "MEDIUM",
            "HIGH"
        )
    )

    error_class = np.where(
        df["error_count"] <= th["errors_q1"],
        "LOW",

        np.where(
            df["error_count"] <= th["errors_q3"],
            "MEDIUM",
            "HIGH"
        )
    )

    combined = []

    for time_label, error_label in zip(
        time_class,
        error_class
    ):

        if (
            time_label == "LOW"
            and error_label == "LOW"
        ):

            combined.append("LOW")

        elif (
            time_label == "HIGH"
            and error_label == "HIGH"
        ):

            combined.append("HIGH")

        else:

            combined.append("MEDIUM")

    return (
        np.asarray(combined),
        time_class,
        error_class
    )


# ============================================================
# PHASE 5 — RELIEFF FEATURE SELECTION
# ============================================================

def relief_f_weights(X, labels, names, k=3):
    """
    Calculate ReliefF-style feature weights.

    All 12 visual features are evaluated; no
    correlation-based pre-filtering is applied.

    ReliefF evaluates feature relevance using:
    - Nearest hits (same-class neighbors)
    - Nearest misses (different-class neighbors)
    - Class-prior weighting

    All visual features are normalized to [0,1].
    """

    X = np.asarray(X, dtype=float)
    labels = np.asarray(labels)

    n_samples, n_features = X.shape

    counts = Counter(labels)

    if len(counts) < 2:
        raise ValueError(
            "ReliefF requires at least two classes."
        )

    if min(counts.values()) < 2:
        raise ValueError(
            "ReliefF requires at least two "
            "records per class."
        )

    priors = {
        cls: count / n_samples
        for cls, count in counts.items()
    }

    weights = np.zeros(
        n_features,
        dtype=float
    )

    # Manhattan distance between normalized records.
    distances = np.sum(
        np.abs(
            X[:, None, :] - X[None, :, :]
        ),
        axis=2
    )

    for i in range(n_samples):

        current_class = labels[i]

        # Find nearest hits.
        hit_indices = np.where(
            labels == current_class
        )[0]

        hit_indices = hit_indices[
            hit_indices != i
        ]

        hit_indices = hit_indices[
            np.argsort(
                distances[i, hit_indices],
                kind="stable"
            )
        ]

        nearest_hits = hit_indices[:k]

        hit_difference = np.mean(
            np.abs(
                X[nearest_hits] - X[i]
            ),
            axis=0
        )

        weights -= (
            hit_difference / n_samples
        )

        # Find nearest misses from every other class.
        for other_class in counts:

            if other_class == current_class:
                continue

            miss_indices = np.where(
                labels == other_class
            )[0]

            miss_indices = miss_indices[
                np.argsort(
                    distances[i, miss_indices],
                    kind="stable"
                )
            ]

            nearest_misses = miss_indices[:k]

            miss_difference = np.mean(
                np.abs(
                    X[nearest_misses] - X[i]
                ),
                axis=0
            )

            prior_weight = (
                priors[other_class]
                / (1.0 - priors[current_class])
            )

            weights += (
                prior_weight
                * miss_difference
                / n_samples
            )

    ranking = pd.DataFrame({
        "Feature": names,
        "ReliefF Weight": weights
    })

    ranking = ranking.sort_values(
        by=["ReliefF Weight", "Feature"],
        ascending=[False, True],
        kind="stable"
    ).reset_index(drop=True)

    return ranking


def choose_largest_gap(ranking):
    """
    Select features using the largest-gap method.

    Features are ranked by ReliefF weight.
    The largest drop between consecutive weights
    determines the selection cutoff.

    If only one feature is available, retain it.
    """

    if ranking.empty:
        raise ValueError(
            "ReliefF ranking is empty."
        )

    features = ranking[
        "Feature"
    ].tolist()

    weights = ranking[
        "ReliefF Weight"
    ].to_numpy(dtype=float)

    if len(features) == 1:
        return features

    gaps = (
        weights[:-1] - weights[1:]
    )

    largest_gap_index = int(
        np.argmax(gaps)
    )

    selected_count = (
        largest_gap_index + 1
    )

    return features[:selected_count]


# ============================================================
# PHASE 6 — C4.5-STYLE DECISION TREE
# ============================================================

def entropy(labels):
    """
    Calculate Shannon entropy.
    """

    labels = np.asarray(labels)

    if len(labels) == 0:
        return 0.0

    counts = Counter(labels)

    probabilities = np.asarray(
        [
            count / len(labels)
            for count in counts.values()
        ],
        dtype=float
    )

    return float(
        -np.sum(
            probabilities
            * np.log2(probabilities)
        )
    )


def majority_class(labels):
    """
    Return the majority class.

    Resolve ties using the predefined
    LOW, MEDIUM, HIGH class ordering.
    """

    counts = Counter(labels)

    return max(
        CLASSES,
        key=lambda cls: counts.get(cls, 0)
    )


def best_gain_ratio_split(
    X,
    labels,
    min_leaf=1
):
    """
    Find the best numeric split using
    the C4.5 gain-ratio criterion.

    Candidate thresholds are midpoints
    between consecutive distinct values.

    A split is accepted only when both
    child nodes meet the minimum leaf size.
    """

    X = np.asarray(X, dtype=float)
    labels = np.asarray(labels)

    n_samples, n_features = X.shape

    parent_entropy = entropy(labels)

    best = None
    best_ratio = -np.inf

    epsilon = 1e-12

    for feature_index in range(n_features):

        values = X[:, feature_index]

        unique_values = np.unique(values)

        if len(unique_values) < 2:
            continue

        thresholds = (
            unique_values[:-1]
            + unique_values[1:]
        ) / 2.0

        for threshold in thresholds:

            left_mask = (
                values <= threshold
            )

            right_mask = ~left_mask

            n_left = int(
                np.sum(left_mask)
            )

            n_right = int(
                np.sum(right_mask)
            )

            if (
                n_left < min_leaf
                or n_right < min_leaf
            ):
                continue

            p_left = n_left / n_samples
            p_right = n_right / n_samples

            weighted_entropy = (
                p_left
                * entropy(labels[left_mask])
                +
                p_right
                * entropy(labels[right_mask])
            )

            information_gain = (
                parent_entropy
                - weighted_entropy
            )

            split_information = (
                -p_left * np.log2(p_left)
                -p_right * np.log2(p_right)
            )

            if split_information <= epsilon:
                continue

            gain_ratio = (
                information_gain
                / split_information
            )

            if (
                information_gain > epsilon
                and gain_ratio > best_ratio + epsilon
            ):

                best_ratio = gain_ratio

                best = {
                    "feature_index": feature_index,
                    "threshold": float(threshold),
                    "gain_ratio": float(gain_ratio),
                    "information_gain": float(
                        information_gain
                    ),
                    "left_mask": left_mask,
                    "right_mask": right_mask
                }

    return best


def train_c45(
    X,
    labels,
    feature_names,
    max_depth=None,
    min_leaf=1,
    depth=0
):
    """
    Train an unpruned C4.5-style
    numeric decision tree.

    Uses gain-ratio splitting.

    Stops when:
    - The node contains one class.
    - No valid positive-gain split exists.
    - The optional maximum depth is reached.

    No post-pruning is performed.
    """

    X = np.asarray(
        X,
        dtype=float
    )

    labels = np.asarray(labels)

    prediction = majority_class(labels)

    counts = Counter(labels)

    node = {
        "prediction": prediction,
        "samples": int(len(labels)),
        "class_counts": {
            cls: int(counts.get(cls, 0))
            for cls in CLASSES
        }
    }

    if len(set(labels)) == 1:
        node["type"] = "leaf"
        return node

    if (
        max_depth is not None
        and depth >= max_depth
    ):
        node["type"] = "leaf"
        return node

    split = best_gain_ratio_split(
        X,
        labels,
        min_leaf=min_leaf
    )

    if split is None:
        node["type"] = "leaf"
        return node

    feature_index = split[
        "feature_index"
    ]

    left_mask = split[
        "left_mask"
    ]

    right_mask = split[
        "right_mask"
    ]

    node.update({
        "type": "split",
        "feature": feature_names[
            feature_index
        ],
        "threshold": split["threshold"],
        "gain_ratio": split["gain_ratio"],
        "information_gain": split[
            "information_gain"
        ]
    })

    node["left"] = train_c45(
        X[left_mask],
        labels[left_mask],
        feature_names,
        max_depth=max_depth,
        min_leaf=min_leaf,
        depth=depth + 1
    )

    node["right"] = train_c45(
        X[right_mask],
        labels[right_mask],
        feature_names,
        max_depth=max_depth,
        min_leaf=min_leaf,
        depth=depth + 1
    )

    return node


def predict_tree(tree, record):
    """
    Predict a player's class using
    the trained decision tree.
    """

    node = tree

    while node["type"] == "split":

        feature = node["feature"]
        threshold = node["threshold"]

        value = float(
            record[feature]
        )

        if value <= threshold:
            node = node["left"]

        else:
            node = node["right"]

    return node["prediction"]


def prediction_trace(tree, record):
    """
    Record each decision made by the tree.

    Returns:
    - Decision path
    - Final predicted class
    """

    node = tree
    path = []

    while node["type"] == "split":

        feature = node["feature"]
        threshold = float(
            node["threshold"]
        )

        value = float(
            record[feature]
        )

        if value <= threshold:
            branch = "left"
            node = node["left"]

        else:
            branch = "right"
            node = node["right"]

        path.append({
            "feature": feature,
            "value": value,
            "threshold": threshold,
            "branch": branch
        })

    return path, node["prediction"]


def tree_text(tree, indent=0):
    """
    Convert the decision tree into
    a readable text representation.
    """

    spacing = "  " * indent

    if tree["type"] == "leaf":

        return (
            f"{spacing}PREDICT "
            f"{tree['prediction']} "
            f"(n={tree['samples']})"
        )

    feature = tree["feature"]
    threshold = tree["threshold"]

    lines = [
        f"{spacing}IF {feature} <= {threshold:.6f}:",
        tree_text(
            tree["left"],
            indent + 1
        ),
        f"{spacing}ELSE ({feature} > {threshold:.6f}):",
        tree_text(
            tree["right"],
            indent + 1
        )
    ]

    return "\n".join(lines)


# ============================================================
# COMPLETE MODEL FITTING
# RELIEFF -> C4.5
# ============================================================

def fit_pipeline(
    baseline,
    max_depth=None,
    min_leaf=1
):
    """
    Train the V2 pipeline:

        ReliefF -> C4.5
    """

    # --------------------------------------------------------
    # STEP 1 — DERIVE TRAINING THRESHOLDS
    # --------------------------------------------------------

    thresholds = derive_thresholds(
        baseline
    )

    labels, time_labels, error_labels = apply_labels(
        baseline,
        thresholds
    )

    counts = Counter(labels)

    missing_classes = [
        cls
        for cls in CLASSES
        if counts[cls] == 0
    ]

    if missing_classes:

        raise ValueError(
            "Cannot train three-class model; "
            f"missing classes: {missing_classes}"
        )

    if min(counts.values()) < 2:

        raise ValueError(
            "ReliefF requires at least two "
            "training records per class. "
            f"Counts: {dict(counts)}"
        )

    # --------------------------------------------------------
    # STEP 2 — RELIEFF FEATURE SELECTION
    # --------------------------------------------------------

    relief = relief_f_weights(
        baseline[
            FEATURES
        ].to_numpy(),
        labels,
        FEATURES,
        k=3
    )

    selected = choose_largest_gap(
        relief
    )

    if not selected:

        raise ValueError(
            "ReliefF returned no selected features."
        )

    # --------------------------------------------------------
    # STEP 3 — C4.5 CLASSIFICATION
    # --------------------------------------------------------

    tree = train_c45(
        baseline[
            selected
        ].to_numpy(),
        labels,
        selected,
        max_depth=max_depth,
        min_leaf=min_leaf
    )

    # --------------------------------------------------------
    # STEP 4 — LABELED BASELINE
    # --------------------------------------------------------

    labelled = baseline.copy()

    labelled[
        "completion_time_class"
    ] = time_labels

    labelled[
        "error_count_class"
    ] = error_labels

    labelled[
        "accessibility_level"
    ] = labels

    # --------------------------------------------------------
    # STEP 5 — UNITY VISUAL PRESETS
    # --------------------------------------------------------

    # Presets are class-mean visual settings
    # calculated from training baseline records.
    #
    # Preserve all 12 settings even when the
    # classifier uses fewer selected features.

    presets = {}

    for cls in CLASSES:

        subset = labelled[
            labelled["accessibility_level"] == cls
        ]

        presets[cls] = {
            feature: float(
                subset[feature].mean()
            )
            for feature in FEATURES
        }

    # --------------------------------------------------------
    # STEP 6 — RETURN TRAINED MODEL
    # --------------------------------------------------------

    return {
        "thresholds": thresholds,
        "labels": labels,
        "labelled": labelled,
        "relief": relief,
        "selected": selected,
        "tree": tree,
        "presets": presets,
        "class_counts": dict(counts)
    }

# ============================================================
# PHASE 7 — CROSS-VALIDATION AND MODEL EVALUATION
# ============================================================

def cv_fold_count(counts, requested_folds):
    """
    Determine the number of stratified folds.

    The number of folds cannot exceed the
    smallest class count.

    At least two folds are required.
    """

    minimum_class_count = min(counts.values())

    folds = min(
        int(requested_folds),
        int(minimum_class_count)
    )

    return folds if folds >= 2 else 0


def score_predictions(actual, predicted):
    """
    Calculate the confusion matrix and
    per-class precision, recall, and F1.

    Rows represent actual classes.
    Columns represent predicted classes.
    """

    matrix = pd.DataFrame(
        0,
        index=CLASSES,
        columns=CLASSES,
        dtype=int
    )

    for true_label, predicted_label in zip(
        actual,
        predicted
    ):
        matrix.loc[
            true_label,
            predicted_label
        ] += 1

    metrics = {}

    for cls in CLASSES:

        tp = int(
            matrix.loc[cls, cls]
        )

        fp = int(
            matrix[cls].sum() - tp
        )

        fn = int(
            matrix.loc[cls].sum() - tp
        )

        precision = (
            tp / (tp + fp)
            if (tp + fp) > 0
            else 0.0
        )

        recall = (
            tp / (tp + fn)
            if (tp + fn) > 0
            else 0.0
        )

        f1 = (
            2 * precision * recall
            / (precision + recall)
            if (precision + recall) > 0
            else 0.0
        )

        metrics[cls] = {
            "precision": float(precision),
            "recall": float(recall),
            "f1": float(f1),
            "support": int(
                matrix.loc[cls].sum()
            )
        }

    macro_f1 = float(
        np.mean([
            metrics[cls]["f1"]
            for cls in CLASSES
        ])
    )

    return matrix, metrics, macro_f1


def stratified_bins(
    baseline,
    requested_folds,
    seed
):
    """
    Create reproducible stratified folds.

    Provisional full-baseline labels are used
    only for fold allocation.

    Training-fold thresholds are recomputed
    independently during cross-validation.
    """

    strata, _, _ = apply_labels(
        baseline,
        derive_thresholds(baseline)
    )

    counts = Counter(strata)

    folds = cv_fold_count(
        counts,
        requested_folds
    )

    if folds < 2:
        return [], counts

    rng = np.random.default_rng(seed)

    bins = [
        []
        for _ in range(folds)
    ]

    for cls in CLASSES:

        indices = np.where(
            strata == cls
        )[0].tolist()

        rng.shuffle(indices)

        for index, record_index in enumerate(indices):

            fold_index = index % folds

            bins[fold_index].append(
                record_index
            )

    return bins, counts


def tune_training_only(
    train,
    seed=42,
    requested_folds=3
):
    """
    Preserve the fixed unpruned tree method.

    No hyperparameter optimization is applied.

    This keeps the training configuration
    consistent across folds.
    """

    return {
        "max_depth": None,
        "min_leaf": 1,
        "status": "unpruned_fixed_method",
        "candidates": [],
        "note": (
            "No pre-pruning depth or "
            "minimum-leaf tuning."
        )
    }


def cross_validate(
    baseline,
    requested_folds=5,
    seed=42,
    tune=True
):
    """
    Evaluate the complete training pipeline.

    Every training fold independently fits:

    1. Performance-label thresholds
    2. ReliefF feature selection
    3. C4.5-style decision tree

    Validation records do not participate
    in fitting ReliefF or C4.5.
    """

    bins, counts = stratified_bins(
        baseline,
        requested_folds,
        seed
    )

    if not bins:

        return {
            "note": (
                "Cross-validation unavailable: "
                "too few records per class. "
                f"Counts={dict(counts)}."
            )
        }

    actual = []
    predicted = []

    records = []
    fold_summaries = []

    for fold_number, test_indices in enumerate(
        bins,
        start=1
    ):

        test_set = set(test_indices)

        # Build the training partition.

        train_indices = [
            i
            for i in range(len(baseline))
            if i not in test_set
        ]

        train = baseline.iloc[
            train_indices
        ].reset_index(drop=True)

        # Build the validation partition.

        valid_indices = sorted(
            test_indices
        )

        valid = baseline.iloc[
            valid_indices
        ].reset_index(drop=True)

        # Preserve fixed training parameters.

        tuning = tune_training_only(
            train,
            seed=seed + fold_number
        )

        # Fit the complete pipeline
        # using training records only.

        fitted = fit_pipeline(
            train,
            max_depth=tuning["max_depth"],
            min_leaf=tuning["min_leaf"]
        )

        # Apply training-fold thresholds
        # to validation performance records.

        valid_labels, _, _ = apply_labels(
            valid,
            fitted["thresholds"]
        )

        fold_summaries.append({
            "fold": fold_number,
            "train_records": len(train),
            "validation_records": len(valid),
            "selected_features": fitted["selected"],
            "class_counts": fitted["class_counts"],
            "thresholds": fitted["thresholds"],
            "label_definition": (
                "training_fold_quartiles"
            ),
            "tuning": tuning,
            "tree": fitted["tree"]
        })

        # Predict each held-out validation record.

        for j, (_, row) in enumerate(
            valid.iterrows()
        ):

            decision_path, pred = prediction_trace(
                fitted["tree"],
                row.to_dict()
            )

            true_label = str(
                valid_labels[j]
            )

            actual.append(
                true_label
            )

            predicted.append(
                pred
            )

            records.append({
                "record_number": int(
                    valid_indices[j]
                ) + 1,

                "csv_row": int(
                    valid_indices[j]
                ) + 2,

                "fold": fold_number,

                "actual": true_label,

                "predicted": pred,

                "correct": bool(
                    true_label == pred
                ),

                "completion_time": float(
                    row["completion_time"]
                ),

                "error_count": int(
                    row["error_count"]
                ),

                "selected_features": (
                    "|".join(
                        fitted["selected"]
                    )
                ),

                "max_depth": tuning["max_depth"],

                "min_leaf": tuning["min_leaf"],

                "decision_path_json": json.dumps(
                    decision_path
                ),

                "label_thresholds_json": json.dumps(
                    fitted["thresholds"]
                )
            })

    # --------------------------------------------------------
    # COMPUTE CLASSIFICATION METRICS
    # --------------------------------------------------------

    matrix, metrics, macro_f1 = score_predictions(
        actual,
        predicted
    )

    accuracy = float(
        np.mean(
            np.asarray(actual)
            == np.asarray(predicted)
        )
    )

    balanced_accuracy = float(
        np.mean([
            metrics[cls]["recall"]
            for cls in CLASSES
        ])
    )

    # --------------------------------------------------------
    # FEATURE-SELECTION STABILITY
    # --------------------------------------------------------

    stability = {
        feature: int(
            sum(
                feature in fold["selected_features"]
                for fold in fold_summaries
            )
        )
        for feature in FEATURES
    }

    return {
        "folds": len(bins),
        "accuracy": accuracy,
        "confusion_matrix": matrix,
        "per_class": metrics,
        "macro_f1": macro_f1,
        "balanced_accuracy": balanced_accuracy,
        "record_predictions": records,
        "fold_diagnostics": fold_summaries,
        "selection_frequency": stability
    }


# ============================================================
# PHASE 8 — MODEL TRAINING AND OUTPUT GENERATION
# ============================================================

def save_model(
    model,
    baseline_path,
    output_dir
):
    """
    Save the trained model and its metadata.

    Includes:
    - ReliefF-selected features
    - C4.5 decision tree
    - Classification thresholds
    - Unity visual presets
    - Dataset provenance
    """

    artifact = {
        "model_version": "V2_ReliefF_C45",

        "algorithm": (
            "C4.5-style gain-ratio decision tree"
        ),

        "baseline_source": str(
            baseline_path
        ),

        "baseline_records": len(
            model["labelled"]
        ),

        "baseline_sha256": hashlib.sha256(
            Path(baseline_path).read_bytes()
        ).hexdigest(),

        "features": FEATURES,

        "selected_features": model["selected"],

        "thresholds": model["thresholds"],

        "class_counts": model["class_counts"],

        "tree": model["tree"],

        "presets": model["presets"],

        "tree_stopping": {
            "max_depth": None,
            "min_leaf": 1,
            "min_gain": 1e-9,
            "post_pruning": False
        },

        "notes": [
            (
                "Synthetic sample data must not "
                "be reported as actual participant "
                "observations."
            ),
            (
                "Presets are baseline class means, "
                "not experimentally optimized settings."
            )
        ]
    }

    model_path = (
        output_dir / "trained_model.json"
    )

    model_path.write_text(
        json.dumps(
            artifact,
            indent=2
        ),
        encoding="utf-8"
    )

    return artifact


def run_training(
    baseline_path,
    output_dir,
    cv_folds
):
    """
    Run the complete V2 training workflow.

    Export reports and trained model files.
    """

    # --------------------------------------------------------
    # STEP 1 — LOAD AND VALIDATE BASELINE
    # --------------------------------------------------------

    raw = pd.read_csv(
        baseline_path
    )

    baseline = validate_frame(
        raw,
        "Baseline",
        min_rows=6
    )

    print(
        f"Baseline records: {len(baseline)}"
    )

    if (
        len(baseline) < 30
        or len(baseline) > 45
    ):

        print(
            "NOTE: Baseline count is outside "
            "the planned 30-45 participant range. "
            "Processing is still permitted."
        )

    duplicate_count = int(
        baseline.duplicated().sum()
    )

    print(
        "Exact duplicate records detected:",
        duplicate_count
    )

    # --------------------------------------------------------
    # STEP 2 — TRAIN FINAL V2 MODEL
    # --------------------------------------------------------

    tuning = tune_training_only(
        baseline,
        seed=42
    )

    model = fit_pipeline(
        baseline,
        max_depth=tuning["max_depth"],
        min_leaf=tuning["min_leaf"]
    )

    output_dir.mkdir(
        parents=True,
        exist_ok=True
    )

    # --------------------------------------------------------
    # STEP 3 — BASELINE DESCRIPTIVE STATISTICS
    # --------------------------------------------------------

    statistics_table = (
        baseline_descriptive_statistics(
            baseline
        )
    )

    print("\n" + "=" * 70)

    print(
        "PHASE 3 - BASELINE DESCRIPTIVE STATISTICS"
    )

    print("=" * 70)

    print(
        statistics_table.round(4).to_string()
    )

    statistics_table.to_csv(
        output_dir
        / "Baseline_Descriptive_Statistics.csv"
    )

    # --------------------------------------------------------
    # STEP 4 — LABELED BASELINE DATA
    # --------------------------------------------------------

    model["labelled"].to_csv(
        output_dir
        / "Baseline_With_Labels.csv",
        index=False
    )

    # --------------------------------------------------------
    # STEP 6 — RELIEFF REPORTS
    # --------------------------------------------------------

    model["relief"].to_csv(
        output_dir
        / "ReliefF_Feature_Ranking.csv",
        index=False
    )

    pd.DataFrame({
        "Selected Feature": model["selected"]
    }).to_csv(
        output_dir
        / "ReliefF_Selected_Features.csv",
        index=False
    )

    # --------------------------------------------------------
    # STEP 7 — SAVE C4.5 TREE
    # --------------------------------------------------------

    (
        output_dir / "C45_Tree.txt"
    ).write_text(
        tree_text(
            model["tree"]
        ) + "\n",
        encoding="utf-8"
    )

    # --------------------------------------------------------
    # STEP 8 — SAVE TRAINED MODEL
    # --------------------------------------------------------

    artifact = save_model(
        model,
        baseline_path,
        output_dir
    )

    artifact["training_only_tuning"] = tuning

    (
        output_dir / "trained_model.json"
    ).write_text(
        json.dumps(
            artifact,
            indent=2
        ),
        encoding="utf-8"
    )

    (
        output_dir / "Training_Only_Tuning.json"
    ).write_text(
        json.dumps(
            tuning,
            indent=2
        ),
        encoding="utf-8"
    )

    # --------------------------------------------------------
    # STEP 9 — PRINT FEATURE SELECTION RESULTS
    # --------------------------------------------------------

    print(
        "\nCLASS DISTRIBUTION:",
        model["class_counts"]
    )

    print(
        "ReliefF selected features:",
        model["selected"]
    )

    print(
        "\nTRAINED C4.5 TREE\n"
        + tree_text(model["tree"])
    )

    # --------------------------------------------------------
    # STEP 10 — CROSS-VALIDATE
    # --------------------------------------------------------

    cv = cross_validate(
        baseline,
        requested_folds=cv_folds,
        seed=42
    )

    # --------------------------------------------------------
    # STEP 11 — EXPORT CROSS-VALIDATION RESULTS
    # --------------------------------------------------------

    if "accuracy" in cv:

        print(
            f"\n{cv['folds']}-fold "
            "cross-validation accuracy:",
            f"{cv['accuracy']:.4f}"
        )

        print(
            "\nCONFUSION MATRIX"
        )

        print(
            cv["confusion_matrix"]
        )

        print(
            "\nPER-CLASS METRICS"
        )

        print(
            json.dumps(
                cv["per_class"],
                indent=2
            )
        )

        cv["confusion_matrix"].to_csv(
            output_dir
            / "Cross_Validation_Confusion_Matrix.csv"
        )

        pd.DataFrame(
            cv["record_predictions"]
        ).to_csv(
            output_dir
            / "Cross_Validation_Record_Predictions.csv",
            index=False
        )

        # Feature-selection stability report.

        stability_rows = [
            {
                "feature": feature,
                "folds_selected": count,
                "total_folds": cv["folds"],
                "selection_rate": (
                    count / cv["folds"]
                )
            }
            for feature, count in cv[
                "selection_frequency"
            ].items()
        ]

        pd.DataFrame(
            stability_rows
        ).sort_values(
            by=[
                "folds_selected",
                "feature"
            ],
            ascending=[
                False,
                True
            ]
        ).to_csv(
            output_dir
            / "ReliefF_Selection_Stability.csv",
            index=False
        )

        (
            output_dir
            / "Cross_Validation_Fold_Trees.json"
        ).write_text(
            json.dumps(
                cv["fold_diagnostics"],
                indent=2
            ),
            encoding="utf-8"
        )

        print(
            f"\nMacro F1: {cv['macro_f1']:.4f}"
        )

        print(
            "Balanced accuracy:",
            f"{cv['balanced_accuracy']:.4f}"
        )

        print(
            "\nINCORRECT VALIDATION PREDICTIONS"
        )

        for record in cv[
            "record_predictions"
        ]:

            if not record["correct"]:

                print(
                    f"CSV row {record['csv_row']}, "
                    f"fold {record['fold']}: "
                    f"{record['actual']} -> "
                    f"{record['predicted']}"
                )

    else:

        print(
            "\nCROSS-VALIDATION NOTE:",
            cv["note"]
        )

    # --------------------------------------------------------
    # STEP 14 — MODEL SUMMARY
    # --------------------------------------------------------

    if "accuracy" in cv:

        confusion_matrix_data = (
            cv["confusion_matrix"].to_dict()
        )

    else:

        confusion_matrix_data = None

    summary = {
        "baseline_records": len(baseline),

        "exact_duplicate_rows": duplicate_count,

        "thresholds": model["thresholds"],

        "class_distribution": model["class_counts"],

        "selected_features": model["selected"],

        "tree": model["tree"],

        "training_only_tuning": tuning,

        "cross_validation": {
            "folds": cv.get("folds"),
            "accuracy": cv.get("accuracy"),
            "confusion_matrix": confusion_matrix_data,
            "per_class": cv.get("per_class"),
            "macro_f1": cv.get("macro_f1"),
            "balanced_accuracy": cv.get(
                "balanced_accuracy"
            ),
            "selection_frequency": cv.get(
                "selection_frequency"
            ),
            "note": cv.get("note"),
            "evaluation_type": (
                "outer_stratified_cross_validation_"
                "with_training_fold_thresholds"
            )
        },

        "labeling_note": (
            "Each validation fold uses quartile "
            "thresholds calculated from its "
            "training partition only. "
            "Consequently, validation class labels "
            "may differ between folds."
        ),

        "warning": (
            "Verify dataset provenance. "
            "Synthetic demonstration data must "
            "not be reported as participant results."
        )
    }

    (
        output_dir / "Model_Summary.json"
    ).write_text(
        json.dumps(
            summary,
            indent=2
        ),
        encoding="utf-8"
    )

    return model

# ============================================================
# PHASE 9 — PLAYER INFERENCE AND UNITY VISUAL PRESETS
# ============================================================

def run_inference(
    player_path,
    model_path,
    output_dir
):
    """
    Run player classification using the saved
    ReliefF + C4.5 model.

    The first data row of the player CSV is used.

    Gameplay performance metrics are not required
    at inference time because the player may not
    have completed the gameplay attempt yet.

    Outputs:
    - AI_RESULT.json
    - Player_Prediction_Explanation.json

    The Unity JSON contains all 12 visual settings.
    """

    # --------------------------------------------------------
    # STEP 1 — LOAD TRAINED MODEL
    # --------------------------------------------------------

    model_path = Path(model_path)

    if not model_path.exists():
        raise FileNotFoundError(
            f"Trained model not found: {model_path}"
        )

    artifact = json.loads(
        model_path.read_text(
            encoding="utf-8"
        )
    )

    # --------------------------------------------------------
    # STEP 2 — READ FIRST PLAYER CSV RECORD
    # --------------------------------------------------------

    player_path = Path(player_path)

    if not player_path.exists():
        raise FileNotFoundError(
            f"Player CSV not found: {player_path}"
        )

    # Read only the first data row.
    # CSV header is not counted as a data row.

    raw = pd.read_csv(
        player_path,
        nrows=1
    )

    if raw.empty:
        raise ValueError(
            "Player CSV contains no gameplay records."
        )

    # --------------------------------------------------------
    # STEP 3 — VALIDATE PLAYER VISUAL FEATURES
    # --------------------------------------------------------

    missing = [
        feature
        for feature in FEATURES
        if feature not in raw.columns
    ]

    if missing:
        raise ValueError(
            f"Player CSV missing visual features: {missing}"
        )

    player = raw[FEATURES].copy()

    for feature in FEATURES:
        player[feature] = pd.to_numeric(
            player[feature],
            errors="coerce"
        )

    values = player.to_numpy(
        dtype=float
    )

    if not np.isfinite(values).all():
        raise ValueError(
            "Player visual settings contain "
            "missing, invalid, or infinite values."
        )

    if (
        (values < 0).any()
        or (values > 1).any()
    ):
        raise ValueError(
            "Player visual settings must be "
            "normalized between 0 and 1."
        )

    # The first gameplay attempt.

    row = player.iloc[0]

    # --------------------------------------------------------
    # STEP 4 — LOAD SELECTED MODEL FEATURES
    # --------------------------------------------------------

    selected = artifact[
        "selected_features"
    ]

    if not selected:
        raise ValueError(
            "Trained model contains no "
            "selected features."
        )

    # --------------------------------------------------------
    # STEP 5 — C4.5 PLAYER CLASSIFICATION
    # --------------------------------------------------------

    prediction = predict_tree(
        artifact["tree"],
        row[selected].to_dict()
    )

    decision_path, traced_prediction = (
        prediction_trace(
            artifact["tree"],
            row[selected].to_dict()
        )
    )

    if traced_prediction != prediction:
        raise RuntimeError(
            "Prediction and decision trace disagree."
        )

    # --------------------------------------------------------
    # STEP 6 — RETRIEVE BASELINE CLASS-MEAN PRESET
    # --------------------------------------------------------

    presets = artifact["presets"]

    preset = presets.get(
        prediction
    )

    if not preset:
        raise ValueError(
            "No trained baseline preset exists "
            f"for predicted class {prediction}."
        )

    missing_preset = [
        feature
        for feature in FEATURES
        if feature not in preset
    ]

    if missing_preset:
        raise ValueError(
            "Model preset is missing features: "
            f"{missing_preset}"
        )

    # --------------------------------------------------------
    # STEP 7 — CONVERT SETTINGS TO UNITY SCALE
    # --------------------------------------------------------

    # Baseline feature values are normalized [0,1].
    #
    # Historical Unity-facing conversion:
    #
    # Color      : 0..1 -> 0..360
    # Contrast   : 0..1 -> 0..2
    # Brightness : 0..1 -> 0..2
    #
    # IMPORTANT:
    # Verify these conversion ranges against
    # the actual Unity settings and slider limits.

    settings = {}

    for feature in FEATURES:

        if feature.endswith(
            "_color"
        ):
            factor = 360.0

        else:
            factor = 2.0

        settings[feature] = round(
            float(preset[feature]) * factor,
            6
        )

    # --------------------------------------------------------
    # STEP 8 — BUILD UNITY AI_RESULT PAYLOAD
    # --------------------------------------------------------

    payload = {
        "predicted_accessibility_level": prediction,

        "recommended_settings": settings,

        "selected_features": selected,

        "player_attempt_index": 0,

        "model_version": artifact.get(
            "model_version"
        ),

        "baseline_records_in_model": artifact[
            "baseline_records"
        ]
    }

    # --------------------------------------------------------
    # STEP 9 — BUILD PLAYER DECISION EXPLANATION
    # --------------------------------------------------------

    explanation_steps = []

    for step in decision_path:

        comparison = (
            "<="
            if step["branch"] == "left"
            else ">"
        )

        condition = (
            f"{step['feature']} "
            f"({step['value']:.6f}) "
            f"{comparison} "
            f"{step['threshold']:.6f}"
        )

        explanation_steps.append({
            **step,
            "comparison": comparison,
            "condition_satisfied": condition
        })

    preset_reason = (
        "The trained decision tree classified "
        f"the selected player as {prediction}; "
        f"the corresponding {prediction} "
        "baseline class-mean preset was selected."
    )

    explanation = {
        "player_attempt_index": 0,

        "predicted_accessibility_level": prediction,

        "selected_features": selected,

        "decision_path": explanation_steps,

        "terminal_leaf_class": traced_prediction,

        "preset_source": (
            "Baseline training records assigned "
            "to the predicted class"
        ),

        "preset_selection_reason": preset_reason,

        "normalized_baseline_class_mean_preset": preset,

        "unity_scaled_recommended_settings": settings,

        "caution": (
            "The decision trace explains model "
            "behavior. It does not establish "
            "that the recommended preset is optimal."
        )
    }

    # --------------------------------------------------------
    # STEP 10 — EXPORT JSON FILES
    # --------------------------------------------------------

    output_dir = Path(
        output_dir
    )

    output_dir.mkdir(
        parents=True,
        exist_ok=True
    )

    ai_result_path = (
        output_dir / "AI_RESULT.json"
    )

    explanation_path = (
        output_dir
        / "Player_Prediction_Explanation.json"
    )

    ai_result_path.write_text(
        json.dumps(
            payload,
            indent=2
        ),
        encoding="utf-8"
    )

    explanation_path.write_text(
        json.dumps(
            explanation,
            indent=2
        ),
        encoding="utf-8"
    )

    # --------------------------------------------------------
    # STEP 11 — PRINT PLAYER PREDICTION
    # --------------------------------------------------------

    print("\n" + "=" * 70)

    print(
        "PHASE 10 - PLAYER CLASSIFICATION"
    )

    print("=" * 70)

    print(
        "\nPLAYER DECISION PATH:"
    )

    if explanation_steps:

        for step in explanation_steps:
            print(
                "  "
                + step["condition_satisfied"]
            )

    else:

        print(
            "  Tree contains a single terminal leaf."
        )

    print(
        "\nPRESET REASON:",
        preset_reason
    )

    print(
        "\nPLAYER PREDICTION:",
        prediction
    )

    print(
        "\nAI_RESULT:",
        json.dumps(
            payload,
            indent=2
        )
    )

    print(
        "\nAI_RESULT SAVED TO:",
        ai_result_path.resolve()
    )

    print(
        "\nPLAYER EXPLANATION SAVED TO:",
        explanation_path.resolve()
    )

    return payload


# ============================================================
# PHASE 10 — COMMAND-LINE EXECUTION
# ============================================================

def main():
    """
    Execute the pipeline.

    Supported modes:

    all:
        Train V2 and classify a player.

    train:
        Train and evaluate V2 only.

    infer:
        Load an existing trained model
        and classify a player.

    This allows Unity to use an existing
    model without retraining after every
    gameplay attempt.
    """

    parser = argparse.ArgumentParser(
        description=(
            "Pipeline V2: ReliefF + "
            "C4.5 Decision Tree"
        )
    )

    # --------------------------------------------------------
    # EXECUTION MODE
    # --------------------------------------------------------

    parser.add_argument(
        "--mode",
        choices=[
            "all",
            "train",
            "infer"
        ],
        default="all",

        help=(
            "all = train and infer; "
            "train = training only; "
            "infer = player inference only"
        )
    )

    # --------------------------------------------------------
    # BASELINE CSV PATH
    # --------------------------------------------------------

    parser.add_argument(
        "--baseline",
        default=None,

        help=(
            "Baseline CSV file path. "
            "Required for all/train modes."
        )
    )

    # --------------------------------------------------------
    # PLAYER CSV PATH
    # --------------------------------------------------------

    parser.add_argument(
        "--player",
        default=None,

        help=(
            "Player CSV file path. "
            "Required for all/infer modes."
        )
    )

    # --------------------------------------------------------
    # TRAINED MODEL PATH
    # --------------------------------------------------------

    parser.add_argument(
        "--model",
        default="Results_V2/trained_model.json",

        help=(
            "Path to an existing trained_model.json "
            "used in inference-only mode."
        )
    )

    # --------------------------------------------------------
    # OUTPUT DIRECTORY
    # --------------------------------------------------------

    parser.add_argument(
        "--output-dir",
        default="Results_V2",

        help=(
            "Directory for generated model "
            "reports and AI_RESULT.json."
        )
    )

    # --------------------------------------------------------
    # CROSS-VALIDATION FOLDS
    # --------------------------------------------------------

    parser.add_argument(
        "--cv-folds",
        type=int,
        default=5,

        help=(
            "Requested number of "
            "stratified cross-validation folds."
        )
    )

    args = parser.parse_args()

    # --------------------------------------------------------
    # VALIDATE REQUIRED ARGUMENTS
    # --------------------------------------------------------

    if (
        args.mode in (
            "all",
            "train"
        )
        and not args.baseline
    ):

        parser.error(
            "--baseline is required for "
            "--mode all or --mode train"
        )

    if (
        args.mode in (
            "all",
            "infer"
        )
        and not args.player
    ):

        parser.error(
            "--player is required for "
            "--mode all or --mode infer"
        )

    output_dir = Path(
        args.output_dir
    )

    model_path = Path(
        args.model
    )

    # --------------------------------------------------------
    # TRAINING
    # --------------------------------------------------------

    if args.mode in (
        "all",
        "train"
    ):

        print("\n" + "=" * 70)

        print(
            "PIPELINE V2 - TRAINING"
        )

        print(
            "RELIEFF + C4.5"
        )

        print("=" * 70)

        run_training(
            baseline_path=args.baseline,
            output_dir=output_dir,
            cv_folds=args.cv_folds
        )

        # The newly trained model is stored
        # inside the selected output directory.

        model_path = (
            output_dir / "trained_model.json"
        )

    # --------------------------------------------------------
    # PLAYER INFERENCE
    # --------------------------------------------------------

    if args.mode in (
        "all",
        "infer"
    ):

        print("\n" + "=" * 70)

        print(
            "PIPELINE V2 - PLAYER INFERENCE"
        )

        print("=" * 70)

        run_inference(
            player_path=args.player,
            model_path=model_path,
            output_dir=output_dir
        )

    # --------------------------------------------------------
    # EXECUTION COMPLETE
    # --------------------------------------------------------

    print("\n" + "=" * 70)

    print(
        "PIPELINE V2 EXECUTION COMPLETED"
    )

    print("=" * 70)

    print(
        "\nOutputs saved to:",
        output_dir.resolve()
    )


# ============================================================
# PROGRAM ENTRY POINT
# ============================================================

if __name__ == "__main__":
    main()