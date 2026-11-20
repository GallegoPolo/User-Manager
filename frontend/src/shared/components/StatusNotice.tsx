import { useEffect, useRef } from "react";

export interface Notice {
  kind: "ok" | "warn";
  text: string;
  takeFocus?: boolean;
}

export function StatusNotice({ notice }: { notice: Notice | null }) {
  const noticeRef = useRef<HTMLParagraphElement>(null);

  useEffect(() => {
    if (notice?.takeFocus) noticeRef.current?.focus();
  }, [notice]);

  if (!notice) return null;

  return (
    <p ref={noticeRef} role="status" tabIndex={-1} className={`notice notice--${notice.kind}`}>
      {notice.text}
    </p>
  );
}
