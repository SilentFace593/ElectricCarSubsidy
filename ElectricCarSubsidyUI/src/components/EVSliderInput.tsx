// src/components/EVSliderInput.tsx
import { useRef, useCallback } from "react";

interface EVSliderInputProps {
  value: number;
  onChange: (value: number) => void;
  min?: number;
  max?: number;
  step?: number;
}

export const EVSliderInput = ({ value, onChange, min = 0, max = 100, step = 5 }: EVSliderInputProps) => {
  const trackRef = useRef<HTMLDivElement>(null);
  const draggingRef = useRef(false);

  const percent = ((value - min) / (max - min)) * 100;

  const computeValueFromClientX = useCallback((clientX: number) => {
    const track = trackRef.current;
    if (!track) return value;

    const rect = track.getBoundingClientRect();
    const ratio = Math.min(1, Math.max(0, (clientX - rect.left) / rect.width));
    const raw = min + ratio * (max - min);
    const stepped = Math.round(raw / step) * step;
    return Math.min(max, Math.max(min, stepped));
  }, [min, max, step, value]);

  const handlePointerDown = useCallback((e: React.MouseEvent) => {
    draggingRef.current = true;
    onChange(computeValueFromClientX(e.clientX));

    const handleMove = (moveEvent: MouseEvent) => {
      if (!draggingRef.current) return;
      onChange(computeValueFromClientX(moveEvent.clientX));
    };

    const handleUp = () => {
      draggingRef.current = false;
      window.removeEventListener("mousemove", handleMove);
      window.removeEventListener("mouseup", handleUp);
    };

    window.addEventListener("mousemove", handleMove);
    window.addEventListener("mouseup", handleUp);
  }, [computeValueFromClientX, onChange]);

  return (
    <div
      ref={trackRef}
      onMouseDown={handlePointerDown}
      style={{
        position: "relative",
        width: "100%",
        height: "20rem",
        display: "flex",
        alignItems: "center",
        cursor: "pointer",
      }}
    >
      <div
        style={{
          position: "absolute",
          left: 0,
          right: 0,
          height: "4rem",
          borderRadius: "2rem",
          background: "rgba(255,255,255,0.15)",
          overflow: "hidden",
        }}
      >
        <div
          style={{
            height: "100%",
            width: `${percent}%`,
            background: "linear-gradient(90deg, #4fc3f7, #29b6f6)",
          }}
        />
      </div>
      <div
  style={{
    position: "absolute",
    left: `${percent}%`,
    transform: "translateX(-8rem)",
    width: "16rem",
    height: "16rem",
    borderRadius: "50%",
    background: "#ffffff",
    border: "2rem solid #29b6f6",
    boxShadow: "0 1rem 3rem rgba(0,0,0,0.4)",
    pointerEvents: "none",
  }}
/>
    </div>
  );
};