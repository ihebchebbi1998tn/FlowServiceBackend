// RS Record Add/Edit Modal - Professional UI with sections
import { useState, useEffect } from 'react';
import { useTranslation } from 'react-i18next';
import { Dialog, DialogContent, DialogHeader, DialogTitle, DialogFooter, DialogDescription } from '@/components/ui/dialog';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '@/components/ui/select';
import { Textarea } from '@/components/ui/textarea';
import { Alert, AlertDescription } from '@/components/ui/alert';
import { Separator } from '@/components/ui/separator';
import { AlertCircle, Calculator, Receipt, Loader2, Building2, User, FileText, ArrowRight } from 'lucide-react';
import { toast } from 'sonner';
import {
  createRSRecord,
  updateRSRecord,
  type RSRecordDto,
  type CreateRSRecordDto,
  type UpdateRSRecordDto,
} from '@/modules/shared/services/rsApiService';

const RS_TYPES = [
  { code: '10', rate: 10 },
  { code: '05', rate: 0.5 },
  { code: '03', rate: 3 },
  { code: '20', rate: 20 },
];

function calcRS(amountPaid: number, typeCode: string): number {
  const type = RS_TYPES.find(t => t.code === typeCode);
  if (!type) return 0;
  return Math.round(amountPaid * (type.rate / 100) * 100) / 100;
}

interface RSRecordModalProps {
  open: boolean;
  onOpenChange: (open: boolean) => void;
  entityType: 'offer' | 'sale';
  entityId: string;
  entityNumber?: string;
  entityAmount: number;
  contactName?: string;
  contactTaxId?: string;
  contactAddress?: string;
  editRecord?: RSRecordDto | null;
  onSuccess: () => void;
}

export function RSRecordModal({
  open,
  onOpenChange,
  entityType,
  entityId,
  entityNumber,
  entityAmount,
  contactName = '',
  contactTaxId = '',
  contactAddress = '',
  editRecord,
  onSuccess,
}: RSRecordModalProps) {
  const { t } = useTranslation();

  const [invoiceNumber, setInvoiceNumber] = useState('');
  const [invoiceDate, setInvoiceDate] = useState('');
  const [invoiceAmount, setInvoiceAmount] = useState(entityAmount);
  const [paymentDate, setPaymentDate] = useState('');
  const [amountPaid, setAmountPaid] = useState(entityAmount);
  const [rsTypeCode, setRsTypeCode] = useState('10');
  const [supplierName, setSupplierName] = useState(contactName);
  const [supplierTaxId, setSupplierTaxId] = useState(contactTaxId);
  const [supplierAddress, setSupplierAddress] = useState(contactAddress);
  const [payerName, setPayerName] = useState('');
  const [payerTaxId, setPayerTaxId] = useState('');
  const [payerAddress, setPayerAddress] = useState('');
  const [notes, setNotes] = useState('');
  const [errors, setErrors] = useState<string[]>([]);
  const [saving, setSaving] = useState(false);

  useEffect(() => {
    if (editRecord) {
      setInvoiceNumber(editRecord.invoiceNumber);
      setInvoiceDate(editRecord.invoiceDate?.split('T')[0] || '');
      setInvoiceAmount(editRecord.invoiceAmount);
      setPaymentDate(editRecord.paymentDate?.split('T')[0] || '');
      setAmountPaid(editRecord.amountPaid);
      setRsTypeCode(editRecord.rsTypeCode);
      setSupplierName(editRecord.supplierName);
      setSupplierTaxId(editRecord.supplierTaxId);
      setSupplierAddress(editRecord.supplierAddress || '');
      setPayerName(editRecord.payerName);
      setPayerTaxId(editRecord.payerTaxId);
      setPayerAddress(editRecord.payerAddress || '');
      setNotes(editRecord.notes || '');
    } else {
      setInvoiceNumber(entityNumber || '');
      setInvoiceDate(new Date().toISOString().split('T')[0]);
      setPaymentDate(new Date().toISOString().split('T')[0]);
      setInvoiceAmount(entityAmount);
      setAmountPaid(entityAmount);
      setSupplierName(contactName);
      setSupplierTaxId(contactTaxId);
      setSupplierAddress(contactAddress);
      setPayerName('');
      setPayerTaxId('');
      setPayerAddress('');
      setNotes('');
      setErrors([]);
    }
  }, [editRecord, entityNumber, entityAmount, contactName, contactTaxId, contactAddress, open]);

  const rsAmount = calcRS(amountPaid, rsTypeCode);
  const netPayment = Math.round((amountPaid - rsAmount) * 100) / 100;
  const rsRate = RS_TYPES.find(t => t.code === rsTypeCode)?.rate ?? 0;

  const handleSubmit = async () => {
    setErrors([]);
    const validationErrors: string[] = [];
    if (!invoiceNumber.trim()) validationErrors.push(t('rs.err.invoiceRequired', 'Le numéro de facture est requis'));
    if (!supplierTaxId.trim()) validationErrors.push(t('rs.err.taxIdRequired', 'Le matricule fiscal du fournisseur est requis'));
    if (!supplierName.trim()) validationErrors.push(t('rs.err.supplierRequired', 'Le nom du fournisseur est requis'));
    if (!payerName.trim()) validationErrors.push(t('rs.err.payerRequired', 'Le nom du payeur est requis'));
    if (!payerTaxId.trim()) validationErrors.push(t('rs.err.payerTaxIdRequired', 'Le matricule fiscal du payeur est requis'));
    if (invoiceAmount <= 0) validationErrors.push(t('rs.err.invoiceAmountPositive', 'Le montant de la facture doit être positif'));
    if (amountPaid <= 0) validationErrors.push(t('rs.err.amountPaidPositive', 'Le montant payé doit être positif'));

    if (validationErrors.length > 0) {
      setErrors(validationErrors);
      return;
    }

    setSaving(true);
    try {
      if (editRecord) {
        const updateDto: UpdateRSRecordDto = {
          invoiceNumber,
          invoiceDate: `${invoiceDate}T00:00:00Z`,
          invoiceAmount,
          paymentDate: `${paymentDate}T00:00:00Z`,
          amountPaid,
          rsTypeCode,
          supplierName,
          supplierTaxId,
          supplierAddress: supplierAddress || undefined,
          payerName,
          payerTaxId,
          payerAddress: payerAddress || undefined,
          notes: notes || undefined,
        };
        await updateRSRecord(editRecord.id, updateDto);
        toast.success(t('rs.recordUpdated', 'Enregistrement RS mis à jour'));
      } else {
        const createDto: CreateRSRecordDto = {
          entityType,
          entityId: Number(entityId),
          entityNumber,
          invoiceNumber,
          invoiceDate: `${invoiceDate}T00:00:00Z`,
          invoiceAmount,
          paymentDate: `${paymentDate}T00:00:00Z`,
          amountPaid,
          rsTypeCode,
          supplierName,
          supplierTaxId,
          supplierAddress: supplierAddress || undefined,
          payerName,
          payerTaxId,
          payerAddress: payerAddress || undefined,
          notes: notes || undefined,
        };
        await createRSRecord(createDto);
        toast.success(t('rs.recordCreated', 'Enregistrement RS créé'));
      }
      onSuccess();
      onOpenChange(false);
    } catch (err: any) {
      setErrors([err.message]);
    } finally {
      setSaving(false);
    }
  };

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent className="max-w-3xl max-h-[90vh] overflow-y-auto p-0">
        <DialogHeader className="px-6 pt-6 pb-4">
          <DialogTitle className="flex items-center gap-2 text-lg">
            <div className="flex items-center justify-center h-8 w-8 rounded-lg bg-primary/10">
              <Receipt className="h-4 w-4 text-primary" />
            </div>
            {editRecord
              ? t('rs.editRecord', 'Modifier l\'enregistrement RS')
              : t('rs.addRecord', 'Ajouter Retenue à la Source')}
          </DialogTitle>
          <DialogDescription>
            {t('rs.modalDescription', 'Calculer et enregistrer la retenue à la source pour ce document')}
          </DialogDescription>
        </DialogHeader>

        {errors.length > 0 && (
          <div className="px-6">
            <Alert variant="destructive">
              <AlertCircle className="h-4 w-4" />
              <AlertDescription>
                <ul className="list-disc pl-4 text-sm">
                  {errors.map((err, i) => <li key={i}>{err}</li>)}
                </ul>
              </AlertDescription>
            </Alert>
          </div>
        )}

        <div className="px-6 pb-6 space-y-6">
          {/* ── Calculation Preview ── */}
          <div className="rounded-xl border border-primary/20 bg-primary/5 p-5">
            <div className="flex items-center gap-2 mb-4">
              <Calculator className="h-4 w-4 text-primary" />
              <span className="text-sm font-semibold text-primary">{t('rs.calculationPreview', 'Aperçu du calcul')}</span>
            </div>
            <div className="grid grid-cols-2 gap-4 mb-4">
              <div className="space-y-1.5">
                <Label className="text-xs text-muted-foreground">{t('rs.rsType', 'Type RS')}</Label>
                <Select value={rsTypeCode} onValueChange={setRsTypeCode}>
                  <SelectTrigger className="bg-background"><SelectValue /></SelectTrigger>
                  <SelectContent>
                    {RS_TYPES.map(rsType => (
                      <SelectItem key={rsType.code} value={rsType.code}>
                        {t(`rs.type${rsType.code}`, rsType.code)} ({rsType.rate}%)
                      </SelectItem>
                    ))}
                  </SelectContent>
                </Select>
              </div>
              <div className="space-y-1.5">
                <Label className="text-xs text-muted-foreground">{t('rs.amountPaid', 'Montant payé')}</Label>
                <Input type="number" step="0.01" min="0" value={amountPaid} onChange={e => setAmountPaid(Number(e.target.value))} className="bg-background" />
              </div>
            </div>
            <div className="grid grid-cols-3 gap-3">
              <div className="rounded-lg bg-background p-3 text-center border border-border/50">
                <p className="text-[10px] uppercase tracking-wider text-muted-foreground mb-1">{t('rs.rate', 'Taux')}</p>
                <p className="text-xl font-bold text-primary">{rsRate}%</p>
              </div>
              <div className="rounded-lg bg-background p-3 text-center border border-destructive/20">
                <p className="text-[10px] uppercase tracking-wider text-muted-foreground mb-1">{t('rs.rsAmount', 'Montant RS')}</p>
                <p className="text-xl font-bold text-destructive">{rsAmount.toFixed(2)}</p>
              </div>
              <div className="rounded-lg bg-background p-3 text-center border border-border/50">
                <p className="text-[10px] uppercase tracking-wider text-muted-foreground mb-1">{t('rs.netPayment', 'Paiement net')}</p>
                <p className="text-xl font-bold text-foreground">{netPayment.toFixed(2)}</p>
              </div>
            </div>
          </div>

          <Separator />

          {/* ── Invoice & Payment Info ── */}
          <div>
            <div className="flex items-center gap-2 mb-3">
              <FileText className="h-4 w-4 text-muted-foreground" />
              <h4 className="text-sm font-semibold">{t('rs.invoicePaymentInfo', 'Informations facture & paiement')}</h4>
            </div>
            <div className="grid grid-cols-2 gap-x-4 gap-y-3">
              <div className="space-y-1.5">
                <Label className="text-xs">{t('rs.invoiceNumber', 'N° Facture')} <span className="text-destructive">*</span></Label>
                <Input value={invoiceNumber} onChange={e => setInvoiceNumber(e.target.value)} placeholder="INV-2026-001" />
              </div>
              <div className="space-y-1.5">
                <Label className="text-xs">{t('rs.invoiceDate', 'Date facture')} <span className="text-destructive">*</span></Label>
                <Input type="date" value={invoiceDate} onChange={e => setInvoiceDate(e.target.value)} />
              </div>
              <div className="space-y-1.5">
                <Label className="text-xs">{t('rs.invoiceAmount', 'Montant facture')} <span className="text-destructive">*</span></Label>
                <Input type="number" step="0.01" min="0" value={invoiceAmount} onChange={e => setInvoiceAmount(Number(e.target.value))} />
              </div>
              <div className="space-y-1.5">
                <Label className="text-xs">{t('rs.paymentDate', 'Date paiement')} <span className="text-destructive">*</span></Label>
                <Input type="date" value={paymentDate} onChange={e => setPaymentDate(e.target.value)} />
              </div>
            </div>
          </div>

          <Separator />

          {/* ── Supplier & Payer Side by Side ── */}
          <div className="grid grid-cols-1 md:grid-cols-2 gap-6">
            {/* Supplier (Beneficiary) */}
            <div className="space-y-3">
              <div className="flex items-center gap-2">
                <div className="flex items-center justify-center h-6 w-6 rounded bg-muted">
                  <User className="h-3.5 w-3.5 text-muted-foreground" />
                </div>
                <h4 className="text-sm font-semibold">{t('rs.supplier', 'Bénéficiaire')}</h4>
              </div>
              <div className="space-y-2.5 pl-8">
                <div className="space-y-1">
                  <Label className="text-xs">{t('rs.supplierName', 'Nom')} <span className="text-destructive">*</span></Label>
                  <Input value={supplierName} onChange={e => setSupplierName(e.target.value)} />
                </div>
                <div className="space-y-1">
                  <Label className="text-xs">{t('rs.supplierTaxId', 'Matricule Fiscal')} <span className="text-destructive">*</span></Label>
                  <Input value={supplierTaxId} onChange={e => setSupplierTaxId(e.target.value)} placeholder="12345678A/M/000" />
                </div>
                <div className="space-y-1">
                  <Label className="text-xs">{t('rs.supplierAddress', 'Adresse')}</Label>
                  <Input value={supplierAddress} onChange={e => setSupplierAddress(e.target.value)} />
                </div>
              </div>
            </div>

            {/* Payer (Declarant) */}
            <div className="space-y-3">
              <div className="flex items-center gap-2">
                <div className="flex items-center justify-center h-6 w-6 rounded bg-muted">
                  <Building2 className="h-3.5 w-3.5 text-muted-foreground" />
                </div>
                <h4 className="text-sm font-semibold">{t('rs.payer', 'Déclarant')}</h4>
              </div>
              <div className="space-y-2.5 pl-8">
                <div className="space-y-1">
                  <Label className="text-xs">{t('rs.payerName', 'Raison sociale')} <span className="text-destructive">*</span></Label>
                  <Input value={payerName} onChange={e => setPayerName(e.target.value)} />
                </div>
                <div className="space-y-1">
                  <Label className="text-xs">{t('rs.payerTaxId', 'Matricule Fiscal')} <span className="text-destructive">*</span></Label>
                  <Input value={payerTaxId} onChange={e => setPayerTaxId(e.target.value)} placeholder="0009876L/A/M/000" />
                </div>
                <div className="space-y-1">
                  <Label className="text-xs">{t('rs.payerAddress', 'Adresse')}</Label>
                  <Input value={payerAddress} onChange={e => setPayerAddress(e.target.value)} />
                </div>
              </div>
            </div>
          </div>

          {/* ── Notes ── */}
          <div className="space-y-1.5">
            <Label className="text-xs">{t('rs.notes', 'Notes')}</Label>
            <Textarea value={notes} onChange={e => setNotes(e.target.value)} rows={2} placeholder={t('rs.notesPlaceholder', 'Remarques optionnelles...')} />
          </div>
        </div>

        <Separator />

        <DialogFooter className="px-6 py-4">
          <Button variant="outline" onClick={() => onOpenChange(false)} disabled={saving}>
            {t('cancel', 'Annuler')}
          </Button>
          <Button onClick={handleSubmit} disabled={saving} className="gap-2">
            {saving ? <Loader2 className="h-4 w-4 animate-spin" /> : <ArrowRight className="h-4 w-4" />}
            {editRecord ? t('rs.update', 'Mettre à jour') : t('rs.create', 'Enregistrer RS')}
          </Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  );
}
