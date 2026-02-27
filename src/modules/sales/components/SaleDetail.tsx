import { useState, useEffect } from "react";
import { calculateEntityTotal } from "@/lib/calculateTotal";
import { useParams, useNavigate } from "react-router-dom";
import { useTranslation } from "react-i18next";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { Button } from "@/components/ui/button";
import { Badge } from "@/components/ui/badge";
import { Avatar, AvatarFallback } from "@/components/ui/avatar";
import { Tabs, TabsContent, TabsList, TabsTrigger } from "@/components/ui/tabs";
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from "@/components/ui/select";
import { Alert, AlertDescription } from "@/components/ui/alert";
import { useLayoutModeContext } from "@/hooks/useLayoutMode";
import { DropdownMenu, DropdownMenuContent, DropdownMenuItem, DropdownMenuSeparator, DropdownMenuTrigger } from "@/components/ui/dropdown-menu";
import { 
  ArrowLeft,
  Edit,
  Trash2,
  FileDown,
  Send,
  MoreVertical,
  TrendingUp,
  AlertCircle,
  FileText,
  ExternalLink,
  Wrench,
  CheckCircle,
  Zap,
  Info,
  Receipt
} from "lucide-react";

import { Sale } from "../types";
import { SalesService } from "../services/sales.service";
import { useCurrency } from '@/shared/hooks/useCurrency';
import { toast } from "sonner";
import { SalePDFPreviewModal } from "./SalePDFPreviewModal";
import { ConvertToServiceOrderDialog } from "./ConvertToServiceOrderDialog";
import { SaleStatusFlow, SaleStatus } from "./SaleStatusFlow";
import { ServiceOrderConfig } from "./ServiceOrderConfigModal";
import { SendSaleModal } from "./SendSaleModal";
import { useWorkflowStatus } from "@/modules/workflow/hooks/useWorkflowStatus";
import { getStatusColorClass } from "@/config/entity-statuses";

// Import tab components
import { OverviewTab } from "./tabs/OverviewTab";
import { ItemsTab } from "./tabs/ItemsTab";
import { NotesTab } from "./tabs/NotesTab";
import { ChecklistsTab } from "./tabs/ChecklistsTab";
import { DocumentsTab } from "./tabs/DocumentsTab";
import { PaymentsTab } from "@/modules/payments/components/PaymentsTab";
import { RSRecordModal } from "@/modules/shared/components/RSRecordModal";
import { TEJExportModal } from "@/modules/shared/components/TEJExportModal";



export function SaleDetail() {
  const { id } = useParams<{ id: string }>();
  const navigate = useNavigate();
  const { t } = useTranslation();
  const { format: formatCurrency } = useCurrency();
  const { isMobile } = useLayoutModeContext();
  const workflowStatus = useWorkflowStatus();
  const [sale, setSale] = useState<Sale | null>(null);
  const [loading, setLoading] = useState(true);
  const [isStatusUpdating, setIsStatusUpdating] = useState(false);
  const [isPDFModalOpen, setIsPDFModalOpen] = useState(false);
  const [activeTab, setActiveTab] = useState("overview");
  const [showServiceOrderDialog, setShowServiceOrderDialog] = useState(false);
  const [showSendModal, setShowSendModal] = useState(false);
  const [showRSModal, setShowRSModal] = useState(false);
  const [showTEJExport, setShowTEJExport] = useState(false);
  const [hasShownAutoPrompt, setHasShownAutoPrompt] = useState(false);

  // Check if sale has service items and conversion status
  const hasServiceItems = sale?.items?.some((item) => item.type === "service") || false;
  const isAlreadyConverted = !!sale?.convertedToServiceOrderId;

  const fetchSale = async () => {
    if (!id) return;
    try {
      setLoading(true);
      const saleData = await SalesService.getSaleById(id);
      setSale(saleData);
    } catch (error) {
      console.error('Failed to fetch sale:', error);
      toast.error("Failed to load sale details");
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    fetchSale();
  }, [id]);

  // Callback to refresh sale data after conversion
  const handleConversionComplete = async (serviceOrderId: string) => {
    // Refresh sale data to show the new service order link
    await fetchSale();
    toast.success(`Service Order #${serviceOrderId} created - sale data refreshed`);
  };

  // Auto-show service order dialog when sale has services 
  // Only if workflow automation is NOT active (manual mode)
  useEffect(() => {
    if (sale && !hasShownAutoPrompt && !loading && !sale.convertedToServiceOrderId && !workflowStatus.isActive) {
      const hasServices = sale.items?.some((item) => item.type === "service");
      if (hasServices) {
        // Small delay for better UX
        const timer = setTimeout(() => {
          setShowServiceOrderDialog(true);
          setHasShownAutoPrompt(true);
        }, 500);
        return () => clearTimeout(timer);
      }
    }
  }, [sale, hasShownAutoPrompt, loading, workflowStatus.isActive]);

  if (loading) {
    return (
      <div className="p-6 space-y-6 animate-pulse">
        <div className="flex items-center gap-3">
          <div className="h-8 w-8 bg-muted rounded" />
          <div className="h-7 w-48 bg-muted rounded" />
        </div>
        <div className="h-12 w-full bg-muted/60 rounded-lg" />
        <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
          {[...Array(4)].map((_, i) => (
            <div key={i} className="h-20 bg-muted/60 rounded-lg" />
          ))}
        </div>
        <div className="h-64 bg-muted/40 rounded-lg" />
      </div>
    );
  }

  if (!sale) {
    return (
      <div className="flex flex-col items-center justify-center min-h-[400px] p-6">
        <AlertCircle className="h-16 w-16 text-muted-foreground mb-4" />
        <h2 className="text-2xl font-semibold text-foreground mb-2">{t('detail.saleNotFound')}</h2>
        <p className="text-muted-foreground mb-4">{t('detail.saleNotFoundDescription', { id })}</p>
        <Button onClick={() => navigate('/dashboard/sales')} variant="outline">
          <ArrowLeft className="mr-2 h-4 w-4" />
          {t('backToSales')}
        </Button>
      </div>
    );
  }

  const getStatusColor = (status: string) => {
    return getStatusColorClass('sale', status);
  };

  const handleSendSaleSuccess = () => {
    // Status update can be added here if needed
    toast.success(t('detail.saleSent'));
  };

  const handleSendInvoice = () => {
    toast.success(t('detail.invoiceSent'));
  };

  const handleDownloadPDF = () => {
    setIsPDFModalOpen(true);
  };

  const handleEditSale = () => {
    navigate(`/dashboard/sales/${id}/edit`);
  };

  const handleDeleteSale = () => {
    toast.success(t('detail.saleDeleted'));
    navigate('/dashboard/sales');
  };

  const handleConvertToServiceOrder = () => {
    setShowServiceOrderDialog(true);
  };

  const handleStatusChange = async (newStatus: SaleStatus, serviceOrderConfig?: ServiceOrderConfig) => {
    if (!sale || !id) return;
    const oldStatus = sale.status;
    setIsStatusUpdating(true);
    try {
      // If we have a service order config (from workflow modal), include it in the update
      // so the backend knows the configuration for auto-creation
      const updateData: any = { status: newStatus as any };
      
      if (serviceOrderConfig) {
        updateData.serviceOrderConfig = serviceOrderConfig;
      }
      
      await SalesService.updateSale(id, updateData);
      
      // Log activity in the sale
      const { salesApi } = await import('@/services/api/salesApi');
      const saleId = parseInt(id, 10);
      if (!isNaN(saleId)) {
        await salesApi.addActivity(saleId, {
          type: 'status_changed',
          description: `Status changed from "${oldStatus}" to "${newStatus}"`,
          details: serviceOrderConfig 
            ? `Sale status updated with service order config (Priority: ${serviceOrderConfig.priority})`
            : `Sale status updated on ${new Date().toLocaleDateString()}`,
        });
      }
      
      // If sale was converted from an offer, log activity in the offer too
      if (sale.offerId) {
        try {
          const { offersApi } = await import('@/services/api/offersApi');
          const offerId = parseInt(sale.offerId, 10);
          if (!isNaN(offerId)) {
            await offersApi.addActivity(offerId, {
              type: 'sale_status_changed',
              description: `Related Sale status changed from "${oldStatus}" to "${newStatus}"`,
              details: `Sale #${sale.saleNumber || sale.id} status was updated`,
            });
          }
        } catch (offerActivityError) {
          console.warn('Failed to log activity in related offer:', offerActivityError);
        }
      }
      
      toast.success(t('sales:statusFlow.statusUpdated'));
      
      // Show additional toast if service order was auto-created
      if (serviceOrderConfig && workflowStatus.isActive) {
        toast.info(t('sales:serviceOrderAutoCreated', 'Service order is being created automatically...'));
      }
      
      fetchSale();
    } catch (error) {
      console.error('Failed to update sale status:', error);
      toast.error(t('sales:error'));
    } finally {
      setIsStatusUpdating(false);
    }
  };

  return (
    <div className="flex flex-col min-h-screen bg-background">
      {/* Header */}
      <div className="border-b border-border bg-gradient-subtle backdrop-blur-sm sticky top-0 z-20 shadow-soft">
        {/* Mobile Header */}
        <div className="md:hidden">
          <div className="flex items-center justify-between p-4 border-b border-border/50">
            <Button
              variant="ghost"
              size="sm"
              onClick={() => navigate('/dashboard/sales')}
              className="gap-2"
            >
              <ArrowLeft className="h-4 w-4" />
              {t('detail.back')}
            </Button>
            <DropdownMenu>
              <DropdownMenuTrigger asChild>
                <Button variant="ghost" size="sm" className="h-8 w-8 p-0">
                  <MoreVertical className="h-4 w-4" />
                </Button>
              </DropdownMenuTrigger>
              <DropdownMenuContent align="end" className="w-48 bg-background/95 backdrop-blur-sm border border-border/50">
                <DropdownMenuItem onClick={handleDownloadPDF} className="gap-2">
                  <FileDown className="h-4 w-4" />
                  {t('detail.downloadPdf')}
                </DropdownMenuItem>
                <DropdownMenuItem onClick={handleSendInvoice} className="gap-2">
                  <Send className="h-4 w-4" />
                  {t('detail.sendInvoice')}
                </DropdownMenuItem>
                <DropdownMenuSeparator />
               {/*  <DropdownMenuItem onClick={() => setShowRSModal(true)} className="gap-2">
                  <Receipt className="h-4 w-4" />
                  {t('rs.addRS', 'Retenue à la Source')}
                </DropdownMenuItem>
                <DropdownMenuItem onClick={() => setShowTEJExport(true)} className="gap-2">
                  <FileDown className="h-4 w-4" />
                  {t('rs.exportTEJ', 'Exporter TEJ XML')}
                </DropdownMenuItem>
                <DropdownMenuSeparator /> */}
                <DropdownMenuItem onClick={handleDeleteSale} className="gap-2 text-destructive">
                  <Trash2 className="h-4 w-4" />
                  {t('deleteSale')}
                </DropdownMenuItem>
                {hasServiceItems && (
                  <>
                    <DropdownMenuSeparator />
                    {!isAlreadyConverted ? (
                      <DropdownMenuItem onClick={handleConvertToServiceOrder} className="gap-2 text-primary">
                        <Wrench className="h-4 w-4" />
                        {t('detail.convertToServiceOrder')}
                      </DropdownMenuItem>
                    ) : (
                      <DropdownMenuItem 
                        onClick={() => navigate(`/dashboard/field/service-orders/${sale.convertedToServiceOrderId}`)} 
                        className="gap-2 text-success"
                      >
                        <Wrench className="h-4 w-4" />
                        {t('detail.viewServiceOrder')}
                      </DropdownMenuItem>
                    )}
                  </>
                )}
              </DropdownMenuContent>
            </DropdownMenu>
          </div>
          <div className="p-4 space-y-3">
            <div className="flex-1 min-w-0">
              <h1 className="text-xl font-bold text-foreground mb-1">
                {sale.title}
              </h1>
              <p className="text-lg font-semibold text-primary mb-2">
                {(() => {
                  const subtotal = sale.items?.reduce((sum, item) => sum + (Number(item.totalPrice) || 0), 0) || 0;
                  const discountAmt = (sale.discount ?? 0) > 0
                    ? (sale.discountType === 'percentage'
                      ? subtotal * ((sale.discount ?? 0) / 100)
                      : (sale.discount ?? 0))
                    : 0;
                  const afterDiscount = subtotal - discountAmt;
                  const tax = (sale.taxes ?? 0) > 0
                    ? (sale.taxType === 'percentage'
                      ? afterDiscount * ((sale.taxes ?? 0) / 100)
                      : (sale.taxes ?? 0))
                    : 0;
                  const fiscalStampAmt = sale.fiscalStamp ?? 0;
                  const totalWithTax = afterDiscount + tax + fiscalStampAmt;
                  return formatCurrency(sale.totalAmount || totalWithTax);
                })()}
              </p>
              <div className="flex flex-wrap items-center gap-2">
                {sale.convertedToServiceOrderId && (
                  <Button
                    variant="ghost"
                    size="sm"
                    onClick={() => navigate(`/dashboard/field/service-orders/${sale.convertedToServiceOrderId}`)}
                    className="gap-1 text-xs h-6 px-2 text-muted-foreground hover:text-primary"
                  >
                    <Wrench className="h-3 w-3" />
                    {t('viewServiceOrder')} #{sale.convertedToServiceOrderId}
                  </Button>
                )}
              </div>
            </div>
            {/* Status flow below title/amount on mobile */}
            <div className="flex justify-start">
              <SaleStatusFlow 
                currentStatus={sale.status} 
                onStatusChange={handleStatusChange}
                sale={sale}
                isUpdating={isStatusUpdating}
              />
            </div>
          </div>
        </div>

        {/* Desktop Header - Compact Card Style */}
        <div className="hidden md:block p-4 lg:p-6">
          <div className="flex items-center gap-4">
            {/* Back Button */}
            <Button
              variant="ghost"
              size="icon"
              onClick={() => navigate('/dashboard/sales')}
              className="h-9 w-9 shrink-0 hover:bg-background/80"
            >
              <ArrowLeft className="h-4 w-4" />
            </Button>

            {/* Sale Info Card */}
            <Card className="flex-1 shadow-sm border-border/50">
              <CardContent className="p-4">
                <div className="flex items-center justify-between gap-6">
                  {/* Left: Title */}
                  <div className="flex items-center gap-6 min-w-0">
                    <h1 className="text-xl font-semibold text-foreground truncate">
                      {sale.title}
                    </h1>
                  </div>

                  {/* Right: Status Flow + Actions */}
                  <div className="flex items-center gap-4 shrink-0">
                    <SaleStatusFlow 
                      currentStatus={sale.status} 
                      onStatusChange={handleStatusChange}
                      sale={sale}
                      isUpdating={isStatusUpdating}
                    />
                    
                    <div className="h-8 w-px bg-border/50" />
                    
                    {/* Actions Dropdown */}
                    <DropdownMenu>
                      <DropdownMenuTrigger asChild>
                        <Button variant="outline" size="sm" className="gap-2">
                          <MoreVertical className="h-4 w-4" />
                          {t('sales:actions', 'Actions')}
                        </Button>
                      </DropdownMenuTrigger>
                      <DropdownMenuContent align="end" className="w-48 bg-background border border-border">
                        <DropdownMenuItem onClick={() => setShowSendModal(true)} className="gap-2 cursor-pointer">
                          <Send className="h-4 w-4" />
                          {t('sendInvoice')}
                        </DropdownMenuItem>
                        <DropdownMenuItem onClick={handleDownloadPDF} className="gap-2 cursor-pointer">
                          <FileDown className="h-4 w-4" />
                          {t('downloadPdf')}
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
                        <DropdownMenuItem 
                          onClick={handleDeleteSale} 
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

      {/* Service Order Status Banner - Only shown when sale has service items */}
      {hasServiceItems && (
        <div className="border-b border-border">
          <div className="px-4 py-3">
            <div className="flex items-center justify-between rounded-lg border border-border bg-card p-4">
              <div className="flex items-center gap-3">
                <div>
                  <p className="text-sm font-medium text-foreground">
                    {isAlreadyConverted 
                      ? t('serviceOrderCreatedBanner') 
                      : workflowStatus.isActive 
                        ? t('sales:workflowAutoConversion', 'Workflow automation active')
                        : t('containsServiceItems')}
                  </p>
                  <p className="text-xs text-muted-foreground mt-0.5">
                    {isAlreadyConverted 
                      ? t('serviceOrderCreatedDescription', { id: sale.convertedToServiceOrderId })
                      : workflowStatus.isActive
                        ? t('sales:workflowAutoConversionDesc', 'Service order will be created automatically when status changes to "In Progress"')
                        : t('createServiceOrderPrompt')
                    }
                  </p>
                </div>
              </div>
              {isAlreadyConverted ? (
                <Button 
                  variant="outline" 
                  size="sm" 
                  onClick={() => navigate(`/dashboard/field/service-orders/${sale.convertedToServiceOrderId}`)}
                  className="gap-2 shrink-0"
                >
                  <ExternalLink className="h-4 w-4" />
                  {t('viewServiceOrder')}
                </Button>
              ) : !workflowStatus.isActive ? (
                <Button 
                  variant="default" 
                  size="sm" 
                  onClick={handleConvertToServiceOrder}
                  className="gap-2 shrink-0"
                >
                  <Wrench className="h-4 w-4" />
                  {t('convertToServiceOrder')}
                </Button>
              ) : (
                <Badge variant="outline" className="gap-1 text-warning border-warning/30 bg-warning/10 shrink-0">
                  <Zap className="h-3 w-3" />
                  {t('sales:autoMode', 'Auto')}
                </Badge>
              )}
            </div>
          </div>
        </div>
      )}

      {/* Tabs Content */}
      <div className="px-4 py-6">
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
                    {activeTab === 'notes' && t('tabs.notesActivity')}
                    {activeTab === 'checklists' && t('tabs.checklists')}
                    {activeTab === 'documents' && t('tabs.documents')}
                  </SelectValue>
                </SelectTrigger>
                <SelectContent className="bg-white dark:bg-card">
                  <SelectItem value="overview">{t('tabs.overview')}</SelectItem>
                  <SelectItem value="items">{t('tabs.items')}</SelectItem>
                  <SelectItem value="payments">{t('payments:title', 'Payments')}</SelectItem>
                  <SelectItem value="notes">{t('tabs.notesActivity')}</SelectItem>
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
                  {t('tabs.notesActivity')}
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
            <OverviewTab sale={sale} />
          </TabsContent>

          <TabsContent value="items">
            <ItemsTab sale={sale} onItemsUpdated={fetchSale} />
          </TabsContent>

          <TabsContent value="payments">
            <PaymentsTab
              entityType="sale"
              entityId={sale.id}
              entityNumber={sale.saleNumber ?? sale.id}
              totalAmount={calculateEntityTotal(sale).total}
              currency={sale.currency ?? 'TND'}
              items={(sale.items ?? []).map(item => ({
                id: item.id,
                itemName: item.itemName,
                totalPrice: item.totalPrice ?? 0,
              }))}
              entityData={sale}
            />
          </TabsContent>

          <TabsContent value="notes">
            <NotesTab sale={sale} />
          </TabsContent>

          <TabsContent value="checklists">
            <ChecklistsTab sale={sale} />
          </TabsContent>

          <TabsContent value="documents">
            {sale && <DocumentsTab sale={sale} />}
          </TabsContent>
        </Tabs>
      </div>

      {/* PDF Preview Modal */}
      {isPDFModalOpen && sale && (
        <SalePDFPreviewModal
          isOpen={isPDFModalOpen}
          onClose={() => setIsPDFModalOpen(false)}
          sale={sale}
          formatCurrency={formatCurrency}
        />
      )}

      {/* Convert to Service Order Dialog */}
      {sale && hasServiceItems && (
        <ConvertToServiceOrderDialog
          open={showServiceOrderDialog}
          onOpenChange={setShowServiceOrderDialog}
          sale={sale}
          onConversionComplete={handleConversionComplete}
        />
      )}

      {/* Send Sale Modal */}
      {sale && (
        <SendSaleModal
          open={showSendModal}
          onOpenChange={setShowSendModal}
          sale={sale}
          onSendSuccess={handleSendSaleSuccess}
        />
      )}

      {/* RS Record Modal */}
      {sale && (
        <RSRecordModal
          open={showRSModal}
          onOpenChange={setShowRSModal}
          entityType="sale"
          entityId={sale.id}
          entityNumber={sale.saleNumber ?? sale.id}
          entityAmount={calculateEntityTotal(sale).total || sale.totalAmount || sale.amount || 0}
          contactName={sale.contactName}
          contactTaxId={sale.contactMatriculeFiscale}
          contactAddress={sale.contactAddress}
          onSuccess={fetchSale}
        />
      )}

      {/* TEJ Export Modal */}
      {sale && (
        <TEJExportModal
          open={showTEJExport}
          onOpenChange={setShowTEJExport}
          entityType="sale"
          entityId={Number(sale.id)}
          onExportComplete={() => {}}
        />
      )}
    </div>
  );
}