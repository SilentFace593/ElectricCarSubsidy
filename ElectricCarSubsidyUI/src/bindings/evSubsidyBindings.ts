// src/bindings/evSubsidyBindings.ts
import { bindValue, trigger } from "cs2/api";
import { Entity } from "cs2/bindings";
import { SubsidyState, EMPTY_STATE } from "../types/evSubsidy";

const GROUP = "evSubsidy";

export const panelVisible$ = bindValue<boolean>(GROUP, "panelVisible", false);
export const subsidyState$ = bindValue<SubsidyState>(GROUP, "subsidyState", EMPTY_STATE);
export const lastApplyToken$ = bindValue<number>(GROUP, "lastApplyToken", 0);
export const lastApplySuccess$ = bindValue<boolean>(GROUP, "lastApplySuccess", false);

export function applySubsidy(district: Entity, sliderValues: number[]) {
    trigger(GROUP, "applySubsidy", district, sliderValues.join(","));
}