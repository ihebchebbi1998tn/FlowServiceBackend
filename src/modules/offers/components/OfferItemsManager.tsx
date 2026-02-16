import { useState } from "react";
import { Package, Wrench, Edit, Trash2, Plus } from "lucide-react";
import { Button } from "@/components/ui/button";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { Badge } from "@/components/ui/badge";
import { Separator } from "@/components/ui/separator";
import { OfferItem } from "../types";
import { AddOfferItemModal } from "./AddOfferItemModal";
import { EditOfferItemModal } from "./EditOfferItemModal";

interface OfferItemsManagerProps {
  items: OfferItem[];
  onUpdateItems: (items: OfferItem[]) => void;
  currency?: string;
  readonly?: boolean;
}

export function OfferItemsManager({ items, onUpdateItems, currency = 'TND', readonly = false }: OfferItemsManagerProps) {
  const [isAddModalOpen, setIsAddModalOpen] = useState(false);
  const [editingItem, setEditingItem] = useState<OfferItem | null>(null);

  const formatCurrency = (amount: number) => {
    return new Intl.NumberFormat('en-US', {
      style: 'currency',
      currency: currency,
    }).format(amount);
  };

  const handleAddItem = (newItem: OfferItem) => {
    onUpdateItems([...items, newItem]);
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

  const calculateSubtotal = () => {
    return items.reduce((sum, item) => sum + item.totalPrice, 0);
  };

  return (
    <div className="space-y-4">
      <div className="flex items-center justify-between">
        <h3 className="text-lg font-semibold">Offer Items</h3>
        {!readonly && (
          <Button onClick={() => setIsAddModalOpen(true)} className="gap-2">
            <Plus className="h-4 w-4" />
            Add Item
          </Button>
        )}
      </div>

      {items.length === 0 ? (
        <div className="text-center py-8 text-muted-foreground">
          <Package className="h-12 w-12 mx-auto mb-4 opacity-50" />
          <p>No items added yet</p>
          {!readonly && (
            <p className="text-sm">Click "Add Item" to get started</p>
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
                        <span>Qty: {item.quantity}</span>
                        <span>Unit: {formatCurrency(item.unitPrice)}</span>
                        {item.discount && item.discount > 0 && (
                          <span>Discount: {item.discountType === 'percentage' ? `${item.discount}%` : formatCurrency(item.discount)}</span>
                        )}
                      </div>
                    </div>
                  </div>
                  <div className="flex items-center gap-3">
                    <div className="text-right">
                      <div className="font-semibold">
                        {formatCurrency(item.totalPrice)}
                      </div>
                    </div>
                    {!readonly && (
                      <div className="flex gap-1">
                        <Button
                          variant="ghost"
                          size="sm"
                          onClick={() => setEditingItem(item)}
                        >
                          <Edit className="h-4 w-4" />
                        </Button>
                        <Button
                          variant="ghost"
                          size="sm"
                          onClick={() => handleDeleteItem(item.id)}
                          className="text-destructive hover:text-destructive"
                        >
                          <Trash2 className="h-4 w-4" />
                        </Button>
                      </div>
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
                <span className="font-semibold">Subtotal:</span>
                <span className="font-semibold text-lg">
                  {formatCurrency(calculateSubtotal())}
                </span>
              </div>
            </>
          )}
        </div>
      )}

      <AddOfferItemModal
        open={isAddModalOpen}
        onOpenChange={setIsAddModalOpen}
        onAddItem={handleAddItem}
        currency={currency}
      />

      {editingItem && (
        <EditOfferItemModal
          open={!!editingItem}
          onOpenChange={(open) => !open && setEditingItem(null)}
          item={editingItem}
          onUpdateItem={handleEditItem}
          currency={currency}
        />
      )}
    </div>
  );
}