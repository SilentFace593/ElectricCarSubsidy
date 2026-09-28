// src/types/evSubsidy.ts

export interface SubsidyState {
    hasState: boolean;
    policyApplied: boolean;
    slidersLocked: boolean;
    applyDeadline: number;
    currentDay: number;
    traitActive: boolean;
    traitExpiry: number;
    distrustTraitActive: boolean;
    distrustTraitExpiry: number;
    subsidizedAdultsCount: number;
    vehicleCost: number;
    sliders: number[];       // 5 valeurs, 0-100
    progress: number[];      // 5 valeurs, 0-1
    eligibleCounts: number[]; // 5 valeurs
    nextEligibilityCheck: number;
    lockExpiry: number;
    angerTraitActive: boolean;
    playerMoney: number;
    combustionBanActive: boolean;
    carriedOverRecipients: number;
}

export const EMPTY_STATE: SubsidyState = {
    hasState: false,
    policyApplied: false,
    slidersLocked: false,
    applyDeadline: 0,
    currentDay: 0,
    traitActive: false,
    traitExpiry: 0,
    distrustTraitActive: false,
    distrustTraitExpiry: 0,
    subsidizedAdultsCount: 0,
    vehicleCost: 35000,
    sliders: [0, 0, 0, 0, 0],
    progress: [0, 0, 0, 0, 0],
    eligibleCounts: [0, 0, 0, 0, 0],
    nextEligibilityCheck: 0,
    lockExpiry: 0,
    angerTraitActive: false,
    playerMoney: 0,
    combustionBanActive: false,
    carriedOverRecipients: 0,
};

export const EDUCATION_LABELS = [
    "Sans Instruction",
    "Peu Instruits",
    "Instruits",
    "Instruction avancée",
    "Instruction supérieure",
];

export const BASE_POINTS = [-100, -70, -50, -30, -10];
export const POINTS_PER_FIVE_PERCENT = 10;