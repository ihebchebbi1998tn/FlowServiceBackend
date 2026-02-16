import { useTranslation } from "react-i18next";
import { Card, CardHeader, CardContent } from "@/components/ui/card";
import { CalendarRange, Plus } from 'lucide-react';
import { Button } from '@/components/ui/button';

export default function Dispatch() {
  // use default namespace so dispatcher.* keys resolve correctly
  const { t } = useTranslation();
  return (
    <div className="flex flex-col">
      {/* Header */}
      <header className="flex items-center justify-between p-4 border-b border-border bg-card/50 backdrop-blur">
        <div className="flex items-center gap-3">
          <div className="p-2 rounded-lg bg-primary/10">
            <CalendarRange className="h-6 w-6 text-primary" />
          </div>
          <div>
            <h1 className="text-xl font-semibold text-foreground">{t('dispatcher.title')}</h1>
            <p className="text-[11px] text-muted-foreground">{t('dispatcher.description')}</p>
          </div>
        </div>
        <div>
          <Button className="bg-primary text-white hover:bg-primary/90 shadow-medium hover-lift w-full sm:w-auto" onClick={() => window.location.assign('/dashboard/field/service-orders')}>
            <Plus className="mr-2 h-4 w-4 text-white" />
            {t('dispatcher.open_service_orders', 'Service Orders')}
          </Button>
        </div>
      </header>

      <div className="p-4">
        <Card className="shadow-card">
          <CardContent>
            <p className="text-muted-foreground text-sm">{t('dispatcher.dispatch_placeholder', 'Interactive calendar & map, drag-and-drop assignments, technician availability.')}</p>
          </CardContent>
        </Card>
      </div>
    </div>
  );
}
