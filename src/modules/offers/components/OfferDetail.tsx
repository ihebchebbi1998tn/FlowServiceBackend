import { useState, useEffect } from "react";
import { calculateEntityTotal } from "@/lib/calculateTotal";
import { useParams, useNavigate, Link } from "react-router-dom";
import { useTranslation } from "react-i18next";
import { Button } from "@/components/ui/button";
import { Badge } from "@/components/ui/badge";
import { Tabs, TabsContent, TabsList, TabsTrigger } from "@/components/ui/tabs";
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from "@/components/ui/select";
import { useLayoutModeContext } from "@/hooks/useLayoutMode";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { Dialog, DialogContent, DialogHeader, DialogTitle, DialogDescription, DialogFooter } from "@/components/ui/dialog";
import { Alert, AlertDescription } from "@/components/ui/alert";
import { ArrowLeft, Edit, Trash2, FileDown, RefreshCw, Send, Check, X, ShoppingCart, Loader2, MoreVertical, Zap, Info, Receipt } from "lucide-react";
import { DropdownMenu, DropdownMenuContent, DropdownMenuItem, DropdownMenuSeparator, DropdownMenuTrigger } from "@/components/ui/dropdown-menu";
import { OffersService } from "../services/offers.service";
import { offersApi } from "@/services/api/offersApi";
import { entityFormDocumentsService } from "@/modules/shared/services/entityFormDocumentsService";
import { salesApi } from "@/services/api/salesApi";
import { Offer } from "../types";
import { useCurrency } from '@/shared/hooks/useCurrency';
import { toast } from "sonner";
import { OfferPDFPreviewModal } from "./OfferPDFPreviewModal";
import { OverviewTab } from "./tabs/OverviewTab";
import { ItemsTab } from "./tabs/ItemsTab";
import { NotesTab } from "./tabs/NotesTab";
import { ChecklistsTab } from "./tabs/ChecklistsTab";
import { DocumentsTab } from "./tabs/DocumentsTab";
import { PaymentsTab } from "@/modules/payments/components/PaymentsTab";
import { RSRecordModal } from "@/modules/shared/components/RSRecordModal";
import { TEJExportModal } from "@/modules/shared/components/TEJExportModal";
import { OfferStatusFlow, OfferStatus } from "./OfferStatusFlow";
import { SendOfferModal } from "./SendOfferModal";
import { useWorkflowStatus } from "@/modules/workflow/hooks/useWorkflowStatus";
import { getStatusColorClass, offerStatusConfig, getWorkflowStepIndex, getPositiveTerminalStatuses, getNegativeStatuses } from "@/config/entity-statuses";

export function OfferDetail() {
  const { id } = useParams<{ id: string }>();
  const navigate = useNavigate();
  const { t } = useTranslation('offers');
  const { format: formatCurrency } = useCurrency();
  const { isMobile } = useLayoutModeContext();
  const workflowStatus = useWorkflowStatus();
  const [offer, setOffer] = useState<Offer | null>(null);
  const [loading, setLoading] = useState(true);
  const [isPDFModalOpen, setIsPDFModalOpen] = useState(false);
  const [activeTab, setActiveTab] = useState("overview");
  const [isConverting, setIsConverting] = useState(false);
  const [showConvertDialog, setShowConvertDialog] = useState(false);
  const [showDeleteDialog, setShowDeleteDialog] = useState(false);
  const [showSendModal, setShowSendModal] = useState(false);
  const [showRSModal, setShowRSModal] = useState(false);
  const [showTEJExport, setShowTEJExport] = useState(false);
  const [actionLoading, setActionLoading] = useState<string | null>(null);

  useEffect(() => {
    const fetchOffer = async () => {
      if (!id) return;
      try {
        setLoading(true);
        const offerData = await OffersService.getOfferById(id);
        setOffer(offerData);
      } catch (error) {
        toast.error(t('failedToFetchOffer'));
      } finally {
        setLoading(false);
      }
    };
    fetchOffer();
  }, [id, t]);

  const handleSendOfferSuccess = async () => {
    if (!offer) return;
    try {
      await OffersService.updateOffer(offer.id, { status: 'sent' });
      setOffer({ ...offer, status: 'sent' });
    } catch (error) {
      console.error('Failed to update offer status after send:', error);
    }
  };

  const handleAcceptOffer = async () => {
    if (!offer) return;
    try {
      setActionLoading('accept');
      await OffersService.updateOffer(offer.id, { status: 'accepted' });
      setOffer({ ...offer, status: 'accepted' });
      toast.success(t('detail.offerAccepted'));
      
      // Only show convert dialog if workflow automation is NOT active
      // When workflow is active, conversion happens automatically via backend
      if (!workflowStatus.isActive) {
        setShowConvertDialog(true);
      } else {
        // Auto-conversion happens silently via workflow
      }
    } catch (error) {
      toast.error(t('detail.failedToAcceptOffer'));
    } finally {
      setActionLoading(null);
    }
  };

  const handleDeclineOffer = async () => {
    if (!offer) return;
    try {
      setActionLoading('decline');
      await OffersService.updateOffer(offer.id, { status: 'declined' });
      setOffer({ ...offer, status: 'declined' });
      toast.success(t('detail.offerDeclined'));
    } catch (error) {
      toast.error(t('detail.failedToDeclineOffer'));
    } finally {
      setActionLoading(null);
    }
  };

  const handleConvertToSale = async () => {
    if (!offer) return;
    
    if (offer.convertedToSaleId) {
      toast.info(t('detail.offerAlreadyConverted'));
      navigate(`/dashboard/sales/${offer.convertedToSaleId}`);
      return;
    }
    
    try {
      setIsConverting(true);
      const result = await OffersService.convertOffer({
        offerId: offer.id,
        convertToSale: true,
        convertToServiceOrder: false,
      });

      setShowConvertDialog(false);

      const saleId = result.saleId ? Number.parseInt(String(result.saleId), 10) : NaN;
      if (Number.isFinite(saleId) && saleId > 0) {
        const offerId = parseInt(offer.id, 10);
        
        if (!result.alreadyConverted) {
          try {
            await offersApi.addActivity(offerId, {
              type: 'conversion',
              description: `Offer converted to Sale #${saleId}`,
              details: `This offer was successfully converted to a sale on ${new Date().toLocaleDateString()}`,
            });
          } catch (e) {
            console.warn('Failed to log offer conversion activity:', e);
          }
          
          // Copy checklists from offer to sale
          try {
            await entityFormDocumentsService.copyToEntity('offer', offerId, 'sale', saleId);
          } catch (e) {
            console.warn('Failed to copy checklists from offer to sale:', e);
          }
          
          // Note: Backend already creates the activity with proper offer number
        }
        
        setOffer({
          ...offer,
          convertedToSaleId: String(saleId),
          convertedAt: new Date(),
          status: 'accepted',
        });

        if (result.alreadyConverted) {
          toast.info(t('detail.alreadyConvertedNavigating'));
        } else {
          toast.success(t('detail.offerConvertedSuccessfully'));
        }
        navigate(`/dashboard/sales/${saleId}`);
        return;
      }

      toast.error(t('detail.conversionFailed'));
      navigate('/dashboard/sales');
    } catch (error) {
      toast.error(t('detail.failedToConvertOffer'));
    } finally {
      setIsConverting(false);
    }
  };

  const handleRenewOffer = async () => {
    if (!offer) return;
    try {
      setActionLoading('renew');
      await OffersService.renewOffer(offer.id);
      toast.success(t('detail.offerRenewedSuccessfully'));
      navigate('/dashboard/offers');
    } catch (error) {
      toast.error(t('detail.failedToRenewOffer'));
    } finally {
      setActionLoading(null);
    }
  };

  const handleStatusFlowChange = async (newStatus: OfferStatus) => {
    if (!offer) return;

    // When moving to "sent", open the Send Offer modal instead of updating directly
    if (newStatus === 'sent') {
      setShowSendModal(true);
      return;
    }

    try {
      setActionLoading(newStatus);
      // StatusFlow now emits canonical config IDs directly — no mapping needed
      await OffersService.updateOffer(offer.id, { status: newStatus as any });
      
      // Refetch to get the actual backend state
      const updatedOffer = await OffersService.getOfferById(offer.id);
      if (updatedOffer) {
        setOffer(updatedOffer);
      }
      
      toast.success(t('detail.statusUpdatedTo', { status: t(`status.${newStatus}`) }));
      
      // Show convert dialog when positively terminal — ONLY if workflow is NOT active
      const positiveTerminals = getPositiveTerminalStatuses(offerStatusConfig);
      if (positiveTerminals.includes(newStatus) && !workflowStatus.isActive) {
        setShowConvertDialog(true);
      } else if (positiveTerminals.includes(newStatus) && workflowStatus.isActive) {
        // Auto-conversion happens silently via workflow
        const pollForSale = async (retries = 10, delay = 1000) => {
          for (let i = 0; i < retries; i++) {
            await new Promise(r => setTimeout(r, delay));
            const refreshed = await OffersService.getOfferById(offer.id);
            if (refreshed?.convertedToSaleId) {
              navigate(`/dashboard/sales/${refreshed.convertedToSaleId}`);
              return;
            }
          }
        };
        pollForSale();
      }
    } catch (error) {
      console.error('Failed to update offer status:', error);
      toast.error(t('detail.failedToUpdateStatus'));
    } finally {
      setActionLoading(null);
    }
  };

  const handleDeleteOffer = async () => {
    if (!offer) return;
    try {
      setActionLoading('delete');
      await OffersService.deleteOffer(offer.id);
      toast.success(t('detail.offerDeleted'));
      navigate('/dashboard/offers');
    } catch (error) {
      toast.error(t('detail.failedToDeleteOffer'));
    } finally {
      setActionLoading(null);
      setShowDeleteDialog(false);
    }
  };

  const getStatusBadge = (status: string) => {
    const colorClass = getStatusColorClass('offer', status);
    const label = t(`status.${status}`, { defaultValue: status });
    return <Badge className={`${colorClass} text-sm px-3 py-1 border`}>{label}</Badge>;
  };

  const getWorkflowStep = (status: string) => {
    const idx = getWorkflowStepIndex(offerStatusConfig, status);
    return idx === -1 ? 1 : idx + 1;
  };

  if (loading) {
    return (
      <div className="flex items-center justify-center min-h-[400px]">
        <div className="flex flex-col items-center gap-4">
          {/* Elegant pulsing loader */}
          <div className="relative">
            <div className="w-12 h-12 rounded-xl bg-primary/10 flex items-center justify-center">
              <div className="w-6 h-6 rounded-lg bg-primary/20 animate-pulse" />
            </div>
            <div className="absolute inset-0 rounded-xl bg-primary/5 animate-ping" style={{ animationDuration: '1.5s' }} />
          </div>
          <div className="flex flex-col items-center gap-1">
            <div className="h-1 w-32 bg-muted rounded-full overflow-hidden">
              <div className="h-full w-1/2 bg-primary/60 rounded-full animate-[shimmer_1.2s_ease-in-out_infinite]" 
                   style={{ transform: 'translateX(-100%)', animation: 'shimmer 1.2s ease-in-out infinite' }} />
            </div>
            <p className="text-sm text-muted-foreground mt-2">{t('detail.loading')}</p>
          </div>
        </div>
      </div>
    );
  }

  if (!offer) {
    return (
      <div className="flex flex-col items-center justify-center min-h-[400px] p-6">
        <h2 className="text-2xl font-semibold text-foreground mb-2">{t('offerNotFound')}</h2>
        <p className="text-muted-foreground mb-4">{t('offerNotFoundDescription', { id })}</p>
        <Button onClick={() => navigate('/dashboard/offers')} variant="outline">
          {t('backToOffers')}
        </Button>
      </div>
    );
  }

  const currentStep = getWorkflowStep(offer.status);

  return (
    <div className="min-h-screen bg-background">
      {/* Header - Compact Card Style */}
      <div className="border-b border-border bg-gradient-subtle backdrop-blur-sm sticky top-0 z-20 shadow-soft">
        <div className="p-4 lg:p-6">
          <div className="flex items-center gap-4">
            {/* Back Button */}
            <Button
              variant="ghost"
              size="icon"
              onClick={() => navigate('/dashboard/offers')}
              className="h-9 w-9 shrink-0 hover:bg-background/80"
            >
              <ArrowLeft className="h-4 w-4" />
            </Button>

            {/* Offer Info Card */}
            <Card className="flex-1 shadow-sm border-border/50">
              <CardContent className="p-4">
                <div className="flex items-center justify-between gap-6">
                  {/* Left: Title */}
                  <div className="flex items-center gap-6 min-w-0">
                    <h1 className="text-xl font-semibold text-foreground truncate">
                      {offer.title}
                    </h1>
                  </div>

                  {/* Right: Status Flow + Actions */}
                  <div className="flex items-center gap-4 shrink-0">
                    <OfferStatusFlow
                      currentStatus={offer.status}
                      onStatusChange={handleStatusFlowChange}
                      disabled={actionLoading !== null}
                      isUpdating={actionLoading !== null}
                    />
                    
                    <div className="h-8 w-px bg-border/50" />
                    
                    {/* Actions Dropdown */}
                    <DropdownMenu>
                      <DropdownMenuTrigger asChild>
                        <Button variant="outline" size="sm" className="gap-2">
                          <MoreVertical className="h-4 w-4" />
                          {t('actions', 'Actions')}
                        </Button>
                      </DropdownMenuTrigger>
                      <DropdownMenuContent align="end" className="w-48 bg-background border border-border">
                        <DropdownMenuItem onClick={() => setShowSendModal(true)} disabled={actionLoading === 'send'} className="gap-2 cursor-pointer">
                          {actionLoading === 'send' ? (
                            <Loader2 className="h-4 w-4 animate-spin" />
                          ) : (
                            <Send className="h-4 w-4" />
                          )}
                          {t('send_offer')}
                        </DropdownMenuItem>
                        <DropdownMenuItem onClick={() => setIsPDFModalOpen(true)} className="gap-2 cursor-pointer">
                          <FileDown className="h-4 w-4" />
                          {t('download_pdf')}
                        </DropdownMenuItem>
                        <DropdownMenuSeparator />
                        <DropdownMenuItem onClick={() => setShowRSModal(true)} className="gap-2 cursor-pointer">
                          <Receipt className="h-4 w-4" />
                          {t('rs.addRS', 'Retenue à la Source')}
                        </DropdownMenuItem>
                        <DropdownMenuItem onClick={() => setShowTEJExport(true)} className="gap-2 cursor-pointer">
                          <FileDown className="h-4 w-4" />
                          {t('rs.exportTEJ', 'Exporter TEJ XML')}
                        </DropdownMenuItem>
                        <DropdownMenuSeparator />
                        <DropdownMenuItem asChild className="gap-2 cursor-pointer">
                          <Link to={`/dashboard/offers/${offer.id}/edit`}>
                            <Edit className="h-4 w-4" />
                            {t('edit_offer')}
                          </Link>
                        </DropdownMenuItem>
                        {getPositiveTerminalStatuses(offerStatusConfig).includes(offer.status) && !offer.convertedToSaleId && !workflowStatus.isActive && (
                          <>
                            <DropdownMenuSeparator />
                            <DropdownMenuItem onClick={() => setShowConvertDialog(true)} className="gap-2 cursor-pointer text-primary">
                              <ShoppingCart className="h-4 w-4" />
                              {t('convert_to_sale')}
                            </DropdownMenuItem>
                          </>
                        )}
                        {getPositiveTerminalStatuses(offerStatusConfig).includes(offer.status) && workflowStatus.isActive && (
                          <>
                            <DropdownMenuSeparator />
                            <DropdownMenuItem disabled className="gap-2 cursor-not-allowed text-muted-foreground">
                              <Zap className="h-4 w-4" />
                              {t('detail.autoConversionEnabled', 'Auto-conversion enabled')}
                            </DropdownMenuItem>
                          </>
                        )}
                        {getNegativeStatuses(offerStatusConfig).includes(offer.status) && (
                          <>
                            <DropdownMenuSeparator />
                            <DropdownMenuItem onClick={handleRenewOffer} disabled={actionLoading === 'renew'} className="gap-2 cursor-pointer">
                              {actionLoading === 'renew' ? (
                                <Loader2 className="h-4 w-4 animate-spin" />
                              ) : (
                                <RefreshCw className="h-4 w-4" />
                              )}
                              {t('detail.renewOffer')}
                            </DropdownMenuItem>
                          </>
                        )}
                        <DropdownMenuSeparator />
                        <DropdownMenuItem 
                          onClick={() => setShowDeleteDialog(true)} 
                          className="gap-2 text-destructive focus:text-destructive cursor-pointer"
                        >
                          <Trash2 className="h-4 w-4" />
                          {t('delete')}
                        </DropdownMenuItem>
                      </DropdownMenuContent>
                    </DropdownMenu>
                  </div>
                </div>
              </CardContent>
            </Card>
          </div>
        </div>
      </div>

      <div className="px-4 py-6 space-y-6">

        <Tabs value={activeTab} onValueChange={setActiveTab}>
          <div className="w-full mb-6">
            {/* Mobile: Dropdown Select */}
            {isMobile ? (
              <Select value={activeTab} onValueChange={setActiveTab}>
                <SelectTrigger className="w-full bg-background">
                  <SelectValue>
                    {activeTab === 'overview' && t('tabs.overview')}
                    {activeTab === 'items' && t('tabs.items')}
                    {activeTab === 'payments' && t('payments:title', 'Payments')}
                    {activeTab === 'notes' && t('tabs.notes')}
                    {activeTab === 'checklists' && t('tabs.checklists')}
                    {activeTab === 'documents' && t('tabs.documents')}
                  </SelectValue>
                </SelectTrigger>
                <SelectContent className="bg-white dark:bg-card">
                  <SelectItem value="overview">{t('tabs.overview')}</SelectItem>
                  <SelectItem value="items">{t('tabs.items')}</SelectItem>
                  <SelectItem value="payments">{t('payments:title', 'Payments')}</SelectItem>
                  <SelectItem value="notes">{t('tabs.notes')}</SelectItem>
                  <SelectItem value="checklists">{t('tabs.checklists')}</SelectItem>
                  <SelectItem value="documents">{t('tabs.documents')}</SelectItem>
                </SelectContent>
              </Select>
            ) : (
              <TabsList className="w-full h-auto p-1 bg-muted/50 rounded-lg grid grid-cols-6">
                <TabsTrigger value="overview" className="px-4 py-2.5 text-sm font-medium">
                  {t('tabs.overview')}
                </TabsTrigger>
                <TabsTrigger value="items" className="px-4 py-2.5 text-sm font-medium">
                  {t('tabs.items')}
                </TabsTrigger>
                <TabsTrigger value="payments" className="px-4 py-2.5 text-sm font-medium">
                  {t('payments:title', 'Payments')}
                </TabsTrigger>
                <TabsTrigger value="notes" className="px-4 py-2.5 text-sm font-medium">
                  {t('tabs.notes')}
                </TabsTrigger>
                <TabsTrigger value="checklists" className="px-4 py-2.5 text-sm font-medium">
                  {t('tabs.checklists')}
                </TabsTrigger>
                <TabsTrigger value="documents" className="px-4 py-2.5 text-sm font-medium">
                  {t('tabs.documents')}
                </TabsTrigger>
              </TabsList>
            )}
          </div>

          <TabsContent value="overview" className="mt-0">
            <OverviewTab offer={offer} />
          </TabsContent>

          <TabsContent value="items">
            <ItemsTab 
              offer={offer} 
              onItemsUpdated={async () => {
                if (id) {
                  const updatedOffer = await OffersService.getOfferById(id);
                  if (updatedOffer) setOffer(updatedOffer);
                }
              }}
            />
          </TabsContent>

          <TabsContent value="payments">
            <PaymentsTab
              entityType="offer"
              entityId={offer.id}
              entityNumber={offer.offerNumber ?? offer.id}
              totalAmount={calculateEntityTotal(offer).total}
              currency={offer.currency ?? 'TND'}
              items={(offer.items ?? []).map(item => ({
                id: item.id,
                itemName: item.itemName,
                totalPrice: item.totalPrice ?? 0,
              }))}
              entityData={offer}
            />
          </TabsContent>

          <TabsContent value="notes">
            <NotesTab offer={offer} />
          </TabsContent>

          <TabsContent value="checklists">
            <ChecklistsTab offer={offer} />
          </TabsContent>

          <TabsContent value="documents">
            {offer && <DocumentsTab offer={offer} />}
          </TabsContent>

        </Tabs>
      </div>

      {isPDFModalOpen && offer && (
        <OfferPDFPreviewModal
          offer={offer}
          isOpen={isPDFModalOpen}
          onClose={() => setIsPDFModalOpen(false)}
          formatCurrency={formatCurrency}
        />
      )}

      {/* Convert to Sale Dialog */}
      <Dialog open={showConvertDialog} onOpenChange={setShowConvertDialog}>
        <DialogContent>
          <DialogHeader>
            <DialogTitle className="flex items-center gap-2">
              <ShoppingCart className="h-5 w-5" />
              {t('detail.convertOfferToSale')}
            </DialogTitle>
            <DialogDescription>
              {t('detail.convertOfferDescription')}
            </DialogDescription>
          </DialogHeader>
          
          <div className="py-4">
            <div className="bg-muted/50 rounded-lg p-4 space-y-2">
              <div className="flex justify-between text-sm">
                <span className="text-muted-foreground">{t('offer')}:</span>
                <span className="font-medium">{offer.title}</span>
              </div>
              <div className="flex justify-between text-sm">
                <span className="text-muted-foreground">{t('detail.customer')}:</span>
                <span className="font-medium">{offer.contactName}</span>
              </div>
              <div className="flex justify-between text-sm">
                <span className="text-muted-foreground">{t('tabs.items')}:</span>
                <span className="font-medium">{offer.items.length} {t('detail.items')}</span>
              </div>
              <div className="flex justify-between text-sm pt-2 border-t">
                <span className="text-muted-foreground">{t('detail.totalAmount')}:</span>
                <span className="font-bold text-primary">
                      {(() => {
                        const subtotal = offer.items.reduce((sum, item) => sum + (Number(item.totalPrice) || 0), 0);
                        const discountAmt = (offer.discount ?? 0) > 0
                          ? (offer.discountType === 'percentage'
                            ? subtotal * ((offer.discount ?? 0) / 100)
                            : (offer.discount ?? 0))
                          : 0;
                        const afterDiscount = subtotal - discountAmt;
                        const tax = (offer.taxes ?? 0) > 0
                          ? (offer.taxType === 'percentage'
                            ? afterDiscount * ((offer.taxes ?? 0) / 100)
                            : (offer.taxes ?? 0))
                          : 0;
                        const fiscalStampAmt = offer.fiscalStamp ?? 0;
                        const totalWithTax = afterDiscount + tax + fiscalStampAmt;
                        return formatCurrency(totalWithTax);
                      })()}
                </span>
              </div>
            </div>
          </div>

          <DialogFooter>
            <Button variant="outline" onClick={() => setShowConvertDialog(false)} disabled={isConverting}>
              {t('cancel')}
            </Button>
            <Button onClick={handleConvertToSale} disabled={isConverting} className="gap-2">
              {isConverting ? (
                <>
                  <Loader2 className="h-4 w-4 animate-spin" />
                  {t('detail.converting')}
                </>
              ) : (
                <>
                  <ShoppingCart className="h-4 w-4" />
                  {t('convert_to_sale')}
                </>
              )}
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>

      {/* Delete Confirmation Dialog */}
      <Dialog open={showDeleteDialog} onOpenChange={setShowDeleteDialog}>
        <DialogContent>
          <DialogHeader>
            <DialogTitle>{t('deleteConfirm.title')}</DialogTitle>
            <DialogDescription>
              {t('deleteConfirm.description')}
            </DialogDescription>
          </DialogHeader>
          <DialogFooter>
            <Button variant="outline" onClick={() => setShowDeleteDialog(false)} disabled={actionLoading === 'delete'}>
              {t('deleteConfirm.cancel')}
            </Button>
            <Button variant="destructive" onClick={handleDeleteOffer} disabled={actionLoading === 'delete'} className="gap-2">
              {actionLoading === 'delete' ? (
                <Loader2 className="h-4 w-4 animate-spin" />
              ) : (
                <Trash2 className="h-4 w-4" />
              )}
              {t('deleteConfirm.confirm')}
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>

      {/* Send Offer Modal */}
      {offer && (
        <SendOfferModal
          open={showSendModal}
          onOpenChange={setShowSendModal}
          offer={offer}
          onSendSuccess={handleSendOfferSuccess}
        />
      )}

      {/* RS Record Modal */}
      {offer && (
        <RSRecordModal
          open={showRSModal}
          onOpenChange={setShowRSModal}
          entityType="offer"
          entityId={offer.id}
          entityNumber={offer.offerNumber ?? offer.id}
          entityAmount={calculateEntityTotal(offer).total}
          contactName={offer.contactName}
          contactTaxId={offer.contactMatriculeFiscale}
          contactAddress={offer.contactAddress}
          onSuccess={() => {}}
        />
      )}

      {/* TEJ Export Modal */}
      {offer && (
        <TEJExportModal
          open={showTEJExport}
          onOpenChange={setShowTEJExport}
          entityType="offer"
          entityId={Number(offer.id)}
          onExportComplete={() => {}}
        />
      )}
    </div>
  );
}
