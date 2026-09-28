// src/index.tsx
import { ModRegistrar } from "cs2/modding";
import { createElement, Fragment } from "react";
import { EVSubsidyPanel } from "./components/EVSubsidyPanel";

const register: ModRegistrar = (moduleRegistry) => {
  const InfoSection = moduleRegistry.registry.get(
    "game-ui/game/components/selected-info-panel/shared-components/info-section/info-section.tsx"
  )?.["InfoSection"];

  moduleRegistry.extend(
    "game-ui/game/components/selected-info-panel/selected-info-sections/selected-info-sections.tsx",
    "selectedInfoSectionComponents",
    (original: any) => {
      const originalPolicies = original["Game.UI.InGame.PoliciesSection"];

      const WrappedPoliciesSection = (props: any) =>
        createElement(
          Fragment,
          null,
          originalPolicies ? createElement(originalPolicies, props) : null,
          createElement(EVSubsidyPanel(InfoSection), props)
          // Si DistrictNoteSection doit rester en dessous, ajoute-le ici aussi :
          // , createElement(DistrictNoteSection(InfoSection), props)
        );

      return {
        ...original,
        "Game.UI.InGame.PoliciesSection": WrappedPoliciesSection,
      };
    }
  );
};

export default register;