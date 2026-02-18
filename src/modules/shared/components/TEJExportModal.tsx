// TEJ Export Modal - Connected to backend API
import { useState } from 'react';
import { useTranslation } from 'react-i18next';
import { Dialog, DialogContent, DialogHeader, DialogTitle, DialogFooter, DialogDescription } from '@/components/ui/dialog';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '@/components/ui/select';
import { Alert, AlertDescription } from '@/components/ui/alert';
import { Card, CardContent } from '@/components/ui/card';
import { FileDown, AlertCircle, CheckCircle2, Loader2 } from 'lucide-react';
import { toast } from 'sonner';
import {
  exportTEJ,
  fetchRSRecords,
  type TEJExportResponseDto,
} from '@/modules/shared/services/rsApiService';

interface TEJExportModalProps {
  open: boolean;
  onOpenChange: (open: boolean) => void;
  entityType: string;
  entityId: number;
  onExportComplete: () => void;
}

const MONTHS_FR = [
  'Janvier', 'Février', 'Mars', 'Avril', 'Mai', 'Juin',
  'Juillet', 'Août', 'Septembre', 'Octobre', 'Novembre', 'Décembre',
];

export function TEJExportModal({ open, onOpenChange, entityType, entityId, onExportComplete }: TEJExportModalProps) {
  const { t } = useTranslation();
  const currentDate = new Date();

  const [payerName, setPayerName] = useState('');
  const [payerTaxId, setPayerTaxId] = useState('');
  const [payerAddress, setPayerAddress] = useState('');
  const [month, setMonth] = useState(String(currentDate.getMonth() + 1));
  const [year, setYear] = useState(String(currentDate.getFullYear()));
  const [errors, setErrors] = useState<string[]>([]);
  const [exportResult, setExportResult] = useState<TEJExportResponseDto | null>(null);
  const [exporting, setExporting] = useState(false);
  const [recordCount, setRecordCount] = useState(0);
  const [totalRS, setTotalRS] = useState(0);

  // Load stats when month/year changes
  const loadStats = async () => {
    try {
      const result = await fetchRSRecords({
        entityType,
        entityId,
        month: Number(month),
        year: Number(year),
        status: 'pending',
        limit: 1,
      });
      setRecordCount(result.pagination?.total || 0);
      setTotalRS(result.records?.reduce((sum, r) => sum + r.rsAmount, 0) || 0);
    } catch {
      // Ignore
    }
  };

  // Load stats on open
  useState(() => {
    if (open) loadStats();
  });

  const handleExport = async () => {
    setErrors([]);
    setExportResult(null);

    if (!payerName.trim()) { setErrors([t('rs.err.payerRequired', 'La raison sociale est requise')]); return; }
    if (!payerTaxId.trim()) { setErrors([t('rs.err.payerTaxIdRequired', 'Le matricule fiscal est requis')]); return; }

    setExporting(true);
    try {
      const result = await exportTEJ({
        month: Number(month),
        year: Number(year),
        declarant: {
          name: payerName,
          taxId: payerTaxId,
          address: payerAddress,
        },
      });

      setExportResult(result);

      if (result.status === 'success') {
        toast.success(t('rs.tejExportSuccess', 'Export TEJ réussi : {{fileName}}', { fileName: result.fileName }));
        onExportComplete();
      } else {
        setErrors([result.errorMessage || t('rs.exportFailed', 'L\'export a échoué')]);
      }
    } catch (err: any) {
      setErrors([err.message]);
    } finally {
      setExporting(false);
    }
  };

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent className="max-w-lg">
        <DialogHeader>
          <DialogTitle className="flex items-center gap-2">
            <FileDown className="h-5 w-5" />
            {t('rs.exportTEJTitle', 'Exporter fichier TEJ XML')}
          </DialogTitle>
          <DialogDescription>
            {t('rs.exportTEJDescription', 'Générer un fichier XML conforme TEJ pour l\'administration fiscale tunisienne')}
          </DialogDescription>
        </DialogHeader>

        {errors.length > 0 && (
          <Alert variant="destructive">
            <AlertCircle className="h-4 w-4" />
            <AlertDescription>
              <ul className="list-disc pl-4 text-sm">
                {errors.map((e, i) => <li key={i}>{e}</li>)}
              </ul>
            </AlertDescription>
          </Alert>
        )}

        {exportResult?.status === 'success' && (
          <Alert className="border-primary/30 bg-primary/5">
            <CheckCircle2 className="h-4 w-4 text-primary" />
            <AlertDescription className="text-primary">
              {t('rs.exportComplete', 'Le fichier TEJ XML a été généré et enregistré comme document avec succès !')}
              {exportResult.documentId && (
                <p className="text-xs mt-1">{t('rs.documentSaved', 'Document enregistré (ID: {{id}})', { id: exportResult.documentId })}</p>
              )}
            </AlertDescription>
          </Alert>
        )}

        <div className="space-y-4">
          {/* Period Selection */}
          <div className="grid grid-cols-2 gap-4">
            <div className="space-y-2">
              <Label>{t('rs.month', 'Mois')}</Label>
              <Select value={month} onValueChange={(v) => { setMonth(v); setTimeout(loadStats, 100); }}>
                <SelectTrigger><SelectValue /></SelectTrigger>
                <SelectContent>
                  {MONTHS_FR.map((m, i) => (
                    <SelectItem key={i} value={String(i + 1)}>{m}</SelectItem>
                  ))}
                </SelectContent>
              </Select>
            </div>
            <div className="space-y-2">
              <Label>{t('rs.year', 'Année')}</Label>
              <Input type="number" value={year} onChange={e => { setYear(e.target.value); setTimeout(loadStats, 100); }} min="2020" max="2030" />
            </div>
          </div>

          {/* Summary */}
          <Card className="bg-muted/50">
            <CardContent className="p-4">
              <div className="grid grid-cols-2 gap-4 text-center">
                <div>
                  <p className="text-xs text-muted-foreground">{t('rs.pendingExport', 'En attente d\'export')}</p>
                  <p className="text-lg font-bold text-warning">{recordCount}</p>
                </div>
                <div>
                  <p className="text-xs text-muted-foreground">{t('rs.totalRSForPeriod', 'Total RS pour la période')}</p>
                  <p className="text-lg font-bold text-destructive">{totalRS.toFixed(2)}</p>
                </div>
              </div>
            </CardContent>
          </Card>

          {/* Declarant Info */}
          <div className="space-y-3">
            <h4 className="text-sm font-semibold">{t('rs.declarantInfo', 'Informations du déclarant')}</h4>
            <div className="grid grid-cols-2 gap-3">
              <div className="space-y-1">
                <Label className="text-xs">{t('rs.companyName', 'Raison sociale')} *</Label>
                <Input value={payerName} onChange={e => setPayerName(e.target.value)} />
              </div>
              <div className="space-y-1">
                <Label className="text-xs">{t('rs.companyTaxId', 'Matricule Fiscal')} *</Label>
                <Input value={payerTaxId} onChange={e => setPayerTaxId(e.target.value)} />
              </div>
            </div>
            <div className="space-y-1">
              <Label className="text-xs">{t('rs.companyAddress', 'Adresse')}</Label>
              <Input value={payerAddress} onChange={e => setPayerAddress(e.target.value)} />
            </div>
          </div>
        </div>

        <DialogFooter>
          <Button variant="outline" onClick={() => onOpenChange(false)} disabled={exporting}>
            {t('close', 'Fermer')}
          </Button>
          <Button onClick={handleExport} disabled={exporting || recordCount === 0} className="gap-2">
            {exporting ? <Loader2 className="h-4 w-4 animate-spin" /> : <FileDown className="h-4 w-4" />}
            {t('rs.generateXML', 'Générer XML TEJ')}
          </Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  );
}
