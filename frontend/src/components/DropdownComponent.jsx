import React, { useState, useRef, useEffect, useCallback, useLayoutEffect } from 'react';
import { createPortal } from 'react-dom';

const DropdownComponent = ({
  toggleContent,
  dropdownContent,
  className = '',
  classNameChild = '',
  minWidthMatchToggle = true,
}) => {
  const [open, setOpen] = useState(false);
  const [visible, setVisible] = useState(false);
  const [coords, setCoords] = useState({ top: 0, left: 0, minWidth: undefined });
  const [mounted, setMounted] = useState(false); // <-- tránh lỗi khi SSR/Next.js
  const dropdownRef = useRef(null);
  const toggleRef = useRef(null);
  const rafRef = useRef(0);
  const pendingRef = useRef(false);

  useEffect(() => setMounted(true), []);

  const computeCoords = useCallback(() => {
    const tEl = toggleRef.current;
    const dEl = dropdownRef.current;
    if (!tEl || !dEl) return;

    const tRect = tEl.getBoundingClientRect();
    const dRect = dEl.getBoundingClientRect();
    const vw = window.innerWidth;
    const vh = window.innerHeight;
    const GAP = 4;

    let top = tRect.bottom + GAP;
    let left = tRect.left;

    if (vh - tRect.bottom < dRect.height + GAP && tRect.top > dRect.height + GAP) {
      top = tRect.top - dRect.height - GAP;
    }
    if (left + dRect.width > vw - 4) left = Math.max(4, tRect.right - dRect.width);
    if (left < 4) left = 4;
    if (top + dRect.height > vh - 4) top = Math.max(4, vh - dRect.height - 4);
    if (top < 4) top = 4;

    setCoords({
      top,
      left,
      minWidth: minWidthMatchToggle ? Math.ceil(tRect.width) : undefined,
    });
  }, [minWidthMatchToggle]);

  const scheduleRecalc = useCallback(() => {
    if (pendingRef.current) return;
    pendingRef.current = true;
    rafRef.current = requestAnimationFrame(() => {
      pendingRef.current = false;
      computeCoords();
    });
  }, [computeCoords]);

  const openDropdown = () => setOpen(true);
  const closeDropdown = () => {
    setVisible(false);
    setOpen(false);
  };
  const toggleDropdown = () => (open ? closeDropdown() : openDropdown());

  useEffect(() => {
    if (!open) return;
    const onDown = (e) => {
      const t = e.target;
      if (
        dropdownRef.current && !dropdownRef.current.contains(t) &&
        toggleRef.current && !toggleRef.current.contains(t)
      ) closeDropdown();
    };
    document.addEventListener('mousedown', onDown);
    document.addEventListener('touchstart', onDown, { passive: true });
    return () => {
      document.removeEventListener('mousedown', onDown);
      document.removeEventListener('touchstart', onDown);
    };
  }, [open]);

  useLayoutEffect(() => {
    if (!open) return;
    setVisible(false); // ẩn để đo cho chuẩn
  }, [open]);

  useEffect(() => {
    if (!open) return;

    const id = requestAnimationFrame(() => {
      computeCoords();
      requestAnimationFrame(() => setVisible(true)); // hiện sau khi set vị trí
    });

    const onRecalc = scheduleRecalc;
    window.addEventListener('resize', onRecalc, { passive: true });
    window.addEventListener('scroll', onRecalc, true);
    window.addEventListener('orientationchange', onRecalc);

    let ro = null;
    if (window.ResizeObserver && toggleRef.current) {
      ro = new ResizeObserver(onRecalc);
      ro.observe(toggleRef.current);
    }

    return () => {
      cancelAnimationFrame(id);
      cancelAnimationFrame(rafRef.current);
      window.removeEventListener('resize', onRecalc, true);
      window.removeEventListener('scroll', onRecalc, true);
      window.removeEventListener('orientationchange', onRecalc);
      if (ro) ro.disconnect();
    };
  }, [open, computeCoords, scheduleRecalc]);

  return (
    <>
      <div className={`dropdown ${open ? 'show' : ''} ${className}`} style={{ position: 'relative' }}>
        <button
          type="button"
          ref={toggleRef}
          onClick={toggleDropdown}
          aria-haspopup="menu"
          aria-expanded={open}
          className="bg-transparent border-0 p-0 m-0"
          style={{ display: 'flex', alignItems: 'center', cursor: 'pointer' }}
          onKeyDown={(e) => {
            if (e.key === 'Escape') closeDropdown();
            if (e.key === 'ArrowDown' && !open) openDropdown();
          }}
        >
          {toggleContent}
        </button>
      </div>

      {mounted && open && createPortal(
        <div
          ref={dropdownRef}
          role="menu"
          // NOTE: thêm 'show' và display:block để không phụ thuộc CSS của Bootstrap
          className={`dropdown-menu dropdown-menu-icon-list show ${classNameChild || ''}`}
          style={{
            position: 'fixed',
            top: coords.top,
            left: coords.left,
            display: 'block',          // <--- QUAN TRỌNG
            zIndex: 1000,
            maxHeight: '80vh',
            minWidth: coords.minWidth,
            transform: visible ? 'translateY(0) scale(1)' : 'translateY(-4px) scale(0.98)',
            opacity: visible ? 1 : 0,
            transition: 'transform 120ms ease, opacity 120ms ease',
            willChange: 'transform, opacity',
          }}
        >
          {dropdownContent}
        </div>,
        document.body
      )}
    </>
  );
};

export default DropdownComponent;