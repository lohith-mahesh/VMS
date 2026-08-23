type RRLogoProps = { size?: 'small' | 'medium' | 'large'; className?: string; ariaLabel?: string }

const sizes = {
  small: { wrapper: 'h-8 w-7', imageWidth: 40, left: -6, top: -15 },
  medium: { wrapper: 'h-11 w-10', imageWidth: 56, left: -8, top: -21 },
  large: { wrapper: 'h-16 w-14', imageWidth: 78, left: -11, top: -30 },
}

export function RRLogo({ size = 'medium', className = '', ariaLabel = 'RRVMS' }: RRLogoProps) {
  const dimensions = sizes[size]
  return <span className={`relative block shrink-0 overflow-hidden bg-[var(--rr-primary)] ${dimensions.wrapper} ${className}`} aria-label={ariaLabel}><img src="/Rolls_royce_holdings_logo.svg" alt="" aria-hidden="true" className="absolute max-w-none" style={{ width: dimensions.imageWidth, left: dimensions.left, top: dimensions.top }} /></span>
}
