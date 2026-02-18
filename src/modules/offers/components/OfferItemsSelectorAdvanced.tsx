import { useState, useEffect, useMemo } from "react";
import { ContentSkeleton } from "@/components/ui/page-skeleton";
import { Package, Wrench, Trash2, Plus, Minus, Search, Loader2, ChevronLeft, ChevronRight } from "lucide-react";
import { useNavigate } from "react-router-dom";
import { useTranslation } from "react-i18next";
import { Button } from "@/components/ui/button";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { Badge } from "@/components/ui/badge";
import { Separator } from "@/components/ui/separator";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from "@/components/ui/select";
import { Dialog, DialogContent, DialogHeader, DialogTitle } from "@/components/ui/dialog";
import { OfferItem } from "../types";
import { EditOfferItemModal } from "./EditOfferItemModal";
import { InstallationSelector } from "@/modules/field/installations/components/InstallationSelector";
import { CreateInstallationModal } from "@/modules/field/installations/components/CreateInstallationModal";
import { articlesApi } from "@/services/api/articlesApi";
import { toast } from "sonner";

interface ArticleItem {
  id: string | number;
  name: string;
  sku?: string;
  articleNumber?: string;
  serviceCode?: string;
  description?: string;
  type: 'material' | 'service';
  salesPrice?: number;
  sellPrice?: number;
  basePrice?: number;
  purchasePrice?: number;
  stockQuantity?: number;
  stock?: number;
  duration?: number;
  categoryId?: number;
  categoryName?: string;
}


interface OfferItemsSelectorAdvancedProps {
  items: OfferItem[];
  onUpdateItems: (items: OfferItem[]) => void;
  currency?: string;
  readonly?: boolean;
}

export function OfferItemsSelectorAdvanced({ items, onUpdateItems, currency = 'TND', readonly = false }: OfferItemsSelectorAdvancedProps) {
  const { t } = useTranslation('offers');
  const navigate = useNavigate();
  const [searchTerm, setSearchTerm] = useState("");
  const [selectedType, setSelectedType] = useState<'article' | 'service'>('article');
  const [showItemSelector, setShowItemSelector] = useState(false);
  const [editingItem, setEditingItem] = useState<OfferItem | null>(null);
  const [selectedQuantity, setSelectedQuantity] = useState(1);
  const [selectedInstallation, setSelectedInstallation] = useState<any | null>(null);
  const [showCreateInstallation, setShowCreateInstallation] = useState(false);
  
  // Real data from backend
  const [allArticles, setAllArticles] = useState<ArticleItem[]>([]);
  const [isLoading, setIsLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  
  
  // Pagination
  const [currentPage, setCurrentPage] = useState(1);
  const itemsPerPage = 12;

  // Fetch articles from backend when modal opens
  useEffect(() => {
    if (showItemSelector) {
      fetchArticles();
    }
  }, [showItemSelector]);

  const fetchArticles = async () => {
    setIsLoading(true);
    setError(null);
    try {
      const response = await articlesApi.getAll({ limit: 1000 });
      const articlesData = response.data || response;
      const articlesList = Array.isArray(articlesData) ? articlesData : [];
      
      // Map backend response to our format
      const mappedArticles: ArticleItem[] = articlesList.map((article: any) => ({
        id: article.id,
        name: article.name,
        sku: article.articleNumber || article.sku || '',
        articleNumber: article.articleNumber,
        serviceCode: article.articleNumber || article.serviceCode || '',
        description: article.description,
        type: article.type === 'service' ? 'service' : 'material',
        salesPrice: article.salesPrice || 0,
        sellPrice: article.salesPrice || article.sellPrice || 0,
        basePrice: article.salesPrice || article.basePrice || 0,
        purchasePrice: article.purchasePrice || 0,
        stockQuantity: article.stockQuantity || 0,
        stock: article.stockQuantity || article.stock || 0,
        duration: article.duration,
        categoryId: article.categoryId,
        categoryName: article.categoryName || article.category,
      }));
      
      setAllArticles(mappedArticles);
    } catch (err) {
      console.error('Failed to fetch articles:', err);
      setError('Failed to load articles. Please try again.');
      toast.error('Failed to load articles');
    } finally {
      setIsLoading(false);
    }
  };


  const formatCurrency = (amount: number) => {
    return new Intl.NumberFormat('en-US', {
      style: 'currency',
      currency: currency,
    }).format(amount);
  };

  // Filter articles based on type, search, and category
  // Filter articles based on type and search
  const filteredItems = useMemo(() => {
    const typeFilter = selectedType === 'article' ? 'material' : 'service';
    return allArticles.filter(article => {
      const matchesType = article.type === typeFilter;
      const matchesSearch = !searchTerm || 
        article.name.toLowerCase().includes(searchTerm.toLowerCase()) ||
        (article.sku && article.sku.toLowerCase().includes(searchTerm.toLowerCase())) ||
        (article.articleNumber && article.articleNumber.toLowerCase().includes(searchTerm.toLowerCase()));
      return matchesType && matchesSearch;
    });
  }, [allArticles, selectedType, searchTerm]);

  // Paginated items
  const totalPages = Math.ceil(filteredItems.length / itemsPerPage);
  const paginatedItems = useMemo(() => {
    const start = (currentPage - 1) * itemsPerPage;
    return filteredItems.slice(start, start + itemsPerPage);
  }, [filteredItems, currentPage]);

  // Reset page when filters change
  useEffect(() => {
    setCurrentPage(1);
  }, [searchTerm, selectedType]);

  const handleSelectItem = (item: ArticleItem) => {
    const unitPrice = item.salesPrice || item.sellPrice || item.basePrice || 0;
    const totalPrice = unitPrice * selectedQuantity;

    const newOfferItem: OfferItem = {
      id: `item-${Date.now()}`,
      offerId: '', // Will be set when offer is created
      type: selectedType,
      itemId: String(item.id),
      itemName: item.name,
      itemCode: item.sku || item.articleNumber || item.serviceCode || '',
      quantity: selectedQuantity,
      unitPrice: unitPrice,
      totalPrice: totalPrice,
      description: item.description || undefined,
      installationId: selectedInstallation?.id || undefined,
      installationName: selectedInstallation?.name || undefined,
      // Include duration for service items (from article)
      duration: item.type === 'service' ? item.duration : undefined
    };

    onUpdateItems([...items, newOfferItem]);
    setShowItemSelector(false);
    setSearchTerm("");
    setSelectedQuantity(1);
    setSelectedInstallation(null);
  };

  const handleEditItem = (updatedItem: OfferItem) => {
    const updatedItems = items.map(item => 
      item.id === updatedItem.id ? updatedItem : item
    );
    onUpdateItems(updatedItems);
    setEditingItem(null);
  };

  const handleDeleteItem = (itemId: string) => {
    const updatedItems = items.filter(item => item.id !== itemId);
    onUpdateItems(updatedItems);
  };

  const handleQuantityChange = (itemId: string, delta: number) => {
    const updatedItems = items.map(item => {
      if (item.id === itemId) {
        const newQuantity = Math.max(1, item.quantity + delta);
        return {
          ...item,
          quantity: newQuantity,
          totalPrice: item.unitPrice * newQuantity
        };
      }
      return item;
    });
    onUpdateItems(updatedItems);
  };

  const calculateSubtotal = () => {
    return items.reduce((sum, item) => sum + item.totalPrice, 0);
  };

  return (
    <div className="space-y-4">
      <div className="flex items-center justify-between">
        <h3 className="text-lg font-semibold">{t('selectItemsToSell')}</h3>
        {!readonly && (
          <Button type="button" variant="outline" onClick={() => setShowItemSelector(true)} className="gap-2">
            <Plus className="h-4 w-4" />
            {t('addItems')}
          </Button>
        )}
      </div>

      {items.length === 0 ? (
        <div className="text-center py-8 text-muted-foreground">
          <Package className="h-12 w-12 mx-auto mb-4 opacity-50" />
          <p>{t('noItemsAddedYet')}</p>
          {!readonly && (
            <p className="text-sm">{t('clickAddItemsToStart')}</p>
          )}
        </div>
      ) : (
        <div className="space-y-3">
          {items.map((item) => (
            <Card key={item.id} className="border">
              <CardContent className="p-4">
                <div className="flex items-center justify-between">
                  <div className="flex-1">
                    <div className="flex items-center gap-2 mb-2">
                      {item.type === 'article' ? (
                        <Package className="h-4 w-4" />
                      ) : (
                        <Wrench className="h-4 w-4" />
                      )}
                      <span className="font-medium">{item.itemName}</span>
                      <Badge variant="outline" className="text-xs">
                        {item.type}
                      </Badge>
                    </div>
                    <div className="text-sm text-muted-foreground">
                      {item.itemCode && (
                          <p className="mb-1">{item.itemCode}</p>
                        )}
                       <div className="flex items-center gap-4">
                         <div className="flex items-center gap-2">
                           <span>{t('qty')}:</span>
                           {!readonly ? (
                             <div className="flex items-center gap-1">
                               <Button
                                 type="button"
                                 variant="outline"
                                 size="icon"
                                 className="h-6 w-6"
                                 onClick={() => handleQuantityChange(item.id, -1)}
                                 disabled={item.quantity <= 1}
                               >
                                 <Minus className="h-3 w-3" />
                               </Button>
                               <span className="w-8 text-center font-medium">{item.quantity}</span>
                               <Button
                                 type="button"
                                 variant="outline"
                                 size="icon"
                                 className="h-6 w-6"
                                 onClick={() => handleQuantityChange(item.id, 1)}
                               >
                                 <Plus className="h-3 w-3" />
                               </Button>
                             </div>
                           ) : (
                             <span>{item.quantity}</span>
                           )}
                         </div>
                         <span>{t('unit')}: {formatCurrency(item.unitPrice)}</span>
                         {item.discount && item.discount > 0 && (
                           <span>{t('discount')}: {item.discountType === 'percentage' ? `${item.discount}%` : formatCurrency(item.discount)}</span>
                         )}
                       </div>
                       {item.installationName && (
                         <div className="text-xs text-muted-foreground mt-1">
                           {t('installation')}: {item.installationName}
                         </div>
                       )}
                    </div>
                  </div>
                  <div className="flex items-center gap-3">
                    {!readonly && (
                      <Button
                        type="button"
                        variant="ghost"
                        size="sm"
                        onClick={() => handleDeleteItem(item.id)}
                        className="text-destructive hover:text-destructive"
                      >
                        <Trash2 className="h-4 w-4" />
                      </Button>
                    )}
                  </div>
                </div>
              </CardContent>
            </Card>
          ))}
          
          {items.length > 0 && (
            <>
              <Separator />
              <div className="flex justify-between items-center py-2">
                <span className="font-semibold">{t('subtotal')}:</span>
                <span className="font-semibold text-lg">
                  {formatCurrency(calculateSubtotal())}
                </span>
              </div>
            </>
          )}
        </div>
      )}

      {/* Item Selector Dialog */}
      <Dialog open={showItemSelector} onOpenChange={setShowItemSelector}>
        <DialogContent className="max-w-4xl max-h-[80vh] overflow-y-auto">
          <DialogHeader>
            <DialogTitle className="flex items-center gap-2">
              <Package className="h-5 w-5" />
              {t('selectItemsToAdd')}
            </DialogTitle>
          </DialogHeader>

          <div className="space-y-4">
            {/* Type, Search and Quantity */}
            <div className="flex flex-wrap gap-4">
              <div className="w-36">
                <Label>{t('itemType')}</Label>
                <Select value={selectedType} onValueChange={(value: 'article' | 'service') => {
                  setSelectedType(value);
                  setSearchTerm("");
                }}>
                  <SelectTrigger>
                    <SelectValue />
                  </SelectTrigger>
                  <SelectContent>
                    <SelectItem value="article">{t('materials')}</SelectItem>
                    <SelectItem value="service">{t('services')}</SelectItem>
                  </SelectContent>
                </Select>
              </div>

              <div className="flex-1 min-w-[200px]">
                <Label>{t('search')}</Label>
                <div className="relative">
                  <Search className="absolute left-3 top-1/2 transform -translate-y-1/2 h-4 w-4 text-muted-foreground" />
                  <Input
                    placeholder={`${t('search')}...`}
                    value={searchTerm}
                    onChange={(e) => setSearchTerm(e.target.value)}
                    className="pl-10"
                  />
                </div>
              </div>

              <div className="w-24">
                <Label>{t('quantity')}</Label>
                <Input
                  type="number"
                  min="1"
                  value={selectedQuantity}
                  onChange={(e) => setSelectedQuantity(parseInt(e.target.value) || 1)}
                />
              </div>
            </div>

            {/* Installation Selection for Services and Materials */}
            <div className="border-t pt-4">
              <InstallationSelector
                onSelect={setSelectedInstallation}
                selectedInstallation={selectedInstallation}
                onCreateNew={() => setShowCreateInstallation(true)}
              />
            </div>

            {/* Results count */}
            {!isLoading && !error && filteredItems.length > 0 && (
              <div className="text-sm text-muted-foreground">
                {t('showingItems', { start: ((currentPage - 1) * itemsPerPage) + 1, end: Math.min(currentPage * itemsPerPage, filteredItems.length), total: filteredItems.length })}
              </div>
            )}

            {/* Items Grid */}
            {isLoading ? (
              <ContentSkeleton rows={6} />
            ) : error ? (
              <div className="text-center py-8 text-destructive">
                <p>{error}</p>
                <Button type="button" variant="outline" onClick={fetchArticles} className="mt-2">
                  {t('retry')}
                </Button>
              </div>
            ) : filteredItems.length === 0 ? (
              <div className="text-center py-8 text-muted-foreground">
                {searchTerm ? (
                  <p>{t('noItemsMatchingFilters', { type: selectedType === 'article' ? t('materials') : t('services') })}</p>
                ) : (
                  <p>{t('noItemsAvailable', { type: selectedType === 'article' ? t('materials') : t('services') })}</p>
                )}
              </div>
            ) : (
              <>
                <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-3 max-h-80 overflow-y-auto">
                  {paginatedItems.map((item) => (
                    <Card 
                      key={item.id} 
                      className="border cursor-pointer hover:bg-muted/50 transition-colors"
                      onClick={() => handleSelectItem(item)}
                    >
                      <CardContent className="p-4">
                        <div className="flex items-start gap-3">
                          <div className="p-2 rounded-lg bg-primary/10">
                            {item.type === 'material' ? (
                              <Package className="h-4 w-4 text-primary" />
                            ) : (
                              <Wrench className="h-4 w-4 text-primary" />
                            )}
                          </div>
                          <div className="flex-1 min-w-0">
                            <h4 className="font-medium truncate">{item.name}</h4>
                            <p className="text-sm text-muted-foreground">
                              {item.sku || item.articleNumber || '-'}
                            </p>
                            <p className="text-sm font-medium text-primary">
                              {formatCurrency(item.salesPrice || item.sellPrice || item.basePrice || 0)}
                            </p>
                            {item.categoryName && (
                              <Badge variant="secondary" className="text-xs mt-1">
                                {item.categoryName}
                              </Badge>
                            )}
                            {item.type === 'material' && (
                              <p className="text-xs text-muted-foreground mt-1">
                                {t('stock')}: {item.stockQuantity || item.stock || 0}
                              </p>
                            )}
                            {item.type === 'service' && item.duration && (
                              <p className="text-xs text-muted-foreground mt-1">
                                {t('duration')}: {item.duration} min
                              </p>
                            )}
                          </div>
                        </div>
                      </CardContent>
                    </Card>
                  ))}
                </div>

                {/* Pagination */}
                {totalPages > 1 && (
                  <div className="flex items-center justify-between pt-4 border-t">
                    <p className="text-sm text-muted-foreground">
                      {t('pagination.page', { current: currentPage, total: totalPages })}
                    </p>
                    <div className="flex items-center gap-2">
                      <Button
                        type="button"
                        variant="outline"
                        size="sm"
                        onClick={() => setCurrentPage(p => Math.max(1, p - 1))}
                        disabled={currentPage === 1}
                      >
                        <ChevronLeft className="h-4 w-4" />
                        {t('pagination.previous')}
                      </Button>
                      <Button
                        type="button"
                        variant="outline"
                        size="sm"
                        onClick={() => setCurrentPage(p => Math.min(totalPages, p + 1))}
                        disabled={currentPage === totalPages}
                      >
                        {t('pagination.next')}
                        <ChevronRight className="h-4 w-4" />
                      </Button>
                    </div>
                  </div>
                )}
              </>
            )}

            <div className="flex justify-end gap-2">
              <Button 
                type="button"
                variant="outline" 
                onClick={() => {
                  setShowItemSelector(false);
                  setSelectedInstallation(null);
                }}
              >
                {t('cancel')}
              </Button>
            </div>
          </div>
        </DialogContent>
      </Dialog>

      {/* Edit Item Modal */}
      {editingItem && (
        <EditOfferItemModal
          open={!!editingItem}
          onOpenChange={(open) => !open && setEditingItem(null)}
          item={editingItem}
          onUpdateItem={handleEditItem}
          currency={currency}
        />
      )}

      {/* Create Installation Modal */}
      <CreateInstallationModal
        open={showCreateInstallation}
        onOpenChange={setShowCreateInstallation}
        onInstallationCreated={(installation) => {
          setSelectedInstallation(installation);
          setShowCreateInstallation(false);
        }}
      />
    </div>
  );
}