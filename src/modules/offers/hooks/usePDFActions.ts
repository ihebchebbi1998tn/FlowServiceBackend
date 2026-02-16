import { useState, useCallback } from 'react';
import { toast } from 'sonner';
import { PdfSettings } from '../utils/pdfSettings.utils';

interface UsePDFActionsProps {
  offer: any;
  formatCurrency: (amount: number) => string;
  pdfSettings: PdfSettings;
}

export const usePDFActions = ({ offer, formatCurrency, pdfSettings }: UsePDFActionsProps) => {
  const [isGenerating, setIsGenerating] = useState(false);

  const handlePrint = useCallback(async () => {
    try {
      setIsGenerating(true);
      
      // Create a temporary window for printing
      const printWindow = window.open('', '_blank');
      if (!printWindow) {
        throw new Error('Failed to open print window');
      }

      printWindow.document.write(`
        <html>
          <head>
            <title>Quote ${offer.id}</title>
            <style>
              body { font-family: Arial, sans-serif; margin: 0; padding: 20px; }
              .quote-header { border-bottom: 2px solid #3B82F6; padding-bottom: 20px; margin-bottom: 20px; }
              .quote-title { font-size: 24px; font-weight: bold; color: #3B82F6; }
              .quote-id { font-size: 18px; color: #6B7280; }
              .content { margin-bottom: 20px; }
              .items-table { width: 100%; border-collapse: collapse; }
              .items-table th, .items-table td { padding: 10px; text-align: left; border-bottom: 1px solid #E5E7EB; }
              .items-table th { background-color: #F3F4F6; font-weight: bold; }
            </style>
          </head>
          <body>
            <div class="quote-header">
              <div class="quote-title">QUOTE</div>
              <div class="quote-id">#${offer.id}</div>
            </div>
            <div class="content">
              <h3>${offer.title}</h3>
              <p><strong>Customer:</strong> ${offer.contactName}</p>
              <p><strong>Total:</strong> ${formatCurrency(offer.totalAmount || offer.amount)}</p>
            </div>
          </body>
        </html>
      `);
      
      printWindow.document.close();
      printWindow.print();
      printWindow.close();
      
      toast.success('Print dialog opened');
    } catch (error) {
      toast.error('Failed to print quote');
      console.error('Print error:', error);
    } finally {
      setIsGenerating(false);
    }
  }, [offer, formatCurrency]);

  const handleShare = useCallback(async (platform?: string) => {
    try {
      if (navigator.share) {
        await navigator.share({
          title: `Quote ${offer.id}`,
          text: `Quote for ${offer.title} - ${formatCurrency(offer.totalAmount || offer.amount)}`,
          url: window.location.href,
        });
        toast.success('Quote shared successfully');
      } else {
        // Fallback to copying URL to clipboard
        await navigator.clipboard.writeText(window.location.href);
        toast.success('Quote URL copied to clipboard');
      }
    } catch (error) {
      toast.error('Failed to share quote');
      console.error('Share error:', error);
    }
  }, [offer, formatCurrency]);

  const handleDownloadSuccess = useCallback(() => {
    toast.success('Quote PDF downloaded successfully');
  }, []);

  const handleDownloadError = useCallback(() => {
    toast.error('Failed to download quote PDF');
  }, []);

  return {
    isGenerating,
    handlePrint,
    handleShare,
    handleDownloadSuccess,
    handleDownloadError
  };
};