import { cn } from "@/lib/utils"
import { ChevronUp } from "lucide-react"

type TierLevel = "Bronze" | "Silver" | "Gold" | "Platinum"

interface TierBadgeProps {
  tier: TierLevel
}

const tierConfig = {
  Bronze: { color: "bg-secondary hover:bg-secondary/80 text-amber-500", chevrons: 1 },
  Silver: { color: "bg-secondary hover:bg-secondary/80 text-slate-400", chevrons: 2 },
  Gold: { color: "bg-secondary hover:bg-secondary/80 text-yellow-500", chevrons: 3 },
  Platinum: { color: "bg-secondary hover:bg-secondary/80 text-cyan-400", chevrons: 4 },
}

export function TierBadge({ tier }: TierBadgeProps) {
  const config = tierConfig[tier]
  
  return (
    <div 
      className={cn(
        "inline-flex items-center rounded-md px-3 py-2 text-xs sm:text-sm font-medium transition-colors h-9",
        config.color
      )}
    >
      <span>{tier} Seller</span>
      <div className="flex flex-col -space-y-2.5 ml-1">
        {Array.from({ length: config.chevrons }).map((_, i) => (
          <ChevronUp key={i} className="h-3.5 w-3.5" />
        ))}
      </div>
    </div>
  )
}
