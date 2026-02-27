import { useTranslation } from "react-i18next";
import { useNavigate } from "react-router-dom";
import { useState, useEffect } from "react";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { Button } from "@/components/ui/button";
import { Badge } from "@/components/ui/badge";
import { Separator } from "@/components/ui/separator";
import { ExternalLink, FileText } from "lucide-react";
import { Sale } from "../../types";
import { useCurrency } from '@/shared/hooks/useCurrency';
import { offersApi } from '@/services/api/offersApi';
import { getStatusColorClass } from "@/config/entity-statuses";

interface OverviewTabProps {
  sale: Sale;
}

export function OverviewTab({ sale }: OverviewTabProps) {
  const { t } = useTranslation();
  const navigate = useNavigate();
  const { format: formatCurrency } = useCurrency();
  const [fetchedOfferNumber, setFetchedOfferNumber] = useState<string | null>(null);

  useEffect(() => {
    const fetchOfferNumber = async () => {
      if (sale.offerId && !sale.offerNumber) {
        try {
          const numId = parseInt(sale.offerId, 10);
          if (!isNaN(numId)) {
            const offer = await offersApi.getById(numId);
            if (offer?.offerNumber) {
              setFetchedOfferNumber(offer.offerNumber);
            }
          }
        } catch (error) {
          console.error('Failed to fetch offer number:', error);
        }
      }
    };
    fetchOfferNumber();
  }, [sale.offerId, sale.offerNumber]);

  const displayOfferNumber = sale.offerNumber || fetchedOfferNumber;

  const getStatusColor = (status: string) => getStatusColorClass('sale', status);

  const formatDate = (date: Date | string) => new Date(date).toLocaleDateString();

  const totalItemsValue = sale.items.reduce((sum, item) => sum + item.totalPrice, 0);
  
  const calculateTaxAmount = () => {
    if (!sale.taxes || sale.taxes === 0) return 0;
    if (sale.taxType === 'percentage') return totalItemsValue * (sale.taxes / 100);
    return sale.taxes;
  };
  
  const calculateDiscountAmount = () => {
    if (!sale.discount || sale.discount === 0) return 0;
    if (sale.discountType === 'percentage') return totalItemsValue * (sale.discount / 100);
    return sale.discount;
  };
  
  const taxAmount = calculateTaxAmount();
  const discountAmount = calculateDiscountAmount();
  const fiscalStampAmount = sale.fiscalStamp || 0;
  const calculatedTotal = totalItemsValue + taxAmount - discountAmount + fiscalStampAmount;
  
  const hasServices = sale.items.some(item => item.type === 'service');
  const hasServiceOrder = !!sale.convertedToServiceOrderId;

  return (
    <div className="space-y-4 sm:space-y-6">
      <Card>
        <CardHeader className="pb-4">
          <CardTitle className="text-sm font-medium text-foreground flex items-center gap-2">
            <FileText className="h-4 w-4 text-primary" />
            {t('overview.saleDetails')}
          </CardTitle>
        </CardHeader>
        <CardContent className="space-y-6">
          <div className="grid grid-cols-1 lg:grid-cols-2 gap-6">
            {/* Left Column */}
            <div className="space-y-4">
              <DetailField label={t('overview.saleId')} value={sale.id} />
              <DetailField label={t('overview.saleTitle')} value={sale.title} />
              <DetailField label={t('overview.description')} value={sale.description || t('overview.noDescription')} />

              <div>
                <span className="text-sm text-muted-foreground">{t('overview.affectedContact')}</span>
                <div className="mt-1">
                  <Button 
                    variant="link" 
                    className="p-0 h-auto text-sm text-primary hover:underline inline-flex items-center max-w-full truncate"
                    onClick={() => navigate(`/dashboard/contacts/${sale.contactId}`)}
                  >
                    <span className="truncate">{sale.contactName || sale.contactCompany || t('overview.unknownContact')}</span>
                    <ExternalLink className="ml-1.5 h-3 w-3 shrink-0" />
                  </Button>
                </div>
              </div>

              <DetailField label={t('overview.contactEmail')} value={sale.contactEmail || t('overview.notSpecified')} />

              {hasServices && (
                <div>
                  <span className="text-sm text-muted-foreground">{t('overview.relatedServiceOrder')}</span>
                  <div className="mt-1">
                    {hasServiceOrder ? (
                      <Button 
                        variant="link" 
                        className="p-0 h-auto text-sm text-primary hover:underline inline-flex items-center max-w-full truncate"
                        onClick={() => navigate(`/dashboard/field/service-orders/${sale.convertedToServiceOrderId}`)}
                      >
                        <span className="truncate">Service Order #{sale.convertedToServiceOrderId}</span>
                        <ExternalLink className="ml-1.5 h-3 w-3 shrink-0" />
                      </Button>
                    ) : (
                      <span className="text-sm text-muted-foreground">{t('overview.notConvertedYet')}</span>
                    )}
                  </div>
                </div>
              )}
            </div>

            {/* Right Column */}
            <div className="space-y-4">
              <div>
                <span className="text-sm text-muted-foreground">{t('overview.saleAmount')}</span>
                <p className="text-sm text-foreground mt-1">
                  {discountAmount > 0 && (
                    <span className="text-muted-foreground line-through mr-2">
                      {formatCurrency(totalItemsValue + taxAmount + fiscalStampAmount)}
                    </span>
                  )}
                  {formatCurrency(calculatedTotal)}
                  {taxAmount > 0 && (
                    <span className="text-muted-foreground ml-2">({t('overview.inclTva')})</span>
                  )}
                </p>
                {discountAmount > 0 && (
                  <p className="text-sm text-success mt-0.5">
                    -{formatCurrency(discountAmount)} {t('discount')}
                    {sale.discountType === 'percentage' && ` (${sale.discount}%)`}
                  </p>
                )}
              </div>

              <div>
                <span className="text-sm text-muted-foreground">{t('overview.currentStatus')}</span>
                <div className="mt-1">
                  <Badge className={`${getStatusColor(sale.status)}`}>
                    {t(sale.status)}
                  </Badge>
                </div>
              </div>

              <DetailField label={t('overview.createdBy')} value={sale.createdByName || t('overview.unknown')} />
              <DetailField label={t('overview.closeDate')} value={sale.actualCloseDate ? formatDate(sale.actualCloseDate) : t('overview.notSet')} />

              {sale.offerId && (
                <div>
                  <span className="text-sm text-muted-foreground">{t('overview.relatedOffer')}</span>
                  <div className="mt-1">
                    <Button 
                      variant="link" 
                      className="p-0 h-auto text-sm text-primary hover:underline inline-flex items-center max-w-full truncate"
                      onClick={() => navigate(`/dashboard/offers/${sale.offerId}`)}
                    >
                      <span className="truncate">{displayOfferNumber || `#${sale.offerId}`}</span>
                      <ExternalLink className="ml-1.5 h-3 w-3 shrink-0" />
                    </Button>
                  </div>
                </div>
              )}
            </div>
          </div>

          {/* Items & Financial Summary */}
          <div className="border-t pt-6">
            <div className="rounded-lg border border-border p-4 space-y-3">
              <div className="flex items-center justify-between">
                <span className="text-sm font-medium text-foreground">{t('overview.itemsSummary')}</span>
                <span className="text-sm text-muted-foreground">{t('overview.itemsCount', { count: sale.items.length })}</span>
              </div>
              
              <div className="grid grid-cols-2 gap-4">
                <div className="flex justify-between text-sm">
                  <span className="text-muted-foreground">{t('overview.services')}:</span>
                  <span className="text-foreground">{sale.items.filter(item => item.type === 'service').length}</span>
                </div>
                <div className="flex justify-between text-sm">
                  <span className="text-muted-foreground">{t('overview.articles')}:</span>
                  <span className="text-foreground">{sale.items.filter(item => item.type === 'article').length}</span>
                </div>
              </div>

              <Separator />
              
              <div className="space-y-2">
                <div className="flex justify-between text-sm">
                  <span className="text-muted-foreground">{t('overview.itemsTotalValue')}:</span>
                  <span className="text-foreground">{formatCurrency(totalItemsValue)}</span>
                </div>
                {taxAmount > 0 && (
                  <div className="flex justify-between text-sm">
                    <span className="text-muted-foreground">
                      {t('overview.tva')} {sale.taxType === 'percentage' ? `(${sale.taxes}%)` : ''}:
                    </span>
                    <span className="text-foreground">+{formatCurrency(taxAmount)}</span>
                  </div>
                )}
                {(sale.discount ?? 0) > 0 && (
                  <div className="flex justify-between text-sm">
                    <span className="text-muted-foreground">
                      {t('discount')} {sale.discountType === 'percentage' ? `(${sale.discount}%)` : ''}:
                    </span>
                    <span className="text-destructive">-{formatCurrency(discountAmount)}</span>
                  </div>
                )}
                {fiscalStampAmount > 0 && (
                  <div className="flex justify-between text-sm">
                    <span className="text-muted-foreground">{t('fiscalStamp')}:</span>
                    <span className="text-foreground">+{formatCurrency(fiscalStampAmount)}</span>
                  </div>
                )}
                <Separator />
                <div className="flex justify-between text-sm font-medium">
                  <span className="text-foreground">{t('overview.totalAmount')}:</span>
                  <span className="text-foreground">{formatCurrency(sale.totalAmount || calculatedTotal)}</span>
                </div>
              </div>
            </div>
          </div>
        </CardContent>
      </Card>
    </div>
  );
}

function DetailField({ label, value }: { label: string; value: string }) {
  return (
    <div>
      <span className="text-sm text-muted-foreground">{label}</span>
      <p className="text-sm text-foreground mt-1">{value}</p>
    </div>
  );
}
