// src/components/EVSubsidyPanel.tsx
import { Component, useEffect, useState, useRef } from "react";
import { useValue } from "cs2/api";
import { Entity, selectedInfo } from "cs2/bindings";
import { subsidyState$, applySubsidy, lastApplyToken$, lastApplySuccess$ } from "../bindings/evSubsidyBindings";
import { EDUCATION_LABELS, BASE_POINTS, POINTS_PER_FIVE_PERCENT } from "../types/evSubsidy";
import { EVSliderInput } from "./EVSliderInput";

function formatRemaining(daysRemaining: number): string {
  if (daysRemaining <= 0) return "imminent";
  const totalMinutes = daysRemaining * 1440;
  const days = Math.floor(daysRemaining);
  const hours = Math.floor((totalMinutes % 1440) / 60);
  if (days > 0) return `${days}j ${hours}h`;
  return `${hours}h`;
}

let editingSliders: number[] | null = null;
type Listener = () => void;
const draftListeners = new Set<Listener>();

function getDraft(fallback: number[]) {
  return editingSliders ?? fallback;
}
function setDraft(value: number[]) {
  editingSliders = value;
  draftListeners.forEach((l) => l());
}
function clearDraft() {
  editingSliders = null;
  draftListeners.forEach((l) => l());
}
function subscribeDraft(listener: Listener) {
  draftListeners.add(listener);
  return () => { draftListeners.delete(listener); };
}

let isExpanded = false;
const expandListeners = new Set<Listener>();
function setExpanded(value: boolean) {
  isExpanded = value;
  expandListeners.forEach((l) => l());
}
function subscribeExpanded(listener: Listener) {
  expandListeners.add(listener);
  return () => { expandListeners.delete(listener); };
}

function CollapsibleArrow({ expanded }: { expanded: boolean }) {
  return (
    <span style={{ display: "inline-block", transition: "transform 0.15s ease", transform: expanded ? "rotate(90deg)" : "rotate(0deg)", marginRight: "6rem" }}>
      ▶
    </span>
  );
}

class SafeBoundary extends Component<{ children: any }, { crashed: boolean }> {
  constructor(props: any) {
    super(props);
    this.state = { crashed: false };
  }
  static getDerivedStateFromError() {
    return { crashed: true };
  }
  render() {
    if (this.state.crashed) return null;
    return this.props.children;
  }
}

const EVSubsidyPanelInner = ({ InfoSection }: { InfoSection: any }) => {
  const selectedEntity = useValue(selectedInfo.selectedEntity$);
  const state = useValue(subsidyState$);
  const lastApplyToken = useValue(lastApplyToken$);
  const lastApplySuccess = useValue(lastApplySuccess$);

  const [, forceRender] = useState(0);
  useEffect(() => subscribeDraft(() => forceRender((n) => n + 1)), []);
  useEffect(() => subscribeExpanded(() => forceRender((n) => n + 1)), []);

  const prevTokenRef = useRef(lastApplyToken);
  useEffect(() => {
    if (lastApplyToken !== prevTokenRef.current) {
      prevTokenRef.current = lastApplyToken;
      if (lastApplySuccess) {
        clearDraft();
      }
      // en échec : le brouillon est conservé tel quel
    }
  }, [lastApplyToken, lastApplySuccess]);

  const expanded = isExpanded;
  const sliders = getDraft(state.sliders);

  if (!state.hasState || !InfoSection) return null;

  const handleSliderChange = (tier: number, value: number) => {
    const updated = [...sliders];
    updated[tier] = value;
    setDraft(updated);
  };

  const handleApply = () => {
    applySubsidy(selectedEntity as Entity, sliders);
    // clearDraft() est déclenché par l'effet ci-dessus, conditionné au succès
  };

  const pointsRemaining = (tier: number) => {
    const compensated = Math.floor(sliders[tier] / 5) * POINTS_PER_FIVE_PERCENT;
    return Math.min(0, BASE_POINTS[tier] + compensated);
  };

  const cost = (tier: number) => {
    return state.vehicleCost * (sliders[tier] / 100) * state.eligibleCounts[tier];
  };

  const totalCost = EDUCATION_LABELS.reduce((sum, _, tier) => sum + cost(tier), 0);
  const insufficientFunds = totalCost > state.playerMoney;

  const toggle = () => setExpanded(!expanded);

  return (
    <InfoSection disableFoldout={true}>
      <div style={{ padding: "4rem 10rem", boxSizing: "border-box" }}>
        <div
          onClick={toggle}
          style={{ display: "flex", alignItems: "center", cursor: "pointer", userSelect: "none", color: "rgba(255,255,255,0.7)", fontSize: "12rem", marginBottom: expanded ? "4rem" : "0", textTransform: "uppercase" }}
        >
          <CollapsibleArrow expanded={expanded} />
          Subvention véhicules électriques
        </div>

        {expanded && (
          <div>
           {state.slidersLocked && (
  <div style={{ color: "#ff6b6b", marginBottom: "6rem" }}>
    {`Sliders verrouillés — les citoyens sont mécontents de l'absence de réaction — Réouverture dans ${formatRemaining(state.lockExpiry - state.currentDay)}`}
  </div>
)}

{state.distrustTraitActive && (
  <div style={{ color: "#ff6b6b", marginBottom: "6rem" }}>
    {`Trait actif : "Citoyens méfiants de la politique locale" — expire dans ${formatRemaining(state.distrustTraitExpiry - state.currentDay)}`}
  </div>
)}

{state.angerTraitActive && (
  <div style={{ color: "#ff3b3b", marginBottom: "6rem" }}>
    {`Trait actif : "Citoyens en colères" — persiste tant que le bannissement des moteurs thermiques est actif`}
  </div>
)}

{state.traitActive && (
  <div style={{ color: "#7dd87d", marginBottom: "6rem" }}>
    {`Trait actif : "Mobilité électrique subventionnée" — expire dans ${formatRemaining(state.traitExpiry - state.currentDay)}`}
  </div>
)}

            {!state.slidersLocked && !state.policyApplied && state.combustionBanActive && (
  <>
    <div style={{ fontSize: "11rem", color: "rgba(255,255,255,0.5)", marginTop: "6rem" }}>
      {`Prochaine réévaluation des éligibles dans ${formatRemaining(state.nextEligibilityCheck - state.currentDay)}`}
    </div>

    {EDUCATION_LABELS.map((label, tier) => (
      <div key={tier} style={{ marginBottom: "8rem" }}>
        <div style={{ display: "flex", justifyContent: "space-between" }}>
          <span>{label}</span>
          <span>{sliders[tier]}%</span>
        </div>
        <EVSliderInput value={sliders[tier]} onChange={(value) => handleSliderChange(tier, value)} />
        <div style={{ display: "flex", justifyContent: "space-between", fontSize: "11rem", color: "rgba(255,255,255,0.6)" }}>
          <span>Points restants : {pointsRemaining(tier)}</span>
          <span>Coût : {cost(tier).toLocaleString()} ¢</span>
        </div>
      </div>
    ))}

    <button
      onClick={handleApply}
      disabled={insufficientFunds}
      style={{
        background: insufficientFunds ? "rgba(255,255,255,0.15)" : "linear-gradient(90deg, #4fc3f7, #29b6f6)",
        color: insufficientFunds ? "rgba(255,255,255,0.4)" : "#ffffff",
        cursor: insufficientFunds ? "not-allowed" : "pointer",
        border: "none", borderRadius: "4rem", padding: "8rem 16rem", fontSize: "13rem", fontWeight: 600, marginTop: "6rem",
      }}
    >
      Appliquer
    </button>
    {insufficientFunds && (
      <div style={{ color: "#ff6b6b", fontSize: "11rem", marginTop: "4rem" }}>
        {`Fonds insuffisants : ${totalCost.toLocaleString()} ¢ nécessaires, ${state.playerMoney.toLocaleString()} ¢ disponibles`}
      </div>
    )}
  </>
)}

{!state.slidersLocked && !state.policyApplied && !state.combustionBanActive && state.distrustTraitActive && (
  <div style={{ color: "rgba(255,255,255,0.6)", fontSize: "11rem", marginTop: "6rem", fontStyle: "italic" }}>
    {`Le bannissement des moteurs thermiques n'est plus actif — aucune nouvelle subvention n'est nécessaire.`}
  </div>
)}

            {state.policyApplied && (
               <div>
    {state.carriedOverRecipients > 0 && (
      <div style={{ color: "rgba(255,255,255,0.5)", fontSize: "11rem", marginBottom: "6rem", fontStyle: "italic" }}>
        {`${state.carriedOverRecipients} citoyen${state.carriedOverRecipients > 1 ? "s" : ""} de ce quartier ayant déjà reçu une subvention ailleurs sont pris en compte dans le calcul.`}
      </div>
    )}
                {EDUCATION_LABELS.map((label, tier) => (
                  state.progress[tier] > 0 || state.sliders[tier] > 0 ? (
                    <div key={tier} style={{ marginBottom: "6rem" }}>
                      <div style={{ display: "flex", justifyContent: "space-between", fontSize: "12rem" }}>
                        <span>{label}</span>
                        <span>{(state.progress[tier] * 100).toFixed(0)}% adopté ({state.sliders[tier]}% subventionné)</span>
                      </div>
                    </div>
                  ) : null
                ))}
              </div>
            )}
          </div>
        )}
      </div>
    </InfoSection>
  );
};

export const EVSubsidyPanel = (InfoSection: any) => (props: any) => (
  <SafeBoundary>
    <EVSubsidyPanelInner InfoSection={InfoSection} />
  </SafeBoundary>
);