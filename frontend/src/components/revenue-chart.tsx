import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "@/components/ui/card";
import { ResponsiveContainer, AreaChart, Area, XAxis, YAxis, CartesianGrid, Tooltip } from 'recharts';
import { format } from "date-fns";

const data = [
  {
    month: "Jan",
    date: "2024-01-01",
    revenue: 4000,
    profit: 2400,
  },
  {
    month: "Feb",
    date: "2024-02-01",
    revenue: 3000,
    profit: 1398,
  },
  {
    month: "Mar",
    date: "2024-03-01",
    revenue: 2000,
    profit: 9800,
  },
  {
    month: "Apr",
    date: "2024-04-01",
    revenue: 2780,
    profit: 3908,
  },
  {
    month: "May",
    date: "2024-05-01",
    revenue: 1890,
    profit: 4800,
  },
  {
    month: "Jun",
    date: "2024-06-01",
    revenue: 2390,
    profit: 3800,
  },
  {
    month: "Jul",
    date: "2024-07-01",
    revenue: 3490,
    profit: 4300,
  },
  {
    month: "Aug",
    date: "2024-08-01",
    revenue: 4000,
    profit: 2400,
  },
  {
    month: "Sep",
    date: "2024-09-01",
    revenue: 3000,
    profit: 1398,
  },
  {
    month: "Oct",
    date: "2024-10-01",
    revenue: 2000,
    profit: 9800,
  },
  {
    month: "Nov",
    date: "2024-11-01",
    revenue: 2780,
    profit: 3908,
  },
  {
    month: "Dec",
    date: "2024-12-01",
    revenue: 3890,
    profit: 4800,
  },
];

export function RevenueChart() {

  return (
    <Card className="col-span-4 shadow-sm hover:shadow-md transition-shadow duration-200">
      <CardHeader className="space-y-1 px-3 sm:px-4 lg:px-6">
        <CardTitle className="text-base sm:text-lg">Revenue vs Profit</CardTitle>
        <CardDescription className="text-xs sm:text-sm">
          Monthly comparison of revenue and profit
        </CardDescription>
      </CardHeader>
      <CardContent className="h-[250px] sm:h-[300px] lg:h-[400px] px-0 sm:px-2 lg:px-4">
        <ResponsiveContainer width="100%" height="100%">
          <AreaChart
            data={data}
            margin={{
              top: 20,
              right: 10,
              left: -10,
              bottom: 0,
            }}
            className="text-xs sm:text-sm"
          >
            <CartesianGrid strokeDasharray="3 3" className="stroke-border" />
            <XAxis
              dataKey="date"
              tickFormatter={(value: string) => format(new Date(value), "MMM")}
              tickLine={false}
              axisLine={false}
              dy={8}
              className="text-muted-foreground"
            />
            <YAxis
              tickLine={false}
              axisLine={false}
              tickFormatter={(value) => `£${value.toLocaleString('en-GB')}`}
              width={55}
              fontSize={12}
              className="text-muted-foreground"
              dx={-5}
            />
            <Tooltip
              content={({ active, payload }) => {
                if (active && payload && payload.length) {
                  return (
                    <div className="rounded-lg border bg-background p-2 shadow-sm text-xs sm:text-sm">
                      <div className="grid grid-cols-2 gap-2 sm:gap-4">
                        <div className="flex flex-col">
                          <span className="text-[0.65rem] sm:text-[0.70rem] uppercase text-muted-foreground">
                            Revenue
                          </span>
                          <span className="font-bold text-muted-foreground">
                            £{payload[0].value&&payload[0]?.value.toLocaleString()}
                          </span>
                        </div>
                        <div className="flex flex-col">
                          <span className="text-[0.65rem] sm:text-[0.70rem] uppercase text-muted-foreground">
                            Profit
                          </span>
                          <span className="font-bold text-muted-foreground">
                            £{payload[1].value&&payload[1].value.toLocaleString()}
                          </span>
                        </div>
                      </div>
                    </div>
                  )
                }
                return null
              }}
            />
            <Area
              type="monotone"
              dataKey="revenue"
              strokeWidth={2}
              stroke="hsl(var(--primary))"
              fill="hsl(var(--primary))"
              fillOpacity={0.2}
              className="stroke-primary fill-primary"
            />
            <Area
              type="monotone"
              dataKey="profit"
              strokeWidth={2}
              stroke="hsl(var(--success))"
              fill="hsl(var(--success))"
              fillOpacity={0.2}
              className="stroke-success fill-success"
            />
          </AreaChart>
        </ResponsiveContainer>
      </CardContent>
    </Card>
  );
}
