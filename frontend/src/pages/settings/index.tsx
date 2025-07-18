import { usePageTitle } from "@/hooks/use-page-title";

export default function SettingsPage() {
  usePageTitle('Settings');
  return (
    <div className="flex-1 space-y-4 p-8 pt-6">
      <div className="flex items-center justify-between space-y-2">
        <h2 className="text-3xl font-bold tracking-tight">Settings</h2>
      </div>
      {/* Add your settings content here */}
    </div>
  );
}
