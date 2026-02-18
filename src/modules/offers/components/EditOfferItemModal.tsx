import { useState, useEffect } from "react";
import { useTranslation } from "react-i18next";
import { Dialog, DialogContent, DialogHeader, DialogTitle } from "@/components/ui/dialog";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { Badge } from "@/components/ui/badge";
import { Package, Wrench } from "lucide-react";
import { OfferItem } from "../types";

interface EditOfferItemModalProps {
  open: boolean;
  onOpenChange: (open: boolean) => void;
  item: OfferItem;
  onUpdateItem: (item: OfferItem) => void;
  currency: string;
}

export function EditOfferItemModal({ open, onOpenChange, item, onUpdateItem, currency }: EditOfferItemModalProps) {
  const { t } = useTranslation('offers');
  const [quantity, setQuantity] = useState(item.quantity);

  useEffect(() => {
    setQuantity(item.quantity);
  }, [item]);

  const calculateTotal = () => {
    const subtotal = quantity * item.unitPrice;
    const discountAmount = item.discountType === 'percentage' 
      ? subtotal * ((item.discount || 0) / 100)
      : (item.discount || 0);
    return subtotal - discountAmount;
  };

  const handleSubmit = (e: React.FormEvent) => {
    e.preventDefault();
    
    if (quantity <= 0) {
      return;
    }

    const updatedItem: OfferItem = {
      ...item,
      quantity: quantity,
      totalPrice: calculateTotal(),
    };

    onUpdateItem(updatedItem);
  };

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent className="sm:max-w-md">
        <DialogHeader>
          <DialogTitle>{t('editQuantity.title')}</DialogTitle>
        </DialogHeader>
        
        <form onSubmit={handleSubmit} className="space-y-4">
          {/* Item Info (Read-only) */}
          <div className="bg-muted/50 p-4 rounded-lg space-y-3">
            <div className="flex items-center gap-2">
              {item.type === 'article' ? (
                <Package className="h-5 w-5 text-muted-foreground" />
              ) : (
                <Wrench className="h-5 w-5 text-muted-foreground" />
              )}
              <span className="font-medium">{item.itemName}</span>
              <Badge variant="outline" className="text-xs capitalize">
                {item.type === 'article' ? t('material') : t('service')}
              </Badge>
            </div>
            {item.itemCode && (
              <p className="text-sm text-muted-foreground">{item.itemCode}</p>
            )}
            <div className="text-sm text-muted-foreground">
              {t('editQuantity.unitPrice')}: {item.unitPrice.toLocaleString()} {currency}
            </div>
          </div>

          {/* Quantity (Editable) */}
          <div className="space-y-2">
            <Label htmlFor="quantity">{t('quantity')} *</Label>
            <Input
              id="quantity"
              type="number"
              min="1"
              step="1"
              value={quantity}
              onChange={(e) => setQuantity(parseInt(e.target.value) || 1)}
              required
              autoFocus
            />
          </div>

          {/* Total */}
          <div className="bg-muted/50 p-4 rounded-lg">
            <div className="flex justify-between items-center">
              <span className="font-medium">{t('editQuantity.totalPrice')}:</span>
              <span className="text-lg font-semibold">
                {calculateTotal().toLocaleString()} {currency}
              </span>
            </div>
          </div>

          <div className="flex justify-end gap-3">
            <Button type="button" variant="outline" onClick={() => onOpenChange(false)}>
              {t('cancel')}
            </Button>
            <Button type="submit">
              {t('editQuantity.update')}
            </Button>
          </div>
        </form>
      </DialogContent>
    </Dialog>
  );
}
