import { useState } from "react";
import { Dialog, DialogContent, DialogHeader, DialogTitle } from "@/components/ui/dialog";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from "@/components/ui/select";
import itemTypes from '@/data/mock/offer-item-types.json';
import { Textarea } from "@/components/ui/textarea";
import { OfferItem } from "../types";

interface AddOfferItemModalProps {
  open: boolean;
  onOpenChange: (open: boolean) => void;
  onAddItem: (item: OfferItem) => void;
  currency: string;
}

export function AddOfferItemModal({ open, onOpenChange, onAddItem, currency }: AddOfferItemModalProps) {
  const [formData, setFormData] = useState({
    type: 'article' as 'article' | 'service',
    itemName: '',
    itemCode: '',
    description: '',
    quantity: 1,
    unitPrice: 0,
    discount: 0,
    discountType: 'percentage' as 'percentage' | 'fixed',
    duration: 60 // Default 60 minutes for services
  });

  const calculateTotal = () => {
    const subtotal = formData.quantity * formData.unitPrice;
    const discountAmount = formData.discountType === 'percentage' 
      ? subtotal * (formData.discount / 100)
      : formData.discount;
    return subtotal - discountAmount;
  };

  const handleSubmit = (e: React.FormEvent) => {
    e.preventDefault();
    
    if (!formData.itemName || formData.quantity <= 0 || formData.unitPrice <= 0) {
      return;
    }

    const newItem: OfferItem = {
      id: `item-${Date.now()}`,
      offerId: '', // Will be set when offer is created
      type: formData.type,
      itemId: `${formData.type}-${Date.now()}`,
      itemName: formData.itemName,
      itemCode: formData.itemCode || undefined,
      quantity: formData.quantity,
      unitPrice: formData.unitPrice,
      totalPrice: calculateTotal(),
      description: formData.description || undefined,
      discount: formData.discount || undefined,
      discountType: formData.discount > 0 ? formData.discountType : undefined,
      // Include duration for service items
      duration: formData.type === 'service' ? formData.duration : undefined
    };

    onAddItem(newItem);
    onOpenChange(false);
    
    // Reset form
    setFormData({
      type: 'article',
      itemName: '',
      itemCode: '',
      description: '',
      quantity: 1,
      unitPrice: 0,
      discount: 0,
      discountType: 'percentage',
      duration: 60
    });
  };

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent className="sm:max-w-2xl max-h-[90vh] overflow-y-auto">
        <DialogHeader>
          <DialogTitle>Add Offer Item</DialogTitle>
        </DialogHeader>
        
        <form onSubmit={handleSubmit} className="space-y-4">
          <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
            <div className="space-y-2">
              <Label htmlFor="type">Type *</Label>
              <Select value={formData.type} onValueChange={(value: 'article' | 'service') => setFormData(prev => ({ ...prev, type: value }))}>
                <SelectTrigger>
                  <SelectValue />
                </SelectTrigger>
                <SelectContent>
                  {itemTypes.map(it => (
                    <SelectItem key={it.id} value={it.id}>{it.name}</SelectItem>
                  ))}
                </SelectContent>
              </Select>
            </div>
            
            <div className="space-y-2">
              <Label htmlFor="itemCode">Item Code</Label>
              <Input
                id="itemCode"
                value={formData.itemCode}
                onChange={(e) => setFormData(prev => ({ ...prev, itemCode: e.target.value }))}
                placeholder="SKU001"
              />
            </div>
          </div>

          <div className="space-y-2">
            <Label htmlFor="itemName">Item Name *</Label>
            <Input
              id="itemName"
              value={formData.itemName}
              onChange={(e) => setFormData(prev => ({ ...prev, itemName: e.target.value }))}
              placeholder="Enter item name"
              required
            />
          </div>

          <div className="space-y-2">
            <Label htmlFor="description">Description</Label>
            <Textarea
              id="description"
              value={formData.description}
              onChange={(e) => setFormData(prev => ({ ...prev, description: e.target.value }))}
              placeholder="Item description"
              rows={2}
            />
          </div>

          <div className="grid grid-cols-2 gap-4">
            <div className="space-y-2">
              <Label htmlFor="quantity">Quantity *</Label>
              <Input
                id="quantity"
                type="number"
                min="1"
                step="1"
                value={formData.quantity}
                onChange={(e) => setFormData(prev => ({ ...prev, quantity: parseInt(e.target.value) || 1 }))}
                required
              />
            </div>
            
            <div className="space-y-2">
              <Label htmlFor="unitPrice">Unit Price ({currency}) *</Label>
              <Input
                id="unitPrice"
                type="number"
                min="0"
                step="0.01"
                value={formData.unitPrice}
                onChange={(e) => setFormData(prev => ({ ...prev, unitPrice: parseFloat(e.target.value) || 0 }))}
                required
              />
            </div>
          </div>

          {/* Duration field for services */}
          {formData.type === 'service' && (
            <div className="space-y-2">
              <Label htmlFor="duration">Duration (minutes) *</Label>
              <Input
                id="duration"
                type="number"
                min="1"
                step="15"
                value={formData.duration}
                onChange={(e) => setFormData(prev => ({ ...prev, duration: parseInt(e.target.value) || 60 }))}
                placeholder="60"
                required
              />
            </div>
          )}

          <div className="grid grid-cols-2 gap-4">
            <div className="space-y-2">
              <Label htmlFor="discount">Discount</Label>
              <Input
                id="discount"
                type="number"
                min="0"
                step="0.01"
                value={formData.discount}
                onChange={(e) => setFormData(prev => ({ ...prev, discount: parseFloat(e.target.value) || 0 }))}
              />
            </div>
            
            <div className="space-y-2">
              <Label htmlFor="discountType">Discount Type</Label>
              <Select value={formData.discountType} onValueChange={(value: 'percentage' | 'fixed') => setFormData(prev => ({ ...prev, discountType: value }))}>
                <SelectTrigger>
                  <SelectValue />
                </SelectTrigger>
                <SelectContent>
                  <SelectItem value="percentage">Percentage (%)</SelectItem>
                  <SelectItem value="fixed">Fixed Amount ({currency})</SelectItem>
                </SelectContent>
              </Select>
            </div>
          </div>

          <div className="bg-muted/50 p-4 rounded-lg">
            <div className="flex justify-between items-center">
              <span className="font-medium">Total Price:</span>
              <span className="text-lg font-semibold">
                {calculateTotal().toLocaleString()} {currency}
              </span>
            </div>
          </div>

          <div className="flex justify-end gap-3">
            <Button type="button" variant="outline" onClick={() => onOpenChange(false)}>
              Cancel
            </Button>
            <Button type="submit">
              Add Item
            </Button>
          </div>
        </form>
      </DialogContent>
    </Dialog>
  );
}