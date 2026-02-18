// TEJ XML Generator for Tunisian Tax Authority
import type { RSRecord, TEJDeclarant, TEJExportLog } from '../types/retenue-source';
import { saveTEJExportLog } from '../services/rsCalculationService';

/**
 * Escape XML special characters
 */
function escapeXml(str: string): string {
  return str
    .replace(/&/g, '&amp;')
    .replace(/</g, '&lt;')
    .replace(/>/g, '&gt;')
    .replace(/"/g, '&quot;')
    .replace(/'/g, '&apos;');
}

/**
 * Format number to 2 decimal places for XML
 */
function formatAmount(amount: number): string {
  return amount.toFixed(2);
}

/**
 * Generate TEJ-compliant XML from RS records
 */
export function generateTEJXml(
  declarant: TEJDeclarant,
  records: RSRecord[],
  year: number,
  month: number
): string {
  const beneficiariesXml = records.map(record => `
        <Beneficiary>
            <Name>${escapeXml(record.supplierName)}</Name>
            <TaxID>${escapeXml(record.supplierTaxId)}</TaxID>
            <Address>${escapeXml(record.supplierAddress || '')}</Address>
            <InvoiceNumber>${escapeXml(record.invoiceNumber)}</InvoiceNumber>
            <InvoiceDate>${record.invoiceDate}</InvoiceDate>
            <PaymentDate>${record.paymentDate}</PaymentDate>
            <PaymentAmount>${formatAmount(record.amountPaid)}</PaymentAmount>
            <RSAmount>${formatAmount(record.rsAmount)}</RSAmount>
            <RSTypeCode>${record.rsTypeCode}</RSTypeCode>
        </Beneficiary>`
  ).join('');

  return `<?xml version="1.0" encoding="UTF-8"?>
<Declaration>
    <Declarant>
        <Name>${escapeXml(declarant.name)}</Name>
        <TaxID>${escapeXml(declarant.taxId)}</TaxID>
        <Address>${escapeXml(declarant.address)}</Address>
    </Declarant>
    <Period>
        <Year>${year}</Year>
        <Month>${String(month).padStart(2, '0')}</Month>
    </Period>
    <Beneficiaries>${beneficiariesXml}
    </Beneficiaries>
</Declaration>`;
}

/**
 * Generate TEJ file name per convention:
 * [MatriculeFiscal]-[Year]-[Month]-[TypeActe].xml
 */
export function generateTEJFileName(
  payerTaxId: string,
  year: number,
  month: number,
  typeActe: string = '0'
): string {
  return `${payerTaxId}-${year}-${String(month).padStart(2, '0')}-${typeActe}.xml`;
}

/**
 * Validate records before TEJ export
 */
export function validateForTEJExport(records: RSRecord[]): string[] {
  const errors: string[] = [];

  if (records.length === 0) {
    errors.push('No RS records to export');
    return errors;
  }

  records.forEach((record, idx) => {
    const prefix = `Record #${idx + 1}`;
    if (!record.supplierTaxId) errors.push(`${prefix}: Missing supplier Tax ID`);
    if (!record.supplierName) errors.push(`${prefix}: Missing supplier name`);
    if (!record.invoiceNumber) errors.push(`${prefix}: Missing invoice number`);
    if (!record.paymentDate) errors.push(`${prefix}: Missing payment date`);
    if (record.rsAmount <= 0) errors.push(`${prefix}: RS amount must be positive`);
    if (record.rsAmount > record.invoiceAmount) errors.push(`${prefix}: RS amount exceeds invoice amount`);
  });

  // Check for duplicates
  const keys = records.map(r => `${r.invoiceNumber}-${r.paymentDate}`);
  const duplicates = keys.filter((k, i) => keys.indexOf(k) !== i);
  if (duplicates.length > 0) {
    errors.push(`Duplicate records found for: ${[...new Set(duplicates)].join(', ')}`);
  }

  return errors;
}

/**
 * Full TEJ export workflow: validate, generate XML, download, log
 */
export function exportTEJFile(
  declarant: TEJDeclarant,
  records: RSRecord[],
  year: number,
  month: number
): { success: boolean; fileName?: string; errors?: string[] } {
  // Validate
  const errors = validateForTEJExport(records);
  if (errors.length > 0) {
    return { success: false, errors };
  }

  // Generate XML
  const xml = generateTEJXml(declarant, records, year, month);
  const fileName = generateTEJFileName(declarant.taxId, year, month);

  // Download file
  const blob = new Blob([xml], { type: 'application/xml;charset=utf-8' });
  const url = URL.createObjectURL(blob);
  const a = document.createElement('a');
  a.href = url;
  a.download = fileName;
  document.body.appendChild(a);
  a.click();
  document.body.removeChild(a);
  URL.revokeObjectURL(url);

  // Log export
  const log: TEJExportLog = {
    id: `TEJ-${Date.now()}`,
    fileName,
    exportDate: new Date().toISOString(),
    exportedBy: 'current-user',
    month,
    year,
    recordCount: records.length,
    totalRSAmount: records.reduce((sum, r) => sum + r.rsAmount, 0),
    status: 'success',
  };
  saveTEJExportLog(log);

  return { success: true, fileName };
}
